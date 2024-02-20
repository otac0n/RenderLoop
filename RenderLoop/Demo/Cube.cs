namespace RenderLoop.Demo
{
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Numerics;
    using RenderLoop.SoftwareRenderer;

    internal class Cube : Display<Cube.AppState>
    {
        public record class AppState(double T);

        private readonly Camera Camera = new();

        /// <remarks>
        /// 0 -- 1
        /// |  / |
        /// | /  |
        /// 2 -- 3
        /// </remarks>
        private static readonly Vector2[] UV = [
            new(0, 0),
            new(0, 1),
            new(1, 0),
            new(1, 1),
        ];

        private static readonly Vector3[] Vertices = [
            new Vector3(-1, -1, +1) / 2, // L, F, T
            new Vector3(-1, +1, +1) / 2, // L, B, T
            new Vector3(+1, +1, +1) / 2, // R, B, T
            new Vector3(+1, -1, +1) / 2, // R, F, T
            new Vector3(-1, -1, -1) / 2, // L, F, B
            new Vector3(-1, +1, -1) / 2, // L, B, B
            new Vector3(+1, +1, -1) / 2, // R, B, B
            new Vector3(+1, -1, -1) / 2, // R, F, B
        ];

        /// <remarks>
        /// Same order as <see cref="UV"/>
        /// </remarks>
        private static readonly int[][] Shapes = [
            [1, 0, 2, 3], // TOP
            [4, 5, 7, 6], // BOTTOM
            [3, 0, 7, 4], // FRONT
            [1, 2, 5, 6], // BACK
            [0, 1, 4, 5], // LEFT
            [2, 3, 6, 7], // RIGHT
        ];

        public Cube()
            : base(new AppState(0))
        {
        }

        protected override void AdvanceFrame(ref AppState state, TimeSpan elapsed)
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

        protected override void DrawScene(AppState state, Graphics g, Bitmap buffer, float[,] depthBuffer)
        {
            this.Camera.Width = buffer.Width;
            this.Camera.Height = buffer.Height;

            var transformed = Array.ConvertAll(Vertices, this.Camera.TransformToScreenSpace);

            foreach (var face in Shapes)
            {
                var indices = face.Select((i, j) => (index: i, uv: UV[j])).ToArray();
                DrawStrip(indices, i => transformed[i.index], (v, vertices) =>
                    FillTriangle(buffer, depthBuffer, vertices, BackfaceCulling.Cull, perspective =>
                    {
                        var uv = MapCoordinates(perspective, [v[0].uv, v[1].uv, v[2].uv]);
                        return ((int)(uv.X * 4) + (int)(uv.Y * 4)) % 2 == 0
                            ? Color.White
                            : Color.Gray;
                    }));
            }
        }
    }
}
