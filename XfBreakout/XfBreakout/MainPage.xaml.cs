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

        private bool _pageIsActive;

        private SKRect _canvasRect = SKRect.Empty;
        private float _scaleFactor;
        private bool _isFirstRender = true;

        private GameStatus _gameStatus;

        #region Score Text Game Data
        private readonly SKPaint _scoreTextPaint = new SKPaint()
        {
            Color = SKColors.Chocolate,
            IsAntialias = true,
            IsStroke = false,
            TextSize = 30.0f
        };

        private readonly (int X, int Y) _scoreTextPoint = (X: 10, Y: 50);
        private int _score = 0;
        private const string ScoreHeaderText = "SCORE:";
        #endregion

        #region PressStart Text Game Data
        private readonly SKPaint _pressStartTextPaint = new SKPaint()
        {
            Color = SKColors.DarkGray,
            IsAntialias = true,
            IsStroke = false,
            TextSize = 40.0f,
            TextAlign = SKTextAlign.Center
        };
        private const string PressStartText = "Press Start";
        #endregion

        #region GamePause Text Game Data
        private readonly SKPaint _gamePauseTextPaint = new SKPaint()
        {
            Color = SKColors.Indigo,
            IsAntialias = true,
            IsStroke = false,
            TextSize = 40.0f,
            TextAlign = SKTextAlign.Center
        };
        private const string GamePauseText = "Game Paused";
        #endregion

        #region GameClear Text Game Data
        private readonly SKPaint _gameClearTextPaint = new SKPaint()
        {
            Color = SKColors.Black,
            IsAntialias = true,
            IsStroke = false,
            TextSize = 40.0f,
            TextAlign = SKTextAlign.Center
        };
        private const string GameClearText = "Game Clear";
        #endregion

        #region GameOver Text Game Data
        private readonly SKPaint _gameOverTextPaint = new SKPaint()
        {
            Color = SKColors.Red,
            IsAntialias = true,
            IsStroke = false,
            TextSize = 40.0f,
            TextAlign = SKTextAlign.Center
        };
        private const string GameOverText = "Game Over";
        #endregion


        #region Brick Game Data
        private const int DefaultRows = 5;
        private const int DefaultCols = 4;
        private Brick[,] _bricks;
        private const float DefaultBrickWidth = 60.0f;
        private const float DefaultBrickHeight = 30.0f;
        private const float topSpacing = 50;
        private const float rowPadding = 5;
        private const float colPadding = 5;

        private bool _isBrickLayoutComputed;

        private readonly Random _rand = new Random(DateTime.Now.Millisecond);
        #endregion

        #region Ball Game Data
        private (float? X, float? Y) _ballCenter;
        private const float BallRadius = 10.0f;
        private readonly SKPaint _ballPaint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Fill };
        private const float DefaultBallHorizontalSpeed = 5f;
        private const float DefaultBallVerticalSpeed = 5f;
        private (int X, int Y) _ballSpeedVector = (X: 1, Y: 1);
        #endregion

        #region Paddle Game Data
        private const int PaddleWidth = 100;
        private const int PaddleHeight = 40;
        private SKRect _paddleRect = SKRect.Empty;
        private readonly SKPaint _paddlePaint = new SKPaint { Color = SKColors.DarkGreen };
        private bool _isPaddleDragging;
        private const float DefaultLeftRightButtonClickPaddleSpeed = 0.2f;
        #endregion


        public MainPage()
        {
            InitializeComponent();
            InitBricks(DefaultRows, DefaultCols);
        }

        private void InitBricks(int rows, int cols)
        {
            _isBrickLayoutComputed = false;
            _bricks = new Brick[DefaultRows, DefaultCols];
            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < cols; j++)
                {
                    _bricks[i, j] = new Brick
                    {
                        Paint = _rand.Next(0, 2) == 1
                        ? new SKPaint { Color = SKColors.Blue }
                        : new SKPaint { Color = SKColors.Green }
                    };
                }
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _pageIsActive = true;

            await AnimationLoop();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _pageIsActive = false;
        }

        private async Task AnimationLoop()
        {
            while (_pageIsActive)
            {
                GameTicker();
                SkglView.InvalidateSurface();
                await Task.Delay(TimeSpan.FromSeconds(DefaultFrameRate));
            }
        }

        private void GameTicker()
        {
            if (_gameStatus != GameStatus.Playing)
            {
                return;
            }

            if (_canvasRect.IsEmpty || _paddleRect.IsEmpty
                                    || !(_ballCenter.X.HasValue && _ballCenter.Y.HasValue))
            {
                return;
            }

            _ballCenter.X += DefaultBallHorizontalSpeed * _ballSpeedVector.X;
            _ballCenter.Y += DefaultBallVerticalSpeed * _ballSpeedVector.Y;

            //check if hit left or right walls
            if (_ballCenter.X - BallRadius <= 0 || _ballCenter.X + BallRadius >= _canvasRect.Width)
            {
                _ballSpeedVector.X = -_ballSpeedVector.X;
            }

            //check if hit top wall
            if (_ballCenter.Y - BallRadius <= 0)
            {
                _ballSpeedVector.Y = -_ballSpeedVector.Y;
            }

            //check if ball fail down out
            if (_ballCenter.Y + BallRadius >= _canvasRect.Height)
            {
                _gameStatus = GameStatus.GameOver;
                LeftBtn.IsEnabled = false;
                RightBtn.IsEnabled = false;
                GameStatusBtn.Text = "Retry";
                return;
            }

            var ballHitZone = SKRect.Create(
                new SKPoint(_ballCenter.X.Value, _ballCenter.Y.Value), new SKSize(BallRadius * 2, BallRadius * 2));

            //check if ball hit paddle
            if (_paddleRect.IntersectsWithInclusive(ballHitZone))
            {
                _ballSpeedVector.Y = -1 * Math.Abs(_ballSpeedVector.Y);
            }

            var brickCollideCount = 0;
            for (var i = 0; i < DefaultRows; i++)
            {
                for (var j = 0; j < DefaultCols; j++)
                {
                    var brick = _bricks[i, j];

                    if (brick.Collided)
                    {
                        continue;
                    }

                    if (brick.Rect.IntersectsWithInclusive(ballHitZone))
                    {
                        brick.Collided = true;
                        brickCollideCount++;
                        _ballSpeedVector.Y = -_ballSpeedVector.Y;
                        break;
                    }
                }
            }

            _score += brickCollideCount;

            //check if game cleared
            var isGameClear = true;
            for (var i = 0; i < DefaultRows; i++)
            {
                for (var j = 0; j < DefaultCols; j++)
                {
                    var brick = _bricks[i, j];

                    if (brick.Collided) continue;
                    isGameClear = false;
                    break;
                }
            }

            if (isGameClear)
            {
                _gameStatus = GameStatus.GameClear;
                LeftBtn.IsEnabled = false;
                RightBtn.IsEnabled = false;
                GameStatusBtn.Text = "Replay";
            }

        }

        private void ResetGameData()
        {
            InitBricks(DefaultRows, DefaultCols);

            _paddleRect = SKRect.Empty;
            _isPaddleDragging = false;

            _ballCenter = (X: null, Y: null);
            _ballSpeedVector = (X: 1, Y: 1);

            _score = 0;
        }


        private void SKGLView_OnPaintSurface(object sender, SKPaintGLSurfaceEventArgs e)
        {
            if (!(sender is SKGLView container))
            {
                return;
            }

            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);

            var info = e.RenderTarget;
            _scaleFactor = (float)(info.Width / container.Width);
            if (_isFirstRender)
            {
                Util.Log($"scale = {_scaleFactor}");
                _isFirstRender = false;
            }

            canvas.Scale(_scaleFactor);
            if (_canvasRect.IsEmpty)
            {
                _canvasRect = canvas.LocalClipBounds;
            }

            DrawScore(canvas);
            DrawBricks(canvas);
            DrawPaddle(canvas);
            DrawBall(canvas);
            DrawGameStatus(canvas);
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
                var canvasRect = canvas.LocalClipBounds;
                _paddleRect = SKRect.Create(PaddleWidth, PaddleHeight);
                var initX = (canvasRect.Width - _paddleRect.Width) / 2;
                var initY = canvasRect.Height - _paddleRect.Height - PaddleWidth / 2;

                Util.Log($"paddle init on ({initX}, {initY})");
                _paddleRect.Location = new SKPoint(initX, initY);
            }

            canvas.DrawRoundRect(_paddleRect, new SKSize(7, 7), _paddlePaint);
        }

        private void DrawBricks(SKCanvas canvas)
        {
            if (_bricks == null)
            {
                return;
            }

            float baseSlotWidth = 0.0f, baseSlotHeight = 0.0f, leftSpacing = 0.0f;

            if (!_isBrickLayoutComputed)
            {
                var canvasRect = canvas.LocalClipBounds;

                baseSlotWidth = (canvasRect.Width - rowPadding * 2) / (DefaultCols + 1);
                leftSpacing = (canvasRect.Width - baseSlotWidth * (DefaultCols + 1)) / 2;
                if (leftSpacing < 0)
                {
                    leftSpacing = 0;
                }
                baseSlotHeight = (canvasRect.Height / 2.0f) / (DefaultRows + 1);
            }

            for (var i = 0; i < DefaultRows; i++)
            {
                for (var j = 0; j < DefaultCols; j++)
                {
                    var brick = _bricks[i, j];
                    if (brick.Collided)
                    {
                        continue;
                    }
                    // |---x---x---x---x---|
                    if (brick.Rect.IsEmpty)
                    {
                        var midX = baseSlotWidth * i + rowPadding + leftSpacing;
                        var midY = baseSlotHeight * j + colPadding + topSpacing;
                        _bricks[i, j].Rect = SKRect.Create(midX, midY,
                            DefaultBrickWidth, DefaultBrickHeight);
                    }
                    canvas.DrawRect(brick.Rect, brick.Paint);
                }
            }

            _isBrickLayoutComputed = true;
        }

        private void DrawScore(SKCanvas canvas)
        {
            var score = $"{ScoreHeaderText} {_score:D2}";
            canvas.DrawText(score, _scoreTextPoint.X, _scoreTextPoint.Y, _scoreTextPaint);
        }

        private void DrawGameStatus(SKCanvas canvas)
        {
            if (_gameStatus == GameStatus.Playing) { return; }
            var rect = canvas.LocalClipBounds;

            switch (_gameStatus)
            {
                case GameStatus.Initial:
                case GameStatus.UnStart:
                    canvas.DrawText(PressStartText, rect.MidX, rect.MidY, _pressStartTextPaint);
                    break;
                case GameStatus.Paused:
                    canvas.DrawText(GamePauseText, rect.MidX, rect.MidY, _gamePauseTextPaint);
                    break;
                case GameStatus.GameClear:
                    canvas.DrawText(GameClearText, rect.MidX, rect.MidY, _gameClearTextPaint);
                    break;
                case GameStatus.GameOver:
                    canvas.DrawText(GameOverText, rect.MidX, rect.MidY, _gameOverTextPaint);
                    break;
            }
        }

        private void SKGLView_OnTouch(object sender, SKTouchEventArgs e)
        {
            if (_gameStatus != GameStatus.Playing)
            {
                _isPaddleDragging = false;
                return;
            }

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
            SkglView.InvalidateSurface();
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
            SkglView.InvalidateSurface();
        }

        private void StartButton_OnClicked(object sender, EventArgs e)
        {
            switch (_gameStatus)
            {
                case GameStatus.Initial:
                    _gameStatus = GameStatus.Playing;
                    LeftBtn.IsEnabled = true;
                    RightBtn.IsEnabled = true;
                    GameStatusBtn.Text = "Pause";
                    break;

                case GameStatus.UnStart:
                    _gameStatus = GameStatus.Playing;
                    LeftBtn.IsEnabled = true;
                    RightBtn.IsEnabled = true;
                    GameStatusBtn.Text = "Pause";
                    ResetGameData();
                    break;

                case GameStatus.Playing:
                    _gameStatus = GameStatus.Paused;
                    LeftBtn.IsEnabled = false;
                    RightBtn.IsEnabled = false;
                    GameStatusBtn.Text = "Resume";
                    break;

                case GameStatus.Paused:
                    _gameStatus = GameStatus.Playing;
                    LeftBtn.IsEnabled = true;
                    RightBtn.IsEnabled = true;
                    GameStatusBtn.Text = "Pause";
                    break;

                case GameStatus.GameClear:
                    _gameStatus = GameStatus.UnStart;
                    LeftBtn.IsEnabled = false;
                    RightBtn.IsEnabled = false;
                    GameStatusBtn.Text = "Replay";
                    break;

                case GameStatus.GameOver:
                    _gameStatus = GameStatus.UnStart;
                    LeftBtn.IsEnabled = false;
                    RightBtn.IsEnabled = false;
                    GameStatusBtn.Text = "Start";
                    break;
            }
        }
    }
}
