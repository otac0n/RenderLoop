// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS2
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;
    using Microsoft.Extensions.DependencyInjection;
    using Entry = (string Path, uint? TextureId);

    internal partial class TextureDisplay : Form
    {
        private readonly Program.Options options;
        private readonly VirtualImageList<Entry> textureDisplay;

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
                Dock = DockStyle.Fill,
            };
            this.textureDisplay.MouseMove += this.TextureDisplay_MouseMove;
            this.Controls.Add(this.textureDisplay);
        }

        private void TextureDisplay_MouseMove(object? sender, MouseEventArgs e)
        {
            var caption = string.Empty;
            if (this.textureDisplay.HitTest(e.Location, out var hit))
            {
                caption = Path.GetRelativePath(this.options.SteamApps, hit.Path + (hit.TextureId is uint id ? $" ({id:x8})" : string.Empty));
            }

            this.toolTip.SetToolTip(this.textureDisplay, caption);
        }
    }
}
