namespace RenderLoop.SoftwareRenderer
{
    using System.Collections.Generic;
    using System.Numerics;

    public class LineDrawing
    {
        public static IEnumerable<(int x, int y)> PlotLine(Vector2 start, Vector2 end)
        {
            var v = end - start;
            var a = Vector2.Abs(v);
            var step = a.X >= a.Y ? a.X : a.Y;
            v /= step;
            var p = start;
            var i = 0;
            while (i <= step)
            {
                yield return ((int)p.X, (int)p.Y);
                p += v;
                i++;
            }
        }
    }
}
