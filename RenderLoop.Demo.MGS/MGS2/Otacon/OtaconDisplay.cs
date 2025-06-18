// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS2.Otacon
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Windows.Forms;
    using SelectedSprite = (int Sprite, int Index);

    internal partial class OtaconDisplay : Form
    {
        private readonly Bitmap[] sprites = new Bitmap[4];
        private readonly Stopwatch selectedTime = new();
        private SelectedSprite selected = (3, 8);
        private Size spriteSize;

        private static readonly ImmutableDictionary<SelectedSprite, (TimeSpan Time, SelectedSprite State)> NextStates =
            new Dictionary<SelectedSprite, (TimeSpan Time, SelectedSprite Next)>
            {
                { (3, 8), (TimeSpan.FromMilliseconds(33), (3, 9)) },
                { (3, 9), (TimeSpan.FromMilliseconds(33), (3, 10)) },
                { (3, 10), (TimeSpan.FromMilliseconds(33), (3, 11)) },
                { (3, 11), (TimeSpan.FromMilliseconds(33), (0, 0)) },
            }.ToImmutableDictionary();

        public OtaconDisplay(IServiceProvider serviceProvider)
        {
            this.InitializeComponent();
            this.EnableDrag();
        }

        private async void Form_Load(object sender, EventArgs e)
        {
            var sprite = await CtxrFile.LoadAsync(@"G:\Games\Steam\steamapps\common\MGS2\textures\flatlist\_win\00d27f22.ctxr").ConfigureAwait(true);
            var size = sprite.Size;
            size.Width /= 12;
            this.ClientSize = this.spriteSize = size;
            MoveToPrimaryBottomCorner(this);

            this.sprites[0] = sprite;
            this.sprites[1] = await CtxrFile.LoadAsync(@"G:\Games\Steam\steamapps\common\MGS2\textures\flatlist\_win\00d37f22.ctxr").ConfigureAwait(true);
            this.sprites[2] = await CtxrFile.LoadAsync(@"G:\Games\Steam\steamapps\common\MGS2\textures\flatlist\_win\00d47f22.ctxr").ConfigureAwait(true);
            this.sprites[3] = await CtxrFile.LoadAsync(@"G:\Games\Steam\steamapps\common\MGS2\textures\flatlist\_win\00d57f22.ctxr").ConfigureAwait(true);

            this.selectedTime.Restart();
            this.updateTimer.Enabled = true;
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (NextStates.TryGetValue(this.selected, out var next) && this.selectedTime.Elapsed >= next.Time)
            {
                this.selected = next.State;
                this.selectedTime.Restart();
                this.Invalidate();
            }
        }

        private void Form_Paint(object sender, PaintEventArgs e)
        {
            var sprite = this.sprites[this.selected.Sprite];
            var offset = -(this.spriteSize.Width * this.selected.Index);
            if (sprite is not null)
            {
                e.Graphics.DrawImage(sprite, offset, 0);
            }
        }

        private static void MoveToPrimaryBottomCorner(Form form)
        {
            ArgumentNullException.ThrowIfNull(form);

            var screen = Screen.PrimaryScreen.WorkingArea;
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(
                screen.Right - form.Width,
                screen.Bottom - form.Height);
        }
    }
}
