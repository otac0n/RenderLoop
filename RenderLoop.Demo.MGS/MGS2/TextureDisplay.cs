// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS2
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Windows.Forms;
    using Microsoft.Extensions.DependencyInjection;

    internal partial class TextureDisplay : Form
    {
        private readonly string basePath;
        private readonly VirtualImageList<string> textureDisplay;

        public TextureDisplay(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<Program.Options>();
            this.basePath = Path.Combine(options.SteamApps, WellKnownPaths.MGS2Texture);

            this.InitializeComponent();
            this.textureDisplay = new VirtualImageList<string>(
                Directory.GetFiles(this.basePath, "*.ctxr", SearchOption.AllDirectories),
                async file =>
                {
                    using var stream = File.OpenRead(file);
                    return await CtxrFile.LoadAsync(stream).ConfigureAwait(true);
                })
            {
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location = Point.Empty,
                Width = this.ClientSize.Width,
            };
            this.textureDisplay.MouseMove += this.TextureDisplay_MouseMove;
            this.Controls.Add(this.textureDisplay);
        }

        private void TextureDisplay_MouseMove(object? sender, MouseEventArgs e)
        {
            var caption = string.Empty;
            if (this.textureDisplay.HitTest(e, out var hit))
            {
                caption = Path.GetRelativePath(this.basePath, hit);
            }

            this.toolTip.SetToolTip(this.textureDisplay, caption);
        }
    }
}
