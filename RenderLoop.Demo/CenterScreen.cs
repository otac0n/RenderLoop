// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.Demo
{
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using Microsoft.Extensions.Logging;
    using RenderLoop.SoftwareRenderer;
    using static Geometry;

    internal class CenterScreen : CubeSW
    {
        private readonly Display display2;
        private ILogger<CenterScreen> logger;

        public CenterScreen(Display display1, Display display2, ILogger<CenterScreen> logger)
            : base(display1)
        {
            this.display2 = display2;
            display1.Text = "Display 1";
            display2.Text = "Display 2";
            this.logger = logger;
        }

        protected override void Initialize()
        {
            var location = this.display.Location;
            location.X += 100;
            location.Y += 100;
            this.display2.StartPosition = FormStartPosition.Manual;
            this.display2.Location = location;
            this.display2.Show();
        }

        protected override void DrawScene(AppState state, TimeSpan elapsed)
        {
            this.logger.LogInformation("Rendering Scene...");
            foreach (var display in new[] { this.display, this.display2 }.Where(d => !d.IsDisposed))
            {
                display.PaintFrame(elapsed, (Graphics g, Bitmap buffer, float[,] depthBuffer) =>
                {
                    this.logger.LogInformation("Rendering {Display}", display.Text);
                    var screen = Screen.FromControl(display);
                    this.Camera.Width = screen.Bounds.Width;
                    this.Camera.Height = screen.Bounds.Height;

                    var topLeft = display.PointToScreen(Point.Empty);

                    var transformed = Array.ConvertAll(Vertices, v =>
                    {
                        var p = this.Camera.TransformToScreenSpace(v);
                        p.X -= topLeft.X;
                        p.Y -= topLeft.Y;
                        return p;
                    });

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
}
