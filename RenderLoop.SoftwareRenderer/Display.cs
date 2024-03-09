// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.SoftwareRenderer
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    public sealed partial class Display : Form
    {
        private static ILogger<Display> StaticLogger = new Logger<Display>(NullLoggerFactory.Instance);
        private static int ToPaint;

        private Bitmap buffer;
        private float[,] depthBuffer;
        private ILogger<Display> logger;
        private double fps;
        private bool sizeValid;

        static Display()
        {
            Application.Idle += Application_Idle;
        }

        public Display(ILogger<Display> logger)
        {
            this.logger = logger;
            this.InitializeComponent();
            Idle += this.Display_Idle;
        }

        private static event Action Idle;

        public event Action Tick;

        public bool ShowFps { get; set; } = true;

        private static void Application_Idle(object? sender, EventArgs e)
        {
            StaticLogger.LogInformation("Rescuing from idle...");
            var toPaint = Interlocked.Exchange(ref ToPaint, 0);
            if (toPaint > 0)
            {
                StaticLogger.LogInformation("Unpainted: {ToPaint}", toPaint);
            }

            Idle?.Invoke();
        }

        private void Display_Idle() => this.Tick?.Invoke();

        protected override void OnClosed(EventArgs e)
        {
            Idle -= this.Display_Idle;
            base.OnClosed(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (ToPaint <= 0)
            {
                logger.LogInformation("Skipping {Form}, To Paint: {ToPaint}", this.Text, ToPaint);
                return;
            }

            logger.LogInformation("Painting {Form}...", this.Text);
            if (this.buffer != null)
            {
                e.Graphics.DrawImageUnscaled(this.buffer, Point.Empty);
            }

            var toPaint = Interlocked.Decrement(ref ToPaint);
            logger.LogInformation("To Paint: {ToPaint}", toPaint);

            if (toPaint <= 0)
            {
                ToPaint = 0;
                logger.LogInformation("Firing Idle Handler...");
                Idle?.Invoke();
            }
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

            var toPaint = Interlocked.Increment(ref ToPaint);
            logger.LogInformation("Invalidating {Form}, To Paint: {ToPaint}", this.Text, toPaint);
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
