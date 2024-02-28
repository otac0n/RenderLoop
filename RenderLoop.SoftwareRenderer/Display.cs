namespace RenderLoop.SoftwareRenderer
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    public sealed partial class Display : Form
    {
        private const int TRIANGLE_POINTS = 3;

        public delegate int FragmentShader<T>(T[] vertices, Vector3 barycenter);

        private Bitmap buffer;
        private float[,] depthBuffer;
        private double fps;
        private bool sizeValid;

        public Display()
        {
            this.InitializeComponent();
        }

        public bool ShowFps { get; set; } = true;

        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.buffer != null)
            {
                e.Graphics.DrawImageUnscaled(this.buffer, Point.Empty);
            }
        }

        private void DrawFps(Graphics g, Bitmap buffer)
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
        /// <returns>An array containing the individual triangles.</returns>
        public static TVertex[] ExpandStrip<TVertex>(TVertex[] source)
        {
            if (source.Length < TRIANGLE_POINTS)
            {
                throw new ArgumentOutOfRangeException(nameof(source));
            }

            var vertices = new TVertex[TRIANGLE_POINTS * (source.Length - TRIANGLE_POINTS + 1)];

            vertices[0] = source[0];
            vertices[1] = source[1];
            vertices[2] = source[2];
            for (int i = 3, j = 3; i < source.Length; i++)
            {
                if (i % 2 == 0)
                {
                    vertices[j] = vertices[j - 3]; j++;
                    vertices[j] = vertices[j - 2]; j++;
                }
                else
                {
                    vertices[j] = vertices[j - 1]; j++;
                    vertices[j] = vertices[j - 3]; j++;
                }

                vertices[j++] = source[i];
            }

            return vertices;
        }

        /// <summary>
        /// Segments a strip of triangles and renders them with the provided function.
        /// </summary>
        /// <typeparam name="TVertex">The type of a triangle vertex.</typeparam>
        /// <param name="source">An array of vertices.</param>
        /// <param name="render">The function that will be called to render each triangle.</param>
        public static void DrawStrip<TVertex>(TVertex[] source, Action<TVertex[]> render)
        {
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

        public static FragmentShader<TVertex> MakeFragmentShader<TVertex>(Converter<TVertex, Vector2> getTextureCoordinate, Func<Vector2, Color> shader) =>
            (vertices, perspective) => shader(MapCoordinates(perspective, Array.ConvertAll(vertices, getTextureCoordinate))).ToArgb();

        public static FragmentShader<TVertex> MakeFragmentShader<TVertex>(Converter<TVertex, Vector3> getTextureCoordinate, Func<Vector3, Color> shader) =>
            (vertices, perspective) => shader(MapCoordinates(perspective, Array.ConvertAll(vertices, getTextureCoordinate))).ToArgb();

        public static FragmentShader<TVertex> MakeFragmentShader<TVertex>(Converter<TVertex, Vector2> getTextureCoordinate, Func<Vector2, int> shader) =>
            (vertices, perspective) => shader(MapCoordinates(perspective, Array.ConvertAll(vertices, getTextureCoordinate)));

        public static FragmentShader<TVertex> MakeFragmentShader<TVertex>(Converter<TVertex, Vector3> getTextureCoordinate, Func<Vector3, int> shader) =>
            (vertices, perspective) => shader(MapCoordinates(perspective, Array.ConvertAll(vertices, getTextureCoordinate)));

        public static Vector3 MapCoordinates(Vector3 barycenter, Vector3[] coordinates) =>
            (barycenter.X * coordinates[0] + barycenter.Y * coordinates[1] + barycenter.Z * coordinates[2]) / (barycenter.X + barycenter.Y + barycenter.Z);

        public static Vector2 MapCoordinates(Vector3 barycenter, Vector2[] coordinates) =>
            (barycenter.X * coordinates[0] + barycenter.Y * coordinates[1] + barycenter.Z * coordinates[2]) / (barycenter.X + barycenter.Y + barycenter.Z);

        public static void FillTriangle(Bitmap bitmap, float[,] depthBuffer, Vector4[] vertices, BackfaceCulling culling, Func<Vector3, int> getArgb)
        {
            var bitmapData = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                FillTriangle(bitmapData, depthBuffer, vertices, culling, getArgb);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }

        public static void FillTriangle(BitmapData bitmap, float[,] depthBuffer, Vector4[] vertices, BackfaceCulling culling, Func<Vector3, int> getArgb)
        {
            var width = bitmap.Width;
            var height = bitmap.Height;

            static Vector3 AsVector3(Vector4 v) => new(v.X, v.Y, v.Z);

            var v0 = AsVector3(vertices[0]);
            var v1 = AsVector3(vertices[1]);
            var v2 = AsVector3(vertices[2]);
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

            var colorData = new int[boundX];
            var scan = bitmap.Scan0 + bitmap.Stride * initY + sizeof(int) * initX;

            var p = Vector3.Zero;
            var f0 = v0.Z * Math.Abs(vertices[0].W);
            var f1 = v1.Z * Math.Abs(vertices[1].W);
            var f2 = v2.Z * Math.Abs(vertices[2].W);
            for (var y = 0; y < boundY; y++, scan += bitmap.Stride)
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
        }

        public static void FillTriangleA(BitmapData bitmap, float[,] depthBuffer, Vector4[] vertices, BackfaceCulling culling, Func<Vector3, int> getArgb)
        {
            var width = bitmap.Width;
            var height = bitmap.Height;

            static Vector3 AsVector3(Vector4 v) => new(v.X, v.Y, v.Z);

            var v0 = AsVector3(vertices[0]);
            var v1 = AsVector3(vertices[1]);
            var v2 = AsVector3(vertices[2]);
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

                int i;
                Vector3 p, pCoord;
                for (i = 0, p = a, pCoord = aCoord; i <= step; i++, p = a + v * i, pCoord = aCoord + vCoord * i)
                {
                    var y = (int)p.Y - initY;
                    if (y >= 0 && y < boundY)
                    {
                        var x = (int)p.X;

                        var (start, startCoord, end, endCoord) = startEnd[y];

                        if (x < start)
                        {
                            start = x;
                            startCoord = pCoord;
                        }

                        if (x > end)
                        {
                            end = x;
                            endCoord = pCoord;
                        }

                        startEnd[y] = (start, startCoord, end, endCoord);
                    }
                }
            }

            DrawEdge(v0, new Vector3(1, 0, 0), v1, new Vector3(0, 1, 0));
            DrawEdge(v2, new Vector3(0, 0, 1), v1, new Vector3(0, 1, 0));
            DrawEdge(v2, new Vector3(0, 0, 1), v0, new Vector3(1, 0, 0));

            var colorData = new int[boundX];
            var scan = bitmap.Scan0 + bitmap.Stride * initY + sizeof(int) * initX;

            var f0 = v0.Z * Math.Abs(vertices[0].W);
            var f1 = v1.Z * Math.Abs(vertices[1].W);
            var f2 = v2.Z * Math.Abs(vertices[2].W);
            for (var y = 0; y < boundY; y++, scan += bitmap.Stride)
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
        }

        private void Renderer_SizeChanged(object sender, EventArgs e) => this.sizeValid = false;

        public void PaintFrame(TimeSpan elapsed, Action<Graphics, Bitmap, float[,]> draw)
        {
            if (elapsed > TimeSpan.Zero)
            {
                this.fps = 1 / elapsed.TotalSeconds;
            }

            if (!this.sizeValid)
            {
                this.UpdateSize();
            }

            using (var g = Graphics.FromImage(this.buffer))
            {
                g.Clear(this.BackColor);
                ClearDepthBuffer(this.depthBuffer);
                draw(g, this.buffer, this.depthBuffer);
                this.DrawFps(g, this.buffer);
            }

            this.Invalidate();
        }

        private void UpdateSize()
        {
            var size = this.ClientSize;
            var width = Math.Max(size.Width, 1);
            var height = Math.Max(size.Height, 1);
            if (width != this.buffer?.Width ||
                height != this.buffer?.Height)
            {
                this.buffer = new Bitmap(width, height);
                this.depthBuffer = new float[height, width];
            }

            this.sizeValid = true;
        }
    }
}
