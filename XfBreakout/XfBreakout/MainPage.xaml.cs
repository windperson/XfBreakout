using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;

namespace XfBreakout
{
    public partial class MainPage : ContentPage
    {
        private bool is1stDraw = true;
        public MainPage()
        {
            InitializeComponent();
        }

        private void SKGLView_OnPaintSurface(object sender, SKPaintGLSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            if (is1stDraw)
            {
                canvas.Clear(SKColors.White);

                is1stDraw = false;
            }
        }

        private void SKGLView_OnTouch(object sender, SKTouchEventArgs e)
        {
            
        }

        private void LeftButton_OnClicked(object sender, EventArgs e)
        {
            
        }

        private void RightButton_OnClicked(object sender, EventArgs e)
        {
            
        }

        private void StartButton_OnClicked(object sender, EventArgs e)
        {
            
        }
    }
}
