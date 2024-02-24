namespace RenderLoop.Demo
{
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using RenderLoop.SoftwareRenderer;

    internal class CenterScreen : Cube
    {
        public CenterScreen(Display display)
            : base(display)
        {
        }

        protected override void DrawScene(AppState state, TimeSpan elapsed)
        {
            this.display.PaintFrame(elapsed, (Graphics g, Bitmap buffer, float[,] depthBuffer) =>
            {
                var screen = Screen.FromControl(this.display);
                this.Camera.Width = screen.Bounds.Width;
                this.Camera.Height = screen.Bounds.Height;

                var topLeft = this.display.PointToScreen(Point.Empty);

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
                    Display.DrawStrip(indices, i => transformed[i.index], (v, vertices) =>
                        Display.FillTriangle(buffer, depthBuffer, vertices, BackfaceCulling.Cull, perspective =>
                        {
                            var uv = Display.MapCoordinates(perspective, [v[0].uv, v[1].uv, v[2].uv]);
                            return ((int)(uv.X * 4) + (int)(uv.Y * 4)) % 2 == 0
                                ? Color.White
                                : Color.Gray;
                        }));
                }
            });
        }
    }
}
