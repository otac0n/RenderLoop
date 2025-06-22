// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS1
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Microsoft.Extensions.DependencyInjection;

    internal partial class TextureDisplay : Form
    {
        private readonly string basePath;
        private readonly VirtualImageList<string> textureDisplay;

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
                    textureFile.Seek(2, SeekOrigin.Current);
                    return Task.FromResult(Model.ReadMgsPcx(textureFile));
                },
                InterpolationMode.NearestNeighbor)
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
                caption = hit;
            }

            this.toolTip.SetToolTip(this.textureDisplay, caption);
        }
    }
}
