// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    internal static class ControlExtensions
    {
        public static void EnableDrag(this Form form, Action? onBegin = null)
        {
            ArgumentNullException.ThrowIfNull(form);

            var mouseDownLocation = Point.Empty;
            var dragging = false;

            void Attach(Control control)
            {
                if (control is not (ContainerControl or ScrollableControl or Label or PictureBox))
                {
                    return;
                }

                control.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        dragging = true;
                        mouseDownLocation = form.PointToClient(control.PointToScreen(e.Location));
                        onBegin?.Invoke();
                    }
                };

                control.MouseMove += (s, e) =>
                {
                    if (dragging && e.Button == MouseButtons.Left)
                    {
                        var newMousePosition = Control.MousePosition;
                        var newLocation = new Point(
                            newMousePosition.X - mouseDownLocation.X,
                            newMousePosition.Y - mouseDownLocation.Y);

                        form.Location = newLocation;
                    }
                };

                control.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        dragging = false;
                    }
                };

                foreach (Control child in control.Controls)
                {
                    Attach(child);
                }
            }

            form.Load += (_, _) => Attach(form);
        }

        public static Rectangle ClampToBounds(this Rectangle rect, Rectangle bounds)
        {
            rect.X = Math.Max(Math.Min(rect.Left, bounds.Right - rect.Width), bounds.Left);
            rect.Y = Math.Max(Math.Min(rect.Top, bounds.Bottom - rect.Height), bounds.Top);
            return rect;
        }
    }
}
