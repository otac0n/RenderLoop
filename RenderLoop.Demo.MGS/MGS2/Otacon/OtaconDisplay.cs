// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS2.Otacon
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using Sprite = (int Sprite, int Index);

    internal partial class OtaconDisplay : Form
    {
        private readonly Bitmap[] sprites = new Bitmap[4];
        private readonly Stopwatch selectedTime = new();
        private readonly AnimationState animationState = new AnimationState();
        private Size spriteSize;

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
            if (this.selectedTime.Elapsed >= TimeSpan.FromSeconds(2))
            {
                var available = Enum.GetValues<AnimationState.State>().Where(s => s != AnimationState.State.Neutral).ToList();
                this.animationState.TargetState = this.animationState.TargetState == AnimationState.State.Neutral ? available[Random.Shared.Next(available.Count)] : AnimationState.State.Neutral;
                this.selectedTime.Restart();
            }

            if (this.animationState.Update())
            {
                this.Invalidate();
            }
        }

        private void Form_Paint(object sender, PaintEventArgs e)
        {
            var current = this.animationState.CurrentSprite;
            var sprite = this.sprites[current.Sprite];
            var offset = -(this.spriteSize.Width * current.Index);
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
