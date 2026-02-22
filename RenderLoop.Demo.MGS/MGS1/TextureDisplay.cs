// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS1
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using ImageMagick;
    using Microsoft.Extensions.DependencyInjection;

    internal partial class TextureDisplay : Form
    {
        private readonly VirtualImageList<string> textureDisplay;
        private MouseMoveFilter? filter;

        public TextureDisplay(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<Program.Options>();
            var stageDir = serviceProvider.GetRequiredKeyedService<StageDirVirtualFileSystem>((WellKnownPaths.AllDataBin, WellKnownPaths.CD1Path, WellKnownPaths.StageDirPath));

            this.InitializeComponent();
            this.textureDisplay = new VirtualImageList<string>(
                stageDir.Directory.EnumerateFiles("", "*.pcx", SearchOption.AllDirectories),
                file =>
                {
                    using var textureFile = stageDir.File.OpenRead(file);
                    return Task.FromResult(new MagickImage(textureFile).ToBitmap());
                },
                InterpolationMode.NearestNeighbor)
            {
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location = Point.Empty,
                Width = this.ClientSize.Width,
            };
            this.Controls.Add(this.textureDisplay);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.filter = new MouseMoveFilter(
                this,
                this.textureDisplay,
                this.toolTip);

            Application.AddMessageFilter(this.filter);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (this.filter != null)
            {
                Application.RemoveMessageFilter(this.filter);
                this.filter = null;
            }

            base.OnFormClosed(e);
        }

        protected override Point ScrollToControl(Control activeControl)
        {
            return this.DisplayRectangle.Location;
        }

        private class MouseMoveFilter(
            Control parent,
            VirtualImageList<string> textureDisplay,
            ToolTip toolTip) : IMessageFilter
        {
            private readonly Control parent = parent;
            private readonly VirtualImageList<string> textureDisplay = textureDisplay;
            private readonly ToolTip toolTip = toolTip;

            const int WM_MOUSEMOVE = 0x0200;

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg == WM_MOUSEMOVE)
                {
                    var client = this.textureDisplay.PointToClient(Cursor.Position);

                    var caption = string.Empty;
                    if (this.textureDisplay.ClientRectangle.Contains(client) &&
                        this.textureDisplay.HitTest(client, out var hit))
                    {
                        caption = hit;
                    }

                    this.toolTip.SetToolTip(this.textureDisplay, caption);
                }

                return false;
            }
        }
    }
}
