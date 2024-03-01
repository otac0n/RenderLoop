namespace RenderLoop.Demo
{
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Numerics;
    using RenderLoop.SoftwareRenderer;
    using static Geometry;

    internal class Cube : GameLoop<Cube.AppState>
    {
        public record class AppState(double T);

        protected readonly Display display;
        protected readonly Camera Camera = new();

        protected readonly Display.FragmentShader<(uint index, Vector2 uv)> shader;

        public Cube(Display display)
            : base(display, new AppState(0))
        {
            this.display = display;

            this.shader = Display.MakeFragmentShader<(uint index, Vector2 uv)>(
                x => x.uv,
                uv => ((int)(uv.X * 4) + (int)(uv.Y * 4)) % 2 == 0
                    ? Color.White
                    : Color.Gray);
        }

        protected sealed override void AdvanceFrame(ref AppState state, TimeSpan elapsed)
        {
            var dist = 2;

            var a = Math.Tau * state.T / 3;
            var (x, y) = Math.SinCos(a);
            var z = Math.Sin(a / 3);
            var p = new Vector3((float)(dist * x), (float)(dist * y), (float)(dist / 2 * z));

            this.Camera.Position = p;
            this.Camera.Direction = -p;

            state = state with
            {
                T = state.T + elapsed.TotalSeconds,
            };
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
                    Display.DrawStrip(indices, i => transformed[i.index], (v, vertices) =>
                        Display.FillTriangle(buffer, depthBuffer, vertices, BackfaceCulling.CullCounterClockwise, perspective => this.shader(v, perspective)));
                }
            });
        }
    }
}
