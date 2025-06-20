// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS2.Otacon
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Windows.Forms;

    internal partial class SpeechBubble : Form
    {
        private readonly Form parent;
        private readonly float scale;

        public SpeechBubble(Form parent, float scale)
        {
            this.InitializeComponent();
            this.EnableDrag();
            this.parent = parent;
            this.scale = scale;
            this.parent.Move += this.SpeechBubble_Move;
        }

        protected override void OnClosed(EventArgs e)
        {
            this.parent.Move -= this.SpeechBubble_Move;
            base.OnClosed(e);
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }

        private enum Side { Top, Bottom, Left, Right }

        private void Form_Paint(object sender, PaintEventArgs e)
        {
            var target = (PointF)this.PointToClient(this.parent.Location + this.parent.Size / 2);

            var radius = 28f;

            var scale = this.scale * 2;
            var bounds = new RectangleF(
                0,
                0,
                this.ClientSize.Width / scale,
                this.ClientSize.Height / scale);
            target = new PointF(target.X / scale, target.Y / scale);
            radius /= scale;

            using var bmp = new Bitmap((int)Math.Ceiling(bounds.Width), (int)Math.Ceiling(bounds.Height));
            using (var g = Graphics.FromImage(bmp))
            {
                using var path = new GraphicsPath();
                bounds.Width -= 1;
                bounds.Height -= 1;

                // Determine side for tail based on chatTarget
                var thisCenter = new PointF(bounds.Left + bounds.Width / 2f, bounds.Top + bounds.Height / 2f);
                var toTarget = new PointF(target.X - thisCenter.X, target.Y - thisCenter.Y);

                Side side;
                var absX = Math.Abs(toTarget.X);
                var absY = Math.Abs(toTarget.Y);
                if (absX > absY)
                {
                    bounds.Width -= radius;
                    if (toTarget.X > 0)
                    {
                        side = Side.Right;
                    }
                    else
                    {
                        bounds.X += radius;
                        side = Side.Left;
                    }
                }
                else
                {
                    bounds.Height -= radius;
                    if (toTarget.Y > 0)
                    {
                        side = Side.Bottom;
                    }
                    else
                    {
                        bounds.Y += radius;
                        side = Side.Top;
                    }
                }

                // Define bubble (clockwise)
                path.StartFigure();

                path.AddArc(bounds.Left, bounds.Top, radius, radius, 180, 90); // Top-left
                if (side == Side.Top)
                {
                    var center = Math.Min(Math.Max(target.X, bounds.Left + radius + radius / 2), bounds.Right - radius - radius / 2);
                    var point = Math.Min(Math.Max(target.X, bounds.Left + radius), bounds.Right - radius);
                    path.AddLine(center - radius / 2, bounds.Top, point, bounds.Top - radius);
                    path.AddLine(point, bounds.Top - radius, center + radius / 2, bounds.Top);
                }

                path.AddArc(bounds.Right - radius, bounds.Top, radius, radius, 270, 90); // Top-right
                if (side == Side.Right)
                {
                    var center = Math.Min(Math.Max(target.Y, bounds.Top + radius + radius / 2), bounds.Bottom - radius - radius / 2);
                    var point = Math.Min(Math.Max(target.Y, bounds.Top + radius), bounds.Bottom - radius);
                    path.AddLine(bounds.Right, center - radius / 2, bounds.Right + radius, point);
                    path.AddLine(bounds.Right + radius, point, bounds.Right, center + radius / 2);
                }

                path.AddArc(bounds.Right - radius, bounds.Bottom - radius, radius, radius, 0, 90); // Bottom-right
                if (side == Side.Bottom)
                {
                    var center = Math.Min(Math.Max(target.X, bounds.Left + radius + radius / 2), bounds.Right - radius - radius / 2);
                    var point = Math.Min(Math.Max(target.X, bounds.Left + radius), bounds.Right - radius);
                    path.AddLine(center + radius / 2, bounds.Bottom, point, bounds.Bottom + radius);
                    path.AddLine(point, bounds.Bottom + radius, center - radius / 2, bounds.Bottom);
                }

                path.AddArc(bounds.Left, bounds.Bottom - radius, radius, radius, 90, 90); // Bottom-left
                if (side == Side.Left)
                {
                    var center = Math.Min(Math.Max(target.Y, bounds.Top + radius + radius / 2), bounds.Bottom - radius - radius / 2);
                    var point = Math.Min(Math.Max(target.Y, bounds.Top + radius), bounds.Bottom - radius);
                    path.AddLine(bounds.Left, center + radius / 2, bounds.Left - radius, point);
                    path.AddLine(bounds.Left - radius, point, bounds.Left, center - radius / 2);
                }

                path.CloseFigure();

                g.SmoothingMode = SmoothingMode.None;
                g.FillPath(Brushes.White, path);
                g.DrawPath(Pens.Black, path);

                bounds.Inflate(-radius / 2, -radius / 2);
            }

            bounds = new RectangleF(
                bounds.X * scale,
                bounds.Y * scale,
                bounds.Width * scale,
                bounds.Height * scale);

            var state = e.Graphics.Save();
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;

            e.Graphics.DrawImage(bmp, this.ClientRectangle);

            var fullSize = e.Graphics.MeasureString(this.Text, this.Font, (int)bounds.Width);
            if (fullSize.Height > bounds.Height)
            {
                using var font = new Font(this.Font.FontFamily, this.Font.Size * (bounds.Width * bounds.Height) / (bounds.Width * fullSize.Height), this.Font.Style);
                e.Graphics.DrawString(this.Text, font, Brushes.Black, bounds);
            }
            else
            {
                e.Graphics.DrawString(this.Text, this.Font, Brushes.Black, bounds);
            }

            e.Graphics.Restore(state);
        }

        private void SpeechBubble_Move(object? sender, EventArgs e)
        {
            this.Invalidate();
        }
    }
}
