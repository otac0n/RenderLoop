namespace RenderLoop.SoftwareRenderer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    public abstract partial class Display : Form
    {
        private int width;
        private int height;
        private Bitmap buffer;
        private float[,] depthBuffer;
        private long timestamp;
        private double fps;
        private readonly Dictionary<Keys, bool> keyDown = [];

        public Display()
        {
            this.InitializeComponent();
            this.UpdateSize();
            this.timestamp = Stopwatch.GetTimestamp();
            this.KeyDown += this.Display_KeyDown;
            this.KeyUp += this.Display_KeyUp;
            this.AdvanceFrame(TimeSpan.Zero);
        }

        private void Display_KeyDown(object? sender, KeyEventArgs e)
        {
            this.keyDown[e.KeyCode] = true;
        }

        private void Display_KeyUp(object? sender, KeyEventArgs e)
        {
            this.keyDown[e.KeyCode] = false;
        }

        public bool ShowFps { get; set; } = true;

        protected Camera Camera { get; } = new();

        public bool this[Keys key] => this.keyDown.TryGetValue(key, out var pressed) && pressed;

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var g = Graphics.FromImage(this.buffer))
            {
                g.Clear(this.BackColor);
                ClearDepthBuffer(this.depthBuffer);
                this.DrawScene(g, this.buffer, this.depthBuffer);
            }

            e.Graphics.DrawImageUnscaled(this.buffer, Point.Empty);
        }

        protected virtual void DrawScene(Graphics g, Bitmap buffer, float[,] depthBuffer)
        {
            if (this.ShowFps)
            {

                var fps = $"{this.fps:F1} FPS";
                var size = g.MeasureString(fps, this.Font);
                using var textBrush = new SolidBrush(this.ForeColor);
                g.DrawString(fps, this.Font, textBrush, new PointF(buffer.Width - size.Width, 0));
            }
        }

        /// <summary>
        /// Segments a strip of triangles and renders them with the provided function.
        /// </summary>
        /// <typeparam name="TVertex">The type of a triangle vertex.</typeparam>
        /// <param name="source">An array of vertices.</param>
        /// <param name="render">The function that will be called to render each triangle.</param>
        public static void DrawStrip<TVertex>(TVertex[] source, Action<TVertex[]> render)
        {
            const int TRIANGLE_POINTS = 3;
            var vertices = new TVertex[TRIANGLE_POINTS];
            if (source.Length >= TRIANGLE_POINTS)
            {
                vertices[0] = source[0];
                vertices[1] = source[1];
                for (var i = 2; i < source.Length; i++)
                {
                    vertices[2] = source[i];
                    render(vertices);

                    vertices[i % 2] = vertices[2];
                }
            }
        }

        /// <summary>
        /// Segments a strip of triangles and renders them with the provided function.
        /// </summary>
        /// <typeparam name="TVertex">The type of a triangle vertex.</typeparam>
        /// <param name="source">An enumerable collection of vertices.</param>
        /// <param name="render">The function that will be called to render each triangle.</param>
        public static void DrawStrip<TVertex>(IEnumerable<TVertex> source, Action<TVertex[]> render)
        {
            const int TRIANGLE_POINTS = 3;
            var vertices = new TVertex[TRIANGLE_POINTS];
            using var enumerable = source.GetEnumerator();
            if (enumerable.MoveNext())
            {
                vertices[0] = enumerable.Current;
                if (enumerable.MoveNext())
                {
                    vertices[1] = enumerable.Current;
                    for (var i = 0; enumerable.MoveNext(); i++)
                    {
                        vertices[2] = enumerable.Current;
                        render(vertices);

                        vertices[i % 2] = vertices[2];
                    }
                }
            }
        }

        /// <summary>
        /// Segments a strip of triangles and renders them with the provided function.
        /// </summary>
        /// <typeparam name="TVertex">The type of a triangle vertex.</typeparam>
        /// <param name="indices">The indices that represent the triangles.</param>
        /// <param name="vertices">The lookup source for the indices.</param>
        /// <param name="render">The function that will be called to render each triangle.</param>
        public static void DrawStrip<TVertex>(int[] indices, TVertex[] vertices, Action<int[], TVertex[]> render)
        {
            const int TRIANGLE_POINTS = 3;
            var indexSwath = new int[TRIANGLE_POINTS];
            var vertexSwath = new TVertex[TRIANGLE_POINTS];
            if (indices.Length >= TRIANGLE_POINTS)
            {
                vertexSwath[0] = vertices[indexSwath[0] = indices[0]];
                vertexSwath[1] = vertices[indexSwath[1] = indices[1]];
                for (var i = 2; i < indices.Length; i++)
                {
                    vertexSwath[2] = vertices[indexSwath[2] = indices[i]];
                    render(indexSwath, vertexSwath);

                    indexSwath[i % 2] = indexSwath[2];
                    vertexSwath[i % 2] = vertexSwath[2];
                }
            }
        }

        /// <summary>
        /// Segments a strip of triangles and renders them with the provided function.
        /// </summary>
        /// <typeparam name="TVertex">The type of a triangle vertex.</typeparam>
        /// <param name="indices">The indices that represent the triangles.</param>
        /// <param name="vertices">The lookup source for the indices.</param>
        /// <param name="render">The function that will be called to render each triangle.</param>
        public static void DrawStrip<TVertex>(int[] indices, TVertex[] vertices, Action<TVertex[]> render)
        {
            const int TRIANGLE_POINTS = 3;
            var vertexSwath = new TVertex[TRIANGLE_POINTS];
            if (indices.Length >= TRIANGLE_POINTS)
            {
                vertexSwath[0] = vertices[indices[0]];
                vertexSwath[1] = vertices[indices[1]];
                for (var i = 2; i < indices.Length; i++)
                {
                    vertexSwath[2] = vertices[indices[i]];
                    render(vertexSwath);

                    vertexSwath[i % 2] = vertexSwath[2];
                }
            }
        }

        /// <summary>
        /// Segments a strip of triangles and renders them with the provided function.
        /// </summary>
        /// <typeparam name="TSource">The type containing a source vertex.</typeparam>
        /// <typeparam name="TVertex">The type of a triangle vertex.</typeparam>
        /// <param name="source">The source containing vertices.</param>
        /// <param name="getVertex">A function that will be used to retrieve each vertex.</param>
        /// <param name="render">The function that will be called to render each triangle.</param>
        public static void DrawStrip<TSource, TVertex>(TSource[] source, Func<TSource, TVertex> getVertex, Action<TSource[], TVertex[]> render)
        {
            const int TRIANGLE_POINTS = 3;
            var items = new TSource[TRIANGLE_POINTS];
            var vertices = new TVertex[TRIANGLE_POINTS];
            if (source.Length >= TRIANGLE_POINTS)
            {
                vertices[0] = getVertex(items[0] = source[0]);
                vertices[1] = getVertex(items[1] = source[1]);
                for (var i = 2; i < source.Length; i++)
                {
                    vertices[2] = getVertex(items[2] = source[i]);
                    render(items, vertices);

                    items[i % 2] = items[2];
                    vertices[i % 2] = vertices[2];
                }
            }
        }

        /// <summary>
        /// Segments a strip of triangles and renders them with the provided function.
        /// </summary>
        /// <typeparam name="TSource">The type containing a source vertex.</typeparam>
        /// <typeparam name="TVertex">The type of a triangle vertex.</typeparam>
        /// <param name="source">The source containing vertices.</param>
        /// <param name="getVertex">A function that will be used to retrieve each vertex.</param>
        /// <param name="render">The function that will be called to render each triangle.</param>
        public static void DrawStrip<TSource, TVertex>(IEnumerable<TSource> source, Func<TSource, TVertex> getVertex, Action<TSource[], TVertex[]> render)
        {
            const int TRIANGLE_POINTS = 3;
            var items = new TSource[TRIANGLE_POINTS];
            var vertices = new TVertex[TRIANGLE_POINTS];
            using var enumerable = source.GetEnumerator();
            if (enumerable.MoveNext())
            {
                vertices[0] = getVertex(items[0] = enumerable.Current);
                if (enumerable.MoveNext())
                {
                    vertices[1] = getVertex(items[1] = enumerable.Current);
                    for (var i = 0; enumerable.MoveNext(); i++)
                    {
                        vertices[2] = getVertex(items[2] = enumerable.Current);
                        render(items, vertices);

                        items[i % 2] = items[2];
                        vertices[i % 2] = vertices[2];
                    }
                }
            }
        }

        /// <summary>
        /// Segments a strip of triangles and renders them with the provided function.
        /// </summary>
        /// <typeparam name="TSource">The type containing a source vertex.</typeparam>
        /// <typeparam name="TVertex">The type of a triangle vertex.</typeparam>
        /// <param name="source">The source containing vertices.</param>
        /// <param name="getVertex">A function that will be used to retrieve each vertex.</param>
        /// <param name="render">The function that will be called to render each triangle.</param>
        public static void DrawStrip<TSource, TVertex>(TSource[] source, Func<TSource, TVertex> getVertex, Action<TVertex[]> render)
        {
            const int TRIANGLE_POINTS = 3;
            var vertices = new TVertex[TRIANGLE_POINTS];
            if (source.Length >= TRIANGLE_POINTS)
            {
                vertices[0] = getVertex(source[0]);
                vertices[1] = getVertex(source[1]);
                for (var i = 2; i < source.Length; i++)
                {
                    vertices[2] = getVertex(source[i]);
                    render(vertices);

                    vertices[i % 2] = vertices[2];
                }
            }
        }

        /// <summary>
        /// Segments a strip of triangles and renders them with the provided function.
        /// </summary>
        /// <typeparam name="TSource">The type containing a source vertex.</typeparam>
        /// <typeparam name="TVertex">The type of a triangle vertex.</typeparam>
        /// <param name="source">The source containing vertices.</param>
        /// <param name="getVertex">A function that will be used to retrieve each vertex.</param>
        /// <param name="render">The function that will be called to render each triangle.</param>
        public static void DrawStrip<TSource, TVertex>(IEnumerable<TSource> source, Func<TSource, TVertex> getVertex, Action<TVertex[]> render)
        {
            const int TRIANGLE_POINTS = 3;
            var vertices = new TVertex[TRIANGLE_POINTS];
            using var enumerable = source.GetEnumerator();
            if (enumerable.MoveNext())
            {
                vertices[0] = getVertex(enumerable.Current);
                if (enumerable.MoveNext())
                {
                    vertices[1] = getVertex(enumerable.Current);
                    for (var i = 0; enumerable.MoveNext(); i++)
                    {
                        vertices[2] = getVertex(enumerable.Current);
                        render(vertices);

                        vertices[i % 2] = vertices[2];
                    }
                }
            }
        }

        private static void ClearDepthBuffer(float[,] depthBuffer)
        {
            for (var y = 0; y < depthBuffer.GetLength(0); y++)
            {
                for (var x = 0; x < depthBuffer.GetLength(1); x++)
                {
                    depthBuffer[y, x] = float.PositiveInfinity;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float EdgeFunction(Vector3 a, Vector3 b, Vector3 c)
        {
            return (c.X - a.X) * (b.Y - a.Y) - (c.Y - a.Y) * (b.X - a.X);
        }

        public static void DrawWireFrame(Graphics g, Vector4[] vertices)
        {
            var p = Array.ConvertAll(vertices, v => new PointF(v.X, v.Y));
            g.DrawLine(Pens.White, p[0], p[1]);
            g.DrawLine(Pens.White, p[2], p[1]);
            g.DrawLine(Pens.White, p[2], p[0]);
        }

        public static Vector3 MapCoordinates(Vector3 barycenter, Vector3[] coordinates) =>
            (barycenter.X * coordinates[0] + barycenter.Y * coordinates[1] + barycenter.Z * coordinates[2]) / (barycenter.X + barycenter.Y + barycenter.Z);

        public static Vector2 MapCoordinates(Vector3 barycenter, Vector2[] coordinates) =>
            (barycenter.X * coordinates[0] + barycenter.Y * coordinates[1] + barycenter.Z * coordinates[2]) / (barycenter.X + barycenter.Y + barycenter.Z);

        public static void FillTriangle(Bitmap bitmap, float[,] depthBuffer, Vector4[] vertices, BackfaceCulling culling, Func<Vector3, Color> getColor) =>
            FillTriangle(bitmap, depthBuffer, vertices, culling, barycenter => getColor(barycenter).ToArgb());

        public static void FillTriangle(Bitmap bitmap, float[,] depthBuffer, Vector4[] vertices, BackfaceCulling culling, Func<Vector3, int> getArgb)
        {
            var width = bitmap.Width;
            var height = bitmap.Height;

            static (Vector3 vertex, float factor) AsVector3(Vector4 v) => (new(v.X, v.Y, v.Z), v.Z * Math.Abs(v.W));

            var (v0, f0) = AsVector3(vertices[0]);
            var (v1, f1) = AsVector3(vertices[1]);
            var (v2, f2) = AsVector3(vertices[2]);
            var min = Vector3.Min(Vector3.Min(v0, v1), v2);
            var max = Vector3.Max(Vector3.Max(v0, v1), v2);

            if (float.IsNaN(min.X) ||
                float.IsNaN(min.Y) ||
                min.Z <= 0 ||
                min.X > width ||
                min.Y > height ||
                max.X < 0 ||
                max.Y < 0)
            {
                return;
            }

            var area = EdgeFunction(v0, v1, v2);
            if (culling == BackfaceCulling.Cull && area <= 0)
            {
                return;
            }

            var clamp = new Vector3(width - 1, height - 1, 0);
            min = Vector3.Min(clamp, Vector3.Max(Vector3.Zero, min));
            max = Vector3.Min(clamp, Vector3.Max(Vector3.Zero, max));
            var initX = (int)min.X;
            var initY = (int)min.Y;
            var boundX = (int)max.X - initX + 1;
            var boundY = (int)max.Y - initY + 1;

            var bmpData = bitmap.LockBits(new Rectangle(initX, initY, boundX, boundY), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            var colorData = new int[boundX];
            var scan = bmpData.Scan0;

            var p = Vector3.Zero;
            for (var y = 0; y < boundY; y++, scan += bmpData.Stride)
            {
                p.Y = y + initY + 0.5f;

                var x = 0;
                var startX = -1;
                for (; x < boundX; x++)
                {
                    p.X = x + initX + 0.5f;

                    var barycenter = new Vector3(
                        EdgeFunction(v1, v2, p),
                        EdgeFunction(v2, v0, p),
                        EdgeFunction(v0, v1, p));
                    if ((barycenter.X >= 0) && (barycenter.Y >= 0) && (barycenter.Z >= 0) ||
                        (barycenter.X <= 0) && (barycenter.Y <= 0) && (barycenter.Z <= 0))
                    {
                        if (startX < 0)
                        {
                            startX = x;
                            Marshal.Copy(scan + sizeof(int) * startX, colorData, startX, boundX - startX);
                        }

                        barycenter /= area;
                        p.Z = barycenter.X * v0.Z + barycenter.Y * v1.Z + barycenter.Z * v2.Z;
                        if (p.Z > 0)
                        {
                            if (p.Z < depthBuffer[y + initY, x + initX])
                            {
                                barycenter.X /= f0;
                                barycenter.Y /= f1;
                                barycenter.Z /= f2;

                                var color = getArgb(barycenter);
                                if ((color & 0xFF000000) == 0xFF000000)
                                {
                                    depthBuffer[y + initY, x + initX] = p.Z;
                                    colorData[x] = color;
                                }
                            }
                        }
                    }
                    else if (startX >= 0)
                    {
                        break;
                    }
                }

                if (startX >= 0)
                {
                    Marshal.Copy(colorData, startX, scan + sizeof(int) * startX, x - startX);
                }
            }

            bitmap.UnlockBits(bmpData);
        }

        public static void FillTriangleA(Bitmap bitmap, float[,] depthBuffer, Vector4[] vertices, BackfaceCulling culling, Func<Vector3, int> getArgb)
        {
            var width = bitmap.Width;
            var height = bitmap.Height;

            static (Vector3 vertex, float factor) AsVector3(Vector4 v) => (new(v.X, v.Y, v.Z), v.Z * Math.Abs(v.W));

            var (v0, f0) = AsVector3(vertices[0]);
            var (v1, f1) = AsVector3(vertices[1]);
            var (v2, f2) = AsVector3(vertices[2]);
            var min = Vector3.Min(Vector3.Min(v0, v1), v2);
            var max = Vector3.Max(Vector3.Max(v0, v1), v2);

            if (float.IsNaN(min.X) ||
                float.IsNaN(min.Y) ||
                min.Z <= 0 ||
                min.X > width ||
                min.Y > height ||
                max.X < 0 ||
                max.Y < 0)
            {
                return;
            }

            var area = EdgeFunction(v0, v1, v2);
            if (culling == BackfaceCulling.Cull && area <= 0)
            {
                return;
            }

            var clamp = new Vector3(width - 1, height - 1, 0);
            min = Vector3.Min(clamp, Vector3.Max(Vector3.Zero, min));
            max = Vector3.Min(clamp, Vector3.Max(Vector3.Zero, max));
            var initY = (int)min.Y;
            var initX = (int)min.X;
            var boundY = (int)max.Y - initY + 1;
            var boundX = (int)max.X - initX + 1;

            var startEnd = new (float start, Vector3 startCoord, float end, Vector3 endCoord)[boundY];
            for (var y = 0; y < boundY; y++)
            {
                startEnd[y].start = float.PositiveInfinity;
                startEnd[y].end = float.NegativeInfinity;
            }

            void DrawEdge(Vector3 a, Vector3 aCoord, Vector3 b, Vector3 bCoord)
            {
                var v = b - a;
                var step = Math.Abs(v.X) >= Math.Abs(v.Y)
                    ? Math.Abs(v.X)
                    : Math.Abs(v.Y);
                v /= step;
                var vCoord = (bCoord - aCoord) / step;
                var i = 0;
                while (i <= step)
                {
                    var y = (int)a.Y - initY;
                    if (y >= 0 && y < boundY)
                    {
                        var x = (int)a.X;

                        var (start, startCoord, end, endCoord) = startEnd[y];

                        if (x < start)
                        {
                            start = x;
                            startCoord = aCoord;
                        }

                        if (x > end)
                        {
                            end = x;
                            endCoord = aCoord;
                        }

                        startEnd[y] = (start, startCoord, end, endCoord);
                    }

                    a += v;
                    aCoord += vCoord;
                    i++;
                }
            }

            DrawEdge(v0, new Vector3(1, 0, 0), v1, new Vector3(0, 1, 0));
            DrawEdge(v2, new Vector3(0, 0, 1), v1, new Vector3(0, 1, 0));
            DrawEdge(v2, new Vector3(0, 0, 1), v0, new Vector3(1, 0, 0));

            var bmpData = bitmap.LockBits(new Rectangle(initX, initY, boundX, boundY), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            var colorData = new int[boundX];
            var scan = bmpData.Scan0;

            for (var y = 0; y < boundY; y++, scan += bmpData.Stride)
            {
                var (start, barycenter, end, endCoord) = startEnd[y];
                if (end >= initX)
                {
                    var init = (int)Math.Clamp(start, 0, width - 1);
                    var startX = init - initX;
                    var endX = (int)Math.Clamp(end, 0, width - 1) - initX;
                    var xLen = endX - startX + 1;
                    Marshal.Copy(scan + sizeof(int) * startX, colorData, 0, xLen);

                    var vCoord = (endCoord - barycenter) / (end - start);
                    barycenter += (init - start) * vCoord;
                    for (var x = startX; x <= endX; x++, barycenter += vCoord)
                    {
                        var z = barycenter.X * v0.Z + barycenter.Y * v1.Z + barycenter.Z * v2.Z;
                        if (z > 0)
                        {
                            if (z < depthBuffer[y + initY, x + initX])
                            {
                                var perspectiveBarycenter = barycenter;
                                perspectiveBarycenter.X /= f0;
                                perspectiveBarycenter.Y /= f1;
                                perspectiveBarycenter.Z /= f2;

                                var color = getArgb(perspectiveBarycenter);
                                if ((color & 0xFF000000) == 0xFF000000)
                                {
                                    depthBuffer[y + initY, x + initX] = z;
                                    colorData[x - startX] = color;
                                }
                            }
                        }
                    }

                    Marshal.Copy(colorData, 0, scan + sizeof(int) * startX, xLen);
                }
            }

            bitmap.UnlockBits(bmpData);
        }

        private void FrameTimer_Tick(object sender, EventArgs e)
        {
            var now = Stopwatch.GetTimestamp();
            var elapsed = Stopwatch.GetElapsedTime(this.timestamp, now);
            this.timestamp = now;
            this.AdvanceFrame(elapsed);
        }

        private void Renderer_SizeChanged(object sender, EventArgs e) => this.UpdateSize();

        protected virtual void AdvanceFrame(TimeSpan elapsed)
        {
            if (elapsed > TimeSpan.Zero)
            {
                this.fps = 1 / elapsed.TotalSeconds;
            }

            this.Invalidate();
        }

        private void UpdateSize()
        {
            var size = this.ClientSize;
            this.width = Math.Max(size.Width, 1);
            this.height = Math.Max(size.Height, 1);
            this.buffer = new Bitmap(this.width, this.height);
            this.depthBuffer = new float[this.height, this.width];
            this.Camera.Width = this.width;
            this.Camera.Height = this.height;
        }
    }
}
