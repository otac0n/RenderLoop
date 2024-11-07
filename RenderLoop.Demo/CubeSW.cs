// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.Demo
{
    using System;
    using System.Drawing;
    using System.Linq;
    using RenderLoop.SoftwareRenderer;
    using static Geometry;
    using Fragment = (System.Numerics.Vector4 Position, System.Numerics.Vector2 UV);
    using Vertex = (System.Numerics.Vector3 Position, System.Numerics.Vector2 UV);

    internal class CubeSW : CameraSpinner
    {
        protected readonly Display display;

        protected readonly Vertex[][] shapes = Array.ConvertAll(Shapes, shape => shape.Select((i, j) => (Vertices[i], UV[j])).ToArray());

        private readonly DynamicDraw.Shader<Vertex, Fragment> shader;

        public CubeSW(CooperativeIdleApplicationContext context)
            : base(context)
        {
            this.display = context.CreateDisplay();

            this.shader = DynamicDraw.MakeShader<Vertex, Fragment>(
                x => (this.Camera.TransformToScreenSpace(x.Position), x.UV),
                x => x.Position,
                DynamicDraw.MakeFragmentShader<Fragment>(
                    x => x.UV,
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

                foreach (var face in this.shapes)
                {
                    DynamicDraw.DrawStrip(buffer, depthBuffer, face, BackfaceCulling.CullCounterClockwise, this.shader);
                }
            });
        }
    }
}
