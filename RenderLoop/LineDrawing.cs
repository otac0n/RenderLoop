// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop
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
            Vector2 p;
            int i;
            for (i = 0, p = start; i <= step; i++, p = start + v * i)
            {
                yield return ((int)p.X, (int)p.Y);
            }
        }
    }
}
