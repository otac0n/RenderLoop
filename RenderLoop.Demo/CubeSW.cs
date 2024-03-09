// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.Demo
{
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Numerics;
    using RenderLoop.SoftwareRenderer;
    using static Geometry;

    internal class CubeSW : CameraSpinner
    {
        protected readonly Display display;

        protected readonly DynamicDraw.FragmentShader<(uint index, Vector2 uv)> shader;

        public CubeSW(Display display)
            : base(display)
        {
            this.display = display;

            this.shader = DynamicDraw.MakeFragmentShader<(uint index, Vector2 uv)>(
                x => x.uv,
                uv => ((int)(uv.X * 4) + (int)(uv.Y * 4)) % 2 == 0
                    ? Color.White
                    : Color.Gray);
        }

        protected override void DrawScene(AppState state, TimeSpan elapsed)
        {
            this.display.PaintFrame(elapsed, (Graphics g, Bitmap buffer, float[,] depthBuffer) =>
            {
                this.Camera.Width = buffer.Width;
                this.Camera.Height = buffer.Height;

                var transformed = Array.ConvertAll(Vertices, this.Camera.TransformToScreenSpace);

                foreach (var face in Shapes)
                {
                    var indices = face.Select((i, j) => (index: i, uv: UV[j])).ToArray();
                    DynamicDraw.DrawStrip(indices, i => transformed[i.index], (v, vertices) =>
                        DynamicDraw.FillTriangle(buffer, depthBuffer, vertices, BackfaceCulling.CullCounterClockwise, perspective => this.shader(v, perspective)));
                }
            });
        }
    }
}
