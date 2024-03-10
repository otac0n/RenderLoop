﻿// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.Demo
{
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using RenderLoop.SoftwareRenderer;
    using static Geometry;

    internal class CenterScreen : CubeSW
    {
        private readonly Display display2;

        public CenterScreen(CooperativeIdleApplicationContext context)
            : base(context)
        {
            this.display2 = context.CreateDisplay();
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
