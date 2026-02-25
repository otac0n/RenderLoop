// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS1
{
    using System;
    using System.Drawing.Drawing2D;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using ImageMagick;
    using Microsoft.Extensions.DependencyInjection;
    using RenderLoop.Demo.MGS.MGS1.Archives;
    using Entry = Archives.NestedFileSystemManager.Entry;

    internal partial class TextureDisplay : Form
    {
        private readonly VirtualImageList<Entry> textureDisplay;

        public TextureDisplay(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<Program.Options>();
            var fsm = serviceProvider.GetRequiredKeyedService<NestedFileSystemManager>(WellKnownPaths.AllDataBin);

            this.InitializeComponent();
            this.textureDisplay = new VirtualImageList<Entry>(
                fsm.EnumerateFiles(Path.GetDirectoryName(WellKnownPaths.CD1Path), "*.pcx", recursive: true).OrderBy(f => f.Path),
                entry =>
                {
                    using var textureFile = fsm.OpenRead(entry.Path);
                    return Task.FromResult(new MagickImage(textureFile).ToBitmap());
                },
                InterpolationMode.NearestNeighbor)
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
                caption = hit.Path;
            }

            this.toolTip.SetToolTip(this.textureDisplay, caption);
        }
    }
}
