namespace RenderLoop.Demo.MiddleEarth
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO.Compression;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using RenderLoop.Input;
    using RenderLoop.SoftwareRenderer;

    internal class FlyBy : GameLoop
    {
        private readonly Display display;
        private readonly ControlChangeTracker controlChangeTracker;
        private readonly ZipArchive archive;
        private readonly Camera Camera = new();
        private Task loading;

        private static readonly Size BakeSize = new(8192, 8192);
        private static readonly Size TextureSize = new(8128, 5764);
        private static readonly Size MapSize = new(256, 256);
        private Color[,] colorMap;
        private float[,] heightMap;
        private Color[,] normalMap;

        public FlyBy(Display display, ControlChangeTracker controlChangeTracker, Program.Options options, IServiceProvider serviceProvider)
            : base(display)
        {
            this.display = display;
            this.controlChangeTracker = controlChangeTracker;
            this.archive = serviceProvider.GetRequiredKeyedService<ZipArchive>(options.File);
        }

        protected override void Initialize()
        {
            Bitmap Resize(Image image, Size size) => new Bitmap(image, size);
            T[,] Remap<T>(Bitmap bitmap, Func<int, int, Color, T> getValue)
            {
                var remapped = new T[bitmap.Width, bitmap.Height];
                var bmp = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    var single = new byte[4];
                    for (var y = 0; y < bmp.Height; y++)
                    {
                        for (var x = 0; x < bmp.Width; x++)
                        {
                            Marshal.Copy(bmp.Scan0 + y * bmp.Stride + x * single.Length, single, 0, single.Length);
                            remapped[x, y] = getValue(x, y, Color.FromArgb(single[3], single[2], single[1], single[0]));
                        }
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bmp);
                }

                return remapped;
            }

            this.loading = Task.Factory.StartNew(() =>
            {
                Image LoadImage(string path) => Image.FromStream(this.archive.GetEntry(path)!.Open());

                this.heightMap = Remap(Resize(LoadImage("Raw_Bakes/Final Height.png"), MapSize), (x, y, color) => color.R / 256f * MapSize.Width / 32);

                this.colorMap = Remap(Resize(LoadImage("Textured/ME_Terrain_albedo.png"), MapSize), (x, y, color) => color);
                this.normalMap = Remap(Resize(LoadImage("Raw_Bakes/Normal Map.png"), MapSize), (x, y, color) => color);
            });

            this.Camera.Position = new Vector3(0, MapSize.Width / 2, MapSize.Width / 8);
            this.Camera.Up = new Vector3(0, 0, 1);
            this.Camera.Direction = new Vector3(512, 512, 0) - this.Camera.Position;
            this.Camera.FarPlane = 1024;
        }

        protected sealed override void AdvanceFrame(TimeSpan elapsed)
        {
            var moveVector = Vector3.Zero;
            var right = 0.0;
            var up = 0.0;

            var bindings = new Bindings<Action<double>>();
            bindings.BindCurrent(
                c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Name == "X",
                v => moveVector += this.Camera.Right * (float)((v - 0.5) * 2));
            bindings.BindCurrent(
                c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Name == "Y",
                v => moveVector += this.Camera.Direction * -(float)((v - 0.5) * 2));
            bindings.BindCurrent(
                c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Name == "Ry",
                v => up += (0.5 - v) * 2);
            bindings.BindCurrent(
                c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Name == "Rx",
                v => right += (0.5 - v) * 2);

            this.controlChangeTracker.ProcessChanges(bindings);

            if (moveVector != Vector3.Zero)
            {
                if (moveVector.LengthSquared() > 1)
                {
                    moveVector = Vector3.Normalize(moveVector);
                }

                moveVector *= (float)elapsed.TotalSeconds * MapSize.Width / 10;
                this.Camera.Position += moveVector;
            }

            if (right != 0)
            {
                right *= elapsed.TotalSeconds / 10 * Math.Tau;

                var (sin, cos) = Math.SinCos(right);
                var v = this.Camera.Direction;
                var k = this.Camera.Up;
                this.Camera.Direction = v * (float)cos + Vector3.Cross(k, v) * (float)sin + k * Vector3.Dot(k, v) * (float)(1 - cos);
            }

            if (up != 0)
            {
                up *= elapsed.TotalSeconds / 10 * Math.Tau;

                var (sin, cos) = Math.SinCos(up);
                var v = this.Camera.Direction;
                var k = this.Camera.Right;
                this.Camera.Direction = v * (float)cos + Vector3.Cross(k, v) * (float)sin + k * Vector3.Dot(k, v) * (float)(1 - cos);
            }
        }

        protected override void DrawScene(TimeSpan elapsed)
        {
            Vector3[] tags = [
                Vector3.Zero,
                new Vector3(MapSize.Width, 0, 0),
                new Vector3(0, MapSize.Height, 0),
            ];

            this.display.PaintFrame(elapsed, (Graphics g, Bitmap buffer, float[,] depthBuffer) =>
            {
                this.Camera.Width = buffer.Width;
                this.Camera.Height = buffer.Height;

                if (this.heightMap != null)
                {
                    var w = this.heightMap.GetLength(0);
                    var h = this.heightMap.GetLength(1);

                    Color GetColor(int x, int y)
                    {
                        if (this.colorMap != null)
                        {
                            var tw = this.colorMap.GetLength(0);
                            var th = this.colorMap.GetLength(1);
                            var q = (TextureSize.Width - TextureSize.Height) / 2f * tw / TextureSize.Height;

                            var tx = (int)Math.Floor((double)x / w * tw);
                            var ty = (int)Math.Floor((double)y / h * TextureSize.Width / TextureSize.Height * th - q); //
                            if (tx > 0 && ty > 0 &&
                                tx < tw && ty < th)
                            {
                                return this.colorMap[tx, ty];
                            }
                        }

                        return Color.White;
                    }


                    for (var y = 0; y < h - 1; y++)
                    {
                        for (var x = 0; x < w - 1; x++)
                        {
                            using var pen = new Pen(GetColor(x, y));

                            var topLeft = this.Camera.TransformToScreenSpace(new Vector3(x, y, this.heightMap[x, y]));
                            var topRight = this.Camera.TransformToScreenSpace(new Vector3(x + 1, y, this.heightMap[x + 1, y]));
                            var bottomLeft = this.Camera.TransformToScreenSpace(new Vector3(x, y + 1, this.heightMap[x, y + 1]));

                            if (topLeft.X > 0 && topLeft.X < buffer.Width &&
                                topLeft.Y > 0 && topLeft.Y < buffer.Height && topLeft.Z > 0)
                            {
                                if (topRight.X > 0 && topRight.X < buffer.Width &&
                                    topRight.Y > 0 && topRight.Y < buffer.Height && topRight.Z > 0)
                                    g.DrawLine(pen, topLeft.X, topLeft.Y, topRight.X, topRight.Y);
                                if (bottomLeft.X > 0 && bottomLeft.X < buffer.Width &&
                                    bottomLeft.Y > 0 && bottomLeft.Y < buffer.Height && bottomLeft.Z > 0)
                                    g.DrawLine(pen, topLeft.X, topLeft.Y, bottomLeft.X, bottomLeft.Y);
                            }
                        }
                    }
                }

                using (var textBrush = new SolidBrush(this.display.ForeColor))
                {
                    foreach (var tag in tags)
                    {
                        var point = this.Camera.TransformToScreenSpace(tag);
                        if (point.Z > 0)
                        {
                            g.DrawString(tag.ToString(), this.display.Font, textBrush, point.X, point.Y - this.display.Font.Size / 2);
                        }
                    }

                    var status = this.loading.Status;
                    switch (status)
                    {
                        case TaskStatus.RanToCompletion:
                            break;

                        default:
                            var statusText = status switch
                            {
                                TaskStatus.Running => "Loading...",
                                TaskStatus.Faulted => this.loading.Exception.Message,
                                _ => status.ToString(),
                            };

                            g.DrawString(statusText, this.display.Font, textBrush, PointF.Empty);

                            break;
                    }
                }
            });
        }
    }
}
