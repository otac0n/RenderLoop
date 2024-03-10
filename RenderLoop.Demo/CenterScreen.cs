// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.Demo
{
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using RenderLoop.SoftwareRenderer;
    using Fragment = (System.Numerics.Vector4 Position, System.Numerics.Vector2 UV);
    using Vertex = (System.Numerics.Vector3 Position, System.Numerics.Vector2 UV);

    internal class CenterScreen : CubeSW
    {
        private readonly Display display2;

        private readonly DynamicDraw.Shader<Point, Vertex, Fragment> shader;

        public CenterScreen(CooperativeIdleApplicationContext context)
            : base(context)
        {
            this.display2 = context.CreateDisplay();

            this.shader = DynamicDraw.MakeShader<Point, Vertex, Fragment>(
                Point.Empty,
                (p, x) =>
                {
                    var pos = this.Camera.TransformToScreenSpace(x.Position);
                    pos.X -= p.X;
                    pos.Y -= p.Y;
                    return (pos, x.UV);
                },
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

            var location = this.display.Location;
            location.X += 100;
            location.Y += 100;
            this.display2.StartPosition = FormStartPosition.Manual;
            this.display2.Location = location;
            this.display2.Show();
        }

        protected override void DrawScene(AppState state, TimeSpan elapsed)
        {
            foreach (var display in new[] { this.display, this.display2 }.Where(d => !d.IsDisposed))
            {
                display.PaintFrame(elapsed, (Graphics g, Bitmap buffer, float[,] depthBuffer) =>
                {
                    var screen = Screen.FromControl(display);
                    this.Camera.Width = screen.Bounds.Width;
                    this.Camera.Height = screen.Bounds.Height;

                    this.shader.Data = display.PointToScreen(Point.Empty);

                    foreach (var face in this.shapes)
                    {
                        DynamicDraw.DrawStrip(buffer, depthBuffer, face, BackfaceCulling.CullCounterClockwise, this.shader);
                    }
                });
            }
        }
    }
}
