// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.SoftwareRenderer
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    public sealed partial class Display : Form
    {
        private CooperativeIdleApplicationContext? cooperativeIdleContext;

        private Bitmap buffer;
        private float[,] depthBuffer;
        private double fps;
        private bool sizeValid;

        public Display()
        {
            this.InitializeComponent();
        }

        public bool ShowFps { get; set; } = true;

        public CooperativeIdleApplicationContext? CooperativeIdleContext
        {
            get => this.cooperativeIdleContext;
            set
            {
                if (this.cooperativeIdleContext != value)
                {
                    this.cooperativeIdleContext?.RemoveDisplay(this);
                    this.cooperativeIdleContext = value;
                    this.cooperativeIdleContext?.AddDisplay(this);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            this.CooperativeIdleContext = null;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.CooperativeIdleContext?.PendingOperations <= 0)
            {
                return;
            }

            if (this.buffer != null)
            {
                e.Graphics.DrawImageUnscaled(this.buffer, Point.Empty);
            }

            this.CooperativeIdleContext?.CompleteOperation();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
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

            this.CooperativeIdleContext?.AddPendingOperation();
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
