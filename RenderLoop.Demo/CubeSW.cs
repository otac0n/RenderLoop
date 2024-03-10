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

        protected readonly (Vector3 position, Vector2 uv)[][] shapes = Array.ConvertAll(Shapes, shape => shape.Select((i, j) => (Vertices[i], UV[j])).ToArray());

        private readonly DynamicDraw.Shader<(Vector3 position, Vector2 uv), (Vector4 position, Vector2 uv)> shader;

        public CubeSW(CooperativeIdleApplicationContext context)
            : base(context)
        {
            this.display = context.CreateDisplay();

            this.shader = DynamicDraw.MakeShader<(Vector3 position, Vector2 uv), (Vector4 position, Vector2 uv)>(
                x => (this.Camera.TransformToScreenSpace(x.position), x.uv),
                x => x.position,
                DynamicDraw.MakeFragmentShader<(Vector4 position, Vector2 uv)>(
                    x => x.uv,
                    uv => ((int)(uv.X * 4) + (int)(uv.Y * 4)) % 2 == 0
                        ? Color.White
                        : Color.Gray));
        }

        protected override void Initialize()
        {
            base.Initialize();
            this.display.Show();
        }

        protected override void DrawScene(AppState state, TimeSpan elapsed)
        {
            this.display.PaintFrame(elapsed, (Graphics g, Bitmap buffer, float[,] depthBuffer) =>
            {
                this.Camera.Width = buffer.Width;
                this.Camera.Height = buffer.Height;

                foreach (var face in shapes)
                {
                    DynamicDraw.DrawStrip(buffer, depthBuffer, face, BackfaceCulling.CullCounterClockwise, this.shader);
                }
            });
        }
    }
}
