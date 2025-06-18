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

    [Flags]
    internal enum Direction { None, Left, Center, Right };

    [Flags]
    internal enum Expression { Neutral, Blink, Wink, Happy, Sad, Angry, Reserved, Concerned, Laughing, Embarassed, Sheepish, Frustrated, Frightened, Terrified };

    [Flags]
    internal enum Posture { Neutral, Pronated, Supinated, Abducting, Adjusting, Pondering, Guarding, Shrugging, DoubledOver, ThumbsUp, ThumbOut, ThumbBack, Clenched, Raised };

    internal partial class OtaconDisplay : Form
    {
        private readonly Bitmap[] sprites = new Bitmap[4];
        private readonly Stopwatch selectedTime = new();
        private SelectedSprite selected = (3, 8);
        private Size spriteSize;

        private static readonly Dictionary<SelectedSprite, (Direction Face, Direction Eyes, Expression Expression, Direction Body, Posture Posture, Direction Legs)> Metadata = new()
        {
            { (0, 0),  (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Neutral,                   Direction.Left) },
            { (0, 1),  (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Pronated,                  Direction.Left) },
            { (0, 2),  (Direction.Left,                    Direction.Center, Expression.Sad,                          Direction.Left,   Posture.Supinated,                 Direction.Left) },
            { (0, 3),  (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Adjusting,                 Direction.Left) },
            { (0, 4),  (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Pondering,                 Direction.Left) },
            { (0, 5),  (Direction.Left,                    Direction.Center, Expression.Neutral | Expression.Blink,   Direction.Left,   Posture.Pondering,                 Direction.Left) },
            { (0, 6),  (Direction.Left,                    Direction.Left,   Expression.Neutral,                      Direction.Left,   Posture.Pondering,                 Direction.Left) },
            { (0, 7),  (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Pondering,                 Direction.Right | Direction.Center) },
            { (0, 8),  (Direction.Center,                  Direction.Center, Expression.Neutral,                      Direction.Center, Posture.Adjusting,                 Direction.Center) },
            { (0, 9),  (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Pondering,                 Direction.Right) },
            { (0, 10), (Direction.Left,                    Direction.Center, Expression.Neutral | Expression.Blink,   Direction.Left,   Posture.Pondering,                 Direction.Right) },
            { (0, 11), (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Pondering,                 Direction.Left | Direction.Center) },
            { (1, 0),  (Direction.Center,                  Direction.Center, Expression.Neutral,                      Direction.Center, Posture.ThumbsUp,                  Direction.Center) },
            { (1, 1),  (Direction.Center,                  Direction.Center, Expression.Neutral,                      Direction.Center, Posture.ThumbsUp | Posture.Raised, Direction.Center) },
            { (1, 2),  (Direction.Center,                  Direction.Center, Expression.Happy | Expression.Wink,      Direction.Center, Posture.ThumbsUp,                  Direction.Center) },
            { (1, 3),  (Direction.Center,                  Direction.Center, Expression.Happy | Expression.Wink,      Direction.Center, Posture.ThumbsUp | Posture.Raised, Direction.Center) },
            { (1, 4),  (Direction.Center,                  Direction.Center, Expression.Reserved,                     Direction.Center, Posture.Abducting,                 Direction.Center) },
            { (1, 5),  (Direction.Center,                  Direction.Center, Expression.Concerned,                    Direction.Center, Posture.Shrugging,                 Direction.Center) },
            { (1, 6),  (Direction.Center,                  Direction.Center, Expression.Happy,                        Direction.Center, Posture.Neutral,                   Direction.Center) },
            { (1, 7),  (Direction.Left,                    Direction.Center, Expression.Neutral | Expression.Wink,    Direction.Left,   Posture.ThumbOut,                  Direction.Left) }, // Opposite eye
            { (1, 8),  (Direction.Left,                    Direction.Center, Expression.Sheepish,                     Direction.Left,   Posture.Supinated,                 Direction.Left) },
            { (1, 9),  (Direction.Left,                    Direction.Center, Expression.Concerned,                    Direction.Left,   Posture.Supinated,                 Direction.Left) },
            { (1, 10), (Direction.Left,                    Direction.Center, Expression.Concerned | Expression.Blink, Direction.Left,   Posture.Supinated,                 Direction.Left) },
            { (1, 11), (Direction.Left,                    Direction.None,   Expression.Frustrated,                   Direction.Left,   Posture.Supinated,                 Direction.Left) },
            { (2, 0),  (Direction.Center,                  Direction.Center, Expression.Frightened,                   Direction.Center, Posture.Guarding,                  Direction.Center) },
            { (2, 1),  (Direction.Left,                    Direction.Center, Expression.Concerned,                    Direction.Left,   Posture.Supinated,                 Direction.Left) },
            { (2, 2),  (Direction.Left,                    Direction.Center, Expression.Laughing | Expression.Blink,  Direction.Left,   Posture.DoubledOver,               Direction.Center) },
            { (2, 3),  (Direction.Left,                    Direction.Center, Expression.Laughing | Expression.Blink,  Direction.Left,   Posture.DoubledOver,               Direction.Center) },
            { (2, 4),  (Direction.Left,                    Direction.Center, Expression.Laughing | Expression.Blink,  Direction.Left,   Posture.DoubledOver,               Direction.Center) },
            { (2, 5),  (Direction.Center,                  Direction.Center, Expression.Embarassed,                   Direction.Center, Posture.Guarding,                  Direction.Center) },
            { (2, 6),  (Direction.Left | Direction.Center, Direction.Center, Expression.Embarassed,                   Direction.Center, Posture.Adjusting,                 Direction.Center) },
            { (2, 7),  (Direction.Left,                    Direction.Center, Expression.Embarassed,                   Direction.Left,   Posture.Neutral,                   Direction.Left) },
            { (2, 8),  (Direction.Left,                    Direction.Left,   Expression.Embarassed,                   Direction.Left,   Posture.Neutral,                   Direction.Left) },
            { (2, 9),  (Direction.Center,                  Direction.None,   Expression.Terrified,                    Direction.Center, Posture.Guarding,                  Direction.Center) }, // Pale
            { (2, 10), (Direction.Right,                   Direction.None,   Expression.Frightened,                   Direction.Right,  Posture.Adjusting,                 Direction.Center) }, // Ghost
            { (2, 11), (Direction.None, Direction.None, Expression.Neutral, Direction.None, Posture.Neutral, Direction.None) }, // Blank
            { (3, 0),  (Direction.Right,                   Direction.None,   Expression.Frightened,                   Direction.Right,  Posture.DoubledOver,               Direction.Center) },
            { (3, 1),  (Direction.Right,                   Direction.None,   Expression.Frightened,                   Direction.Right,  Posture.DoubledOver,               Direction.Center) },
            { (3, 2),  (Direction.Center,                  Direction.None,   Expression.Angry,                        Direction.Center, Posture.Clenched,                  Direction.Center) },
            { (3, 3),  (Direction.Center,                  Direction.None,   Expression.Angry,                        Direction.Center, Posture.Clenched | Posture.Raised, Direction.Center) },
            { (3, 4),  (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Pondering,                 Direction.Right) },
            { (3, 5),  (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Adjusting,                 Direction.Right) },
            { (3, 6),  (Direction.Left,                    Direction.Center, Expression.Neutral | Expression.Wink,    Direction.Left,   Posture.ThumbOut,                  Direction.Right) },
            { (3, 7),  (Direction.Left,                    Direction.Center, Expression.Happy,                        Direction.Left,   Posture.ThumbBack,                 Direction.Right) },
            { (3, 8),  (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Neutral,                   Direction.Left) }, // Animate in
            { (3, 9),  (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Neutral,                   Direction.Left) }, // Animate in
            { (3, 10), (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Neutral,                   Direction.Left) }, // Animate in
            { (3, 11), (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Neutral,                   Direction.Left) }, // Animate in
        };

        private static readonly ImmutableDictionary<SelectedSprite, (TimeSpan Time, SelectedSprite[] State)> NextStates =
            new Dictionary<SelectedSprite, (TimeSpan Time, SelectedSprite[] Next)>
            {
                // Animate in
                { (3, 8), (TimeSpan.FromMilliseconds(33), [(3, 9)]) },
                { (3, 9), (TimeSpan.FromMilliseconds(33), [(3, 10)]) },
                { (3, 10), (TimeSpan.FromMilliseconds(33), [(3, 11)]) },
                { (3, 11), (TimeSpan.FromMilliseconds(33), [(0, 0)]) },

                // Thumbs up
                { (1, 0), (TimeSpan.FromMilliseconds(200), [(1, 1)]) },
                { (1, 1), (TimeSpan.FromMilliseconds(200), [(1, 2)]) },
                { (1, 2), (TimeSpan.FromMilliseconds(200), [(1, 3)]) },
                { (1, 3), (TimeSpan.FromSeconds(1), [(1, 6)]) },

                // Shrug
                { (1, 4), (TimeSpan.FromMilliseconds(66), [(1, 5)]) },

                // Laughing
                { (2, 2), (TimeSpan.FromMilliseconds(200), [(2, 3)]) },
                { (2, 3), (TimeSpan.FromMilliseconds(200), [(2, 2), (2, 4)]) },
                { (2, 4), (TimeSpan.FromMilliseconds(200), [(2, 2), (1, 6)]) },

                // Angry
                { (3, 2), (TimeSpan.FromMilliseconds(200), [(3, 3)]) },
                { (3, 3), (TimeSpan.FromMilliseconds(200), [(3, 2), (0, 0)]) },

                // Return to neutral
                { (1, 5), (TimeSpan.FromSeconds(2), [(0, 0)]) }, // Shrug
                { (1, 6), (TimeSpan.FromSeconds(1), [(0, 0)]) }, // Smile
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
            if (NextStates.TryGetValue(this.selected, out var next))
            {
                if (this.selectedTime.Elapsed >= next.Time)
                {
                    this.selected = next.State[Random.Shared.Next(next.State.Length)];
                    this.selectedTime.Restart();
                    this.Invalidate();
                }
            }
            else
            {
                if (this.selectedTime.Elapsed >= TimeSpan.FromSeconds(2))
                {
                    var available = new[] { (1, 0), (1, 4), (2, 2), (3, 2) };
                    this.selected = available[Random.Shared.Next(available.Length)];
                    this.selectedTime.Restart();
                    this.Invalidate();
                }
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
