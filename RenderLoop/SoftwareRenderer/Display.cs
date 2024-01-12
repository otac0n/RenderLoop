namespace RenderLoop.SofwareRenderer
{
    using System;
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
        private Camera camera = new Camera();
        private long timestamp;

        public Display()
        {
            this.InitializeComponent();
            this.UpdateSize();
            this.timestamp = Stopwatch.GetTimestamp();
        }

        protected Camera Camera => this.camera;

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

        protected abstract void DrawScene(Graphics g, Bitmap buffer, float[,] depthBuffer);

        protected static void DrawShape<TPoint>(int[] shape, TPoint[] points, Action<TPoint[]> render) =>
            DrawShape(shape, i => points[i], (_, points) => render(points));

        protected static void DrawShape<TShape, TPoint>(TShape[] shape, Func<TShape, TPoint> getPoint, Action<TShape[], TPoint[]> render)
        {
            const int TRIANGLE_POINTS = 3;
            var swath = new TShape[TRIANGLE_POINTS];
            var subject = new TPoint[TRIANGLE_POINTS];
            for (var i = 0; i <= shape.Length - TRIANGLE_POINTS; i++)
            {
                if (i % 2 == 0)
                {
                    for (var j = 0; j < TRIANGLE_POINTS; j++)
                    {
                        swath[j] = shape[i + j];
                        subject[j] = getPoint(swath[j]);
                    }
                }
                else
                {
                    for (var j = 0; j < TRIANGLE_POINTS; j++)
                    {
                        swath[j] = shape[i + (TRIANGLE_POINTS - 1 - j)];
                        subject[j] = getPoint(swath[j]);
                    }
                }

                render(swath, subject);
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

        public static void DrawWireFrame(Graphics g, Vector3[] vertices)
        {
            var p = Array.ConvertAll(vertices, v => new PointF(v.X, v.Y));
            g.DrawLine(Pens.White, p[0], p[1]);
            g.DrawLine(Pens.White, p[2], p[1]);
            g.DrawLine(Pens.White, p[2], p[0]);
        }

        public static Vector3 MapCoordinates(Vector3 barycenter, Vector3[] coordinates) =>
            barycenter.X * coordinates[0] + barycenter.Y * coordinates[1] + barycenter.Z * coordinates[2];

        public static void FillTriangle(Bitmap bitmap, float[,] depthBuffer, Vector3[] vertices, Color color) =>
            FillTriangle(bitmap, depthBuffer, vertices, (_, _) => color);

        public static void FillTriangle(Bitmap bitmap, float[,] depthBuffer, Vector3[] vertices, Func<Vector3, float[], Color> getColor) =>
            FillTriangle(bitmap, depthBuffer, vertices, (b, z) => getColor(b, z).ToArgb());

        public static void FillTriangle(Bitmap bitmap, float[,] depthBuffer, Vector3[] vertices, Func<Vector3, float[], int> getArgb)
        {
            var width = bitmap.Width;
            var height = bitmap.Height;
            var black = Color.Black.ToArgb();

            var v0 = vertices[0];
            var v1 = vertices[1];
            var v2 = vertices[2];
            var min = Vector3.Min(Vector3.Min(v0, v1), v2);
            var max = Vector3.Max(Vector3.Max(v0, v1), v2);

            if (float.IsNaN(min.X) || float.IsNaN(min.Y) || max.Z < 0)
            {
                return;
            }

            var area = EdgeFunction(v0, v1, v2);
            if (area <= 0)
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

                    float w0, w1, w2;
                    if ((w0 = EdgeFunction(v1, v2, p)) >= 0 &&
                        (w1 = EdgeFunction(v2, v0, p)) >= 0 &&
                        (w2 = EdgeFunction(v0, v1, p)) >= 0)
                    {
                        if (startX < 0)
                        {
                            startX = x;
                            Marshal.Copy(scan + sizeof(int) * startX, colorData, startX, boundX - startX);
                        }

                        var z = w0 * v0.Z + w1 * v1.Z + w2 * v2.Z;
                        if (z > 0)
                        {
                            z /= area;
                            if (z < depthBuffer[y + initY, x + initX])
                            {
                                depthBuffer[y + initY, x + initX] = z;
                                colorData[x] = getArgb(GetBarycentricCoordinates(v0, v1, v2, p with { Z = z }), [v0.Z, v1.Z, v2.Z, z]);
                            }
                        }
                        //else
                        //{
                        //    colorData[x] = black;
                        //    depthBuffer[y + initY, x + initX] = 0;
                        //}
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

        private static Vector3 GetBarycentricCoordinates(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 p)
        {
            var u = v1 - v0;
            var v = v2 - v0;
            var w = p - v0;

            var vw = Vector3.Cross(v, w);
            var vu = Vector3.Cross(v, u);

            var q0 = Vector3.Dot(vw, vu);
            if (q0 < 0)
            {
                return Vector3.Zero;
            }

            var uw = Vector3.Cross(u, w);
            var uv = Vector3.Cross(u, v);

            var q1 = Vector3.Dot(uw, uv);
            if (q1 < 0)
            {
                return Vector3.Zero;
            }

            var l = uv.Length();
            var b1 = vw.Length() / l;
            var b2 = uw.Length() / l;
            if ((b1 > 1) || (b2 > 1))
            {
                return Vector3.Zero;
            }

            var rem = b1 + b2;
            if (rem > 1)
            {
                return Vector3.Zero;
            }

            return new Vector3((1 - rem), b1, b2);
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
            this.Invalidate();
        }

        private void UpdateSize()
        {
            var size = this.ClientSize;
            this.width = Math.Max(size.Width, 1);
            this.height = Math.Max(size.Height, 1);
            this.buffer = new Bitmap(this.width, this.height);
            this.depthBuffer = new float[this.height, this.width];
            this.camera.Width = this.width;
            this.camera.Height = this.height;
        }
    }
}
