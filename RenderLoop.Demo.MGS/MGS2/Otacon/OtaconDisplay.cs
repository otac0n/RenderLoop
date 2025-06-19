// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS2.Otacon
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    internal partial class OtaconDisplay : Form
    {
        private readonly Bitmap[] sprites = new Bitmap[4];
        private readonly AnimationState animationState = new();
        private Size spriteSize;

        public OtaconDisplay(IServiceProvider serviceProvider)
        {
            this.InitializeComponent();
            this.EnableDrag();
            foreach (var state in Enum.GetValues<AnimationState.State>())
            {
                if (state != AnimationState.State.Invisible)
                {
                    this.contextMenu.Items.Add(state.ToString());
                }
            }
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

            this.updateTimer.Enabled = true;
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
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

        private void Form_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this.contextMenu.Show(this.Location + new Size(this.Width, 0));
            }
        }

        private void ContextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            this.animationState.TargetState = Enum.Parse<AnimationState.State>(e.ClickedItem.Text);
        }
    }
}
