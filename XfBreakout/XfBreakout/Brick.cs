namespace XfBreakout
{
    public class Brick
    {
        public SkiaSharp.SKRect Rect { get; set; }
        public SkiaSharp.SKPaint Paint { get; set; }

        public bool Collided { get; set; }
    }
}