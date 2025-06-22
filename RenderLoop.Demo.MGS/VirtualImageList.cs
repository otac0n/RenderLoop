// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    public class VirtualImageList<T> : Control
        where T : notnull
    {
        private const int ImageSize = 128;
        private readonly List<T> items;
        private readonly Func<T, Task<Bitmap>> getImage;
        private readonly InterpolationMode interpolation;
        private readonly Dictionary<T, Task<Bitmap>> images = [];
        private readonly SemaphoreSlim semaphore = new(5);

        public VirtualImageList(IEnumerable<T> items, Func<T, Task<Bitmap>> getImage, InterpolationMode interpolation = InterpolationMode.Default)
        {
            this.items = [.. items];
            this.getImage = getImage;
            this.interpolation = interpolation;
            this.DoubleBuffered = true;
        }

        public bool HitTest(MouseEventArgs e, [NotNullWhen(true)] out T? hit)
        {
            var columns = Math.Max(1, this.ClientSize.Width / ImageSize);
            var col = e.X / ImageSize;
            var row = e.Y / ImageSize;
            var index = row * columns + col;
            if (index >= 0 && index < this.items.Count)
            {
                hit = this.items[index];
                return true;
            }

            hit = default;
            return false;
        }

        protected override void OnAutoSizeChanged(EventArgs e)
        {
            base.OnAutoSizeChanged(e);
            this.Resize();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            this.Resize();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            this.Resize();
        }

        private void Resize()
        {
            if (!this.AutoSize)
            {
                return;
            }

            var width = Math.Max(this.ClientSize.Width, ImageSize);
            var columns = Math.Max(1, width / ImageSize);
            var rows = (this.items.Count + columns - 1) / columns;
            var height = rows * ImageSize;
            var size = new Size(width, height);
            if (this.ClientSize != size)
            {
                this.ClientSize = size;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var clip = e.ClipRectangle;
            var width = this.ClientSize.Width;
            var columns = Math.Max(1, width / ImageSize);

            var rowStart = clip.Top / ImageSize;
            var rowEnd = (clip.Bottom + ImageSize - 1) / ImageSize;

            void ZoomImage(Bitmap bmp, RectangleF destRect)
            {
                var scale = Math.Min(destRect.Width / bmp.Width, destRect.Height / bmp.Height);
                var drawWidth = (int)(bmp.Width * scale);
                var drawHeight = (int)(bmp.Height * scale);

                var dx = destRect.X + (destRect.Width - drawWidth) / 2;
                var dy = destRect.Y + (destRect.Height - drawHeight) / 2;

                var state = e.Graphics.Save();
                e.Graphics.InterpolationMode = this.interpolation;
                e.Graphics.DrawImage(bmp, new RectangleF(dx, dy, drawWidth, drawHeight));
                e.Graphics.Restore(state);
            }

            for (var row = rowStart; row < rowEnd; row++)
            {
                for (var col = 0; col < columns; col++)
                {
                    var index = row * columns + col;
                    if (index >= this.items.Count)
                    {
                        return;
                    }

                    var task = this.GetBitmapAsync(index);
                    if (!task.IsCompletedSuccessfully || !(task.Result is Bitmap bmp))
                    {
                        continue;
                    }

                    var destRect = new Rectangle(col * ImageSize, row * ImageSize, ImageSize, ImageSize);

                    ZoomImage(bmp, destRect);
                }
            }
        }

        private Task<Bitmap> GetBitmapAsync(int index)
        {
            var item = this.items[index];
            Task<Bitmap> task;
            lock (this.images)
            {
                if (!this.images.TryGetValue(item, out task!))
                {
                    async Task<Bitmap> GetAsync(T item)
                    {
                        await this.semaphore.WaitAsync().ConfigureAwait(false);
                        try
                        {
                            var bmp = await this.getImage(item).ConfigureAwait(true);
                            var columns = Math.Max(1, this.ClientSize.Width / ImageSize);
                            var row = index / columns;
                            var col = index % columns;
                            this.Invalidate(new Rectangle(col * ImageSize, row * ImageSize, ImageSize, ImageSize));
                            return bmp;
                        }
                        finally
                        {
                            this.semaphore.Release();
                        }
                    }

                    this.images[item] = task = GetAsync(item);
                }
            }

            return task;
        }
    }
}
