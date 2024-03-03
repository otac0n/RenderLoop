// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

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
    using Microsoft.Extensions.Logging;
    using RenderLoop.Input;
    using RenderLoop.SoftwareRenderer;

    internal partial class FlyBy : GameLoop
    {
        private readonly Display display;
        private readonly ControlChangeTracker controlChangeTracker;
        private readonly ILogger<FlyBy> logger;
        private readonly ZipArchive archive;
        private readonly Camera Camera = new();
        private Task loading;

        private static readonly Size BakeSize = new(8192, 8192);
        private static readonly Size TextureSize = new(8128, 5764);
        private static readonly Size MapSize = new(8192, 8192);
        private int skip = 32;
        private int[,] colorMap;
        private float[,] heightMap;
        private int[,] normalMap;

        public FlyBy(Display display, ControlChangeTracker controlChangeTracker, Program.Options options, IServiceProvider serviceProvider, ILogger<FlyBy> logger)
            : base(display)
        {
            this.display = display;
            this.controlChangeTracker = controlChangeTracker;
            this.logger = logger;
            this.archive = serviceProvider.GetRequiredKeyedService<ZipArchive>(options.File);
        }

        protected override void Initialize()
        {
            Bitmap Resize(Image image, Size size) => new Bitmap(image, size);
            T[,] Remap<T>(Bitmap bitmap, Func<int, int, int, T> getValue)
            {
                LogMessages.RemappingResource(this.logger);
                var remapped = new T[bitmap.Width, bitmap.Height];
                var bmp = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    var single = new int[1];
                    for (var y = 0; y < bmp.Height; y++)
                    {
                        for (var x = 0; x < bmp.Width; x++)
                        {
                            Marshal.Copy(bmp.Scan0 + y * bmp.Stride + x * sizeof(int), single, 0, single.Length);
                            remapped[bmp.Width - 1 - x, y] = getValue(x, y, single[0]);
                        }
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bmp);
                }

                LogMessages.RemappingDone(this.logger);
                return remapped;
            }

            Image LoadImage(string path)
            {
                LogMessages.LoadingImage(this.logger, path);
                return Image.FromStream(this.archive.GetEntry(path)!.Open());
            }

            this.loading = Task.Factory.StartNew(() =>
            {
                this.heightMap = Remap(Resize(LoadImage("Raw_Bakes/Final Height.png"), MapSize), (x, y, color) => Color.FromArgb(color).R / 256f * MapSize.Width / 32);
                this.colorMap = Remap(Resize(LoadImage("Textured/ME_Terrain_albedo.png"), MapSize), (x, y, color) => color);
                this.normalMap = Remap(Resize(LoadImage("Raw_Bakes/Normal Map.png"), MapSize), (x, y, color) => color);
            });

            this.Camera.Position = new Vector3(MapSize.Width / 2, MapSize.Height, MapSize.Width / 4);
            this.Camera.Up = new Vector3(0, 0, 1);
            this.Camera.Direction = new Vector3(MapSize.Width / 2, 0, 0) - this.Camera.Position;
            this.Camera.FarPlane = 1024;
        }

        protected sealed override void AdvanceFrame(TimeSpan elapsed)
        {
            var moveVector = Vector2.Zero;
            var right = 0.0;
            var up = 0.0;

            var bindings = new Bindings<Action<double>>();
            bindings.BindCurrent(
                [(c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Name == "X", v => (v - 0.5) * 2)],
                v => moveVector.X += (float)v);
            bindings.BindCurrent(
                [(c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Name == "Y", v => (v - 0.5) * 2)],
                v => moveVector.Y += (float)v);
            bindings.BindCurrent(
                [(c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Name == "Ry", v => (v - 0.5) * 2)],
                v => up -= v);
            bindings.BindCurrent(
                [(c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Name == "Rx", v => (v - 0.5) * 2)],
                v => right -= v);

            bindings.BindEach(
                [(c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Name == "Button 4")],
                v => this.skip = Math.Max(this.skip / 2, 1));
            bindings.BindEach(
                [(c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Name == "Button 5")],
                v => this.skip = Math.Min(this.skip * 2, MapSize.Width / 2));

            this.controlChangeTracker.ProcessChanges(bindings);

            var moveLength = moveVector.Length();
            if (moveLength > 0.1)
            {
                var scale = moveLength >= 1
                    ? 1f / moveLength
                    : (moveLength - 0.1f) / (0.9f * moveLength);

                moveVector *= scale;
                this.Camera.Position += (this.Camera.Right * moveVector.X - this.Camera.Direction * moveVector.Y) * (float)(elapsed.TotalSeconds * MapSize.Width / 10);
            }

            if (Math.Abs(right) > 0.1)
            {
                right *= elapsed.TotalSeconds / 10 * Math.Tau;

                var (sin, cos) = Math.SinCos(right);
                var v = this.Camera.Direction;
                var k = this.Camera.Up;
                this.Camera.Direction = v * (float)cos + Vector3.Cross(k, v) * (float)sin + k * Vector3.Dot(k, v) * (float)(1 - cos);
            }

            if (Math.Abs(up) > 0.1)
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
            this.display.PaintFrame(elapsed, (Graphics g, Bitmap buffer, float[,] depthBuffer) =>
            {
                this.Camera.Width = buffer.Width;
                this.Camera.Height = buffer.Height;

                if (this.heightMap != null)
                {
                    var w = this.heightMap.GetLength(0);
                    var h = this.heightMap.GetLength(1);

                    var white = Color.White.ToArgb();
                    Func<int, int, int> getColor;
                    if (this.colorMap != null)
                    {
                        var tw = this.colorMap.GetLength(0);
                        var th = this.colorMap.GetLength(1);
                        var q = (TextureSize.Width - TextureSize.Height) / 2f * tw / TextureSize.Height;
                        var sx = (double)tw / w;
                        var sy = (double)th * TextureSize.Width / (h * TextureSize.Height);

                        getColor = (x, y) =>
                        {
                            var tx = (int)(x * sx);
                            var ty = (int)(y * sy - q);
                            if (tx >= 0 && tx < tw &&
                                ty >= 0 && ty < th)
                            {
                                return this.colorMap[tx, ty];
                            }

                            return white;
                        };
                    }
                    else
                    {
                        getColor = (_, _) => white;
                    }

                    Vector3 GetPoint(int x, int y) => new(x, y, this.heightMap[x, y]);

                    var bitmapData = buffer.LockBits(new Rectangle(Point.Empty, buffer.Size), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

                    for (var y = 0; y < h - this.skip; y += this.skip)
                    {
                        var topLeft = this.Camera.TransformToScreenSpace(GetPoint(0, y));
                        var bottomLeft = this.Camera.TransformToScreenSpace(GetPoint(0, y + this.skip));
                        for (var x = this.skip; x < w; x += this.skip)
                        {
                            var topRight = this.Camera.TransformToScreenSpace(GetPoint(x, y));
                            var bottomRight = this.Camera.TransformToScreenSpace(GetPoint(x, y + this.skip));

                            var c = getColor(x - this.skip, y);
                            Display.FillTriangle(bitmapData, depthBuffer, [topLeft, topRight, bottomLeft], BackfaceCulling.None, _ => c);
                            Display.FillTriangle(bitmapData, depthBuffer, [topRight, bottomRight, bottomLeft], BackfaceCulling.None, _ => c);

                            topLeft = topRight;
                            bottomLeft = bottomRight;
                        }
                    }

                    buffer.UnlockBits(bitmapData);
                }

                using (var textBrush = new SolidBrush(this.display.ForeColor))
                {
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

        private static partial class LogMessages
        {
            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Loading '{ImagePath}'...")]
            public static partial void LoadingImage(ILogger logger, string imagePath);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Remapping...")]
            public static partial void RemappingResource(ILogger logger);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Done.")]
            public static partial void RemappingDone(ILogger logger);
        }
    }
}
