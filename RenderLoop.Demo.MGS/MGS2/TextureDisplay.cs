// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS2
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;
    using Microsoft.Extensions.DependencyInjection;
    using Entry = (string Path, uint? TextureId);

    internal partial class TextureDisplay : Form
    {
        private readonly Program.Options options;
        private readonly VirtualImageList<Entry> textureDisplay;
        private MouseMoveFilter? filter;

        public TextureDisplay(IServiceProvider serviceProvider)
        {
            this.options = serviceProvider.GetRequiredService<Program.Options>();
            var texturePath = Path.Combine(this.options.SteamApps, WellKnownPaths.MGS2Texture);
            var assetsPath = Path.Combine(this.options.SteamApps, WellKnownPaths.MGS2Assets);

            this.InitializeComponent();

            this.textureDisplay = new VirtualImageList<Entry>(
                Enumerable.Concat(
                    Directory.GetFiles(texturePath, "*.ctxr", SearchOption.AllDirectories).Select(f => (f, default(uint?))),
                    Directory.GetFiles(assetsPath, "*.tri", SearchOption.AllDirectories).SelectMany(f => TriFile.List(f).Select(id => (Entry)(f, id))).OrderBy(e => e.Path)),
                async pair =>
                {
                    var (path, id) = pair;
                    return await (id == null ? CtxrFile.LoadAsync(path) : TriFile.LoadAsync(path, id.Value)).ConfigureAwait(true);
                })
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
            TextureDisplay parent,
            VirtualImageList<Entry> textureDisplay,
            ToolTip toolTip) : IMessageFilter
        {
            private readonly TextureDisplay parent = parent;
            private readonly VirtualImageList<Entry> textureDisplay = textureDisplay;
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
                        caption = Path.GetRelativePath(this.parent.options.SteamApps, hit.Path + (hit.TextureId is uint id ? $" ({id:x8})" : string.Empty));
                    }

                    this.toolTip.SetToolTip(this.textureDisplay, caption);
                }

                return false;
            }
        }
    }
}
