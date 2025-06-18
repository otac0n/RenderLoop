// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    internal static class ControlExtensions
    {
        public static void EnableDrag(this Form form)
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
                    }
                };

                control.MouseMove += (s, e) =>
                {
                    if (dragging && e.Button == MouseButtons.Left)
                    {
                        var newMousePosition = Control.MousePosition;
                        form.Location = new Point(
                            newMousePosition.X - mouseDownLocation.X,
                            newMousePosition.Y - mouseDownLocation.Y);
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
    }
}
