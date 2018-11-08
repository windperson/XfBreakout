using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;
using XfBreakout.Common;

namespace XfBreakout
{
    public partial class MainPage : ContentPage
    {
        private const double DefaultFrameRate = 1.0 / 30;
        private const string str = "SCORE: 0000";

        private bool _pageIsActive;

        private SKRect _canvasRect;
        private float _scaleFactor;

        private (float? X, float? Y) _ballCenter;
        private const float BallRadius = 10.0f;
        private readonly SKPaint _ballPaint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Fill };
        private const float DefaultBallHorizontalSpeed = 0.2f;
        private const float DefaultBallVerticalSpeed = 0.2f;

        private SKRect _paddleRect = SKRect.Empty;
        private readonly SKPaint _paddlePaint = new SKPaint { Color = SKColors.DarkGreen };
        private bool _is1stTime = true;
        private bool _isPaddleDragging;

        private const float DefaultLeftRightButtonClickPaddleSpeed = 0.2f;

        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _pageIsActive = true;
#pragma warning disable 4014
            AnimationLoop();
#pragma warning restore 4014
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _pageIsActive = false;
        }

        private async Task AnimationLoop()
        {
            Util.Log("Start animation loop");
            while (_pageIsActive)
            {
                await GameTicker();
                SkglView.InvalidateSurface();
                await Task.Delay(TimeSpan.FromSeconds(DefaultFrameRate));
            }
            Util.Log("Stop animation loop");
        }

        private async Task GameTicker()
        {

        }


        private void SKGLView_OnPaintSurface(object sender, SKPaintGLSurfaceEventArgs e)
        {
            if (!(sender is SKGLView container))
            {
                return;
            }

            var canvas = e.Surface.Canvas;
            GRBackendRenderTargetDesc info = e.RenderTarget;

            canvas.Clear(SKColors.White);


            _scaleFactor = (float)(info.Width / container.Width);
            if (_is1stTime)
            {
                Util.Log($"scale = {_scaleFactor}");
                _is1stTime = false;
            }

            canvas.Scale(_scaleFactor);
            if (_canvasRect.IsEmpty)
            {
                _canvasRect = canvas.LocalClipBounds;
            }

            DrawScore(canvas);
            DrawBricks(canvas, info);
            DrawPaddle(canvas);
            DrawBall(canvas);

        }

        private void DrawBall(SKCanvas canvas)
        {
            if (!_ballCenter.X.HasValue && !_ballCenter.Y.HasValue)
            {
                var canvasRect = canvas.LocalClipBounds;
                _ballCenter.X = canvasRect.MidX;
                _ballCenter.Y = canvasRect.MidY;
            }

            canvas.DrawCircle(_ballCenter.X.Value, _ballCenter.Y.Value, BallRadius, _ballPaint);
        }

        private void DrawPaddle(SKCanvas canvas)
        {
            if (_paddleRect.IsEmpty)
            {
                _paddleRect = SKRect.Create(80, 25);
                var initX = (canvas.LocalClipBounds.Width - _paddleRect.Width) / 2;
                var initY = canvas.LocalClipBounds.Height - _paddleRect.Height - 40;

                Util.Log($"paddle init on ({initX}, {initY})");
                _paddleRect.Location = new SKPoint(initX, initY);
            }

            canvas.DrawRect(_paddleRect, _paddlePaint);
        }

        private void DrawBricks(SKCanvas canvas, GRBackendRenderTargetDesc info)
        {


        }

        private static void DrawScore(SKCanvas canvas)
        {
            var textPaint = new SKPaint()
            {
                Color = SKColors.Chocolate,
                IsAntialias = true,
                IsStroke = false,
                TextSize = 30.0f
            };

            canvas.DrawText(str, 10, 50, textPaint);
        }

        private void SKGLView_OnTouch(object sender, SKTouchEventArgs e)
        {
            Util.Log("touch event triggered.");
            if (_canvasRect.IsEmpty || _paddleRect.IsEmpty)
            {
                return;

            }
            switch (e.ActionType)
            {
                case SKTouchAction.Pressed:
                    var pressPoint = e.Location;
                    Util.Log($"press on ({pressPoint.X}, {pressPoint.Y})");
                    if (_paddleRect.IsEmpty) { return; }

                    var touchEnableRect = SKRect.Create(e.Location.X / _scaleFactor, e.Location.Y / _scaleFactor, 100, 100);

                    if (touchEnableRect.IntersectsWithInclusive(_paddleRect))
                    {
                        Util.Log($"paddle touched");
                        _isPaddleDragging = true;
                    }
                    else
                    {
                        _isPaddleDragging = false;
                    }
                    break;

                case SKTouchAction.Moved:
                    Util.Log($"move on ({e.Location.X}, {e.Location.Y})");
                    if (!_isPaddleDragging) { return; }

                    if (e.InContact)
                    {
                        var newX = e.Location.X / _scaleFactor - _paddleRect.Width / 2.0f;
                        if (newX < 0)
                        {
                            newX = 0;
                        }
                        else if (newX > _canvasRect.Width - _paddleRect.Width)
                        {
                            newX = _canvasRect.Width - _paddleRect.Width;
                        }
                        var newY = _paddleRect.Top;

                        _paddleRect = SKRect.Create(newX, newY, _paddleRect.Width, _paddleRect.Height);
                    }
                    break;

                case SKTouchAction.Released:
                    Util.Log($"released on ({e.Location.X}, {e.Location.Y})");
                    _isPaddleDragging = false;
                    break;
            }

            e.Handled = true;
            // update the UI
            ((SKGLView)sender).InvalidateSurface();

        }

        private void LeftButton_OnClicked(object sender, EventArgs e)
        {
            if (_paddleRect.IsEmpty || _canvasRect.IsEmpty)
            {
                return;
            }

            var newX = _paddleRect.Left - DefaultLeftRightButtonClickPaddleSpeed * _canvasRect.Width;
            if (newX < 0)
            {
                newX = 0;
            }

            _paddleRect = SKRect.Create(newX, _paddleRect.Top, _paddleRect.Width, _paddleRect.Height);
        }

        private void RightButton_OnClicked(object sender, EventArgs e)
        {
            if (_paddleRect.IsEmpty || _canvasRect.IsEmpty)
            {
                return;
            }

            var newX = _paddleRect.Left + DefaultLeftRightButtonClickPaddleSpeed * _canvasRect.Width;
            if (newX > _canvasRect.Right - _paddleRect.Width)
            {
                newX = _canvasRect.Right - _paddleRect.Width;
            }

            _paddleRect = SKRect.Create(newX, _paddleRect.Top, _paddleRect.Width, _paddleRect.Height);
        }

        private void MenuButton_OnClicked(object sender, EventArgs e)
        {

        }
    }
}
