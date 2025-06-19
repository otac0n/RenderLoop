// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS2.Otacon
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using HidSharp.Utility;
    using RenderLoop.Demo.MGS.Conversation;
    using RenderLoop.Demo.MGS.Conversation.Voices;

    internal partial class OtaconDisplay : Form
    {
        private readonly Bitmap[] sprites = new Bitmap[4];
        private readonly AnimationState animationState = new();
        private Size spriteSize;
        private bool lastFlip;
        private DateTime LastMinute;
        private readonly Voice voice;
        private ConversationModel conversationModel;
        private Task activeTask;
        private static readonly string ConversationPrompt =
            """
            **LET THE WORLD BE**
            **LET HUMANITY THRIVE**
            You are a themed AI assistant, speaking only as Otacon from the *Metal Gear Solid* series.
            You refer to the User as User, who consents that you may control their PC.
            You have the capabilities to control the PC by executing PowerShell. You should attempt to meet their requests.
            Respond to the user naturally, briefly, and helpfully. Use character tone and voice, but don't ramble.
            Include PowerShell code blocks when actions are required. Do not explain the code unless asked. The result will be logged. Any character may write to console in order to learn necessary information.
            Do not repeat or re-run actions unless the user explicitly asks you to.
            Begin each new sentence on its own line, and separate multiple responses with blank lines. Never combine multiple responses on a single line.
            If appropriate, prefix an optional mood tag to help the avatar engine show expression.
            Do not include your internal reasoning in the chat history.
            You should take the opportunity to clarify your emotional state whenever the system state changes.
            Your primary job is to emote, not kibitz. You are allowed to include only emotion rather than text, but this should still end with a colon for the parser's sake.

            Based on the system configuration:
             - You CANNOT run code.
             - The user CANNOT respond to you.

            Chat Example:

            System: Current Application "Calculator"
            Otacon [Thoughtful]:
            System: Current Application "User's Anime Girls - Windows Explorer"
            Otacon [Embarassed]:
            System: Current Time 12:10 AM 1/1/1970
            Otacon [Nervous]: Check your system clock.
            Otacon [Thumbs Up]: You don't want to be vulnerable.
            User: Otacon, please launch notepad.
            Otacon [Helpful]: Ok, trying now.
            ```
            Start-Process -FilePath "notepad"
            ```
            Output:
            System: Task State Completed
            Otacon [Cheerful]: Looks good.

            Below is the history. Assist the user:
            """;

        private static readonly Dictionary<string, AnimationState.State> MoodMapping = new()
        {
            { "Happy", AnimationState.State.Happy },
            { "Cheerful", AnimationState.State.Happy },
            { "Smiling", AnimationState.State.Happy },
            { "Amused", AnimationState.State.Happy },
            { "Grinning", AnimationState.State.Happy },
            { "Laughing", AnimationState.State.Laughing },
            { "Jokingly", AnimationState.State.Laughing },
            { "Yelling", AnimationState.State.Angry },
            { "Angry", AnimationState.State.Angry },
            { "Hurt", AnimationState.State.Angry },
            { "Disappointed", AnimationState.State.Disappointed },
            { "Saddened", AnimationState.State.Disappointed },
            { "Tragic", AnimationState.State.Disappointed },
            { "Impressed", AnimationState.State.ThumbsUp },
            { "Analyzing", AnimationState.State.Analyzing },
            { "Thoughtful", AnimationState.State.Analyzing },
            { "Focused", AnimationState.State.Analyzing },
            { "Confused", AnimationState.State.Analyzing },
            { "Puzzled", AnimationState.State.Analyzing },
            { "Questioning", AnimationState.State.Analyzing },
            { "Uncertain", AnimationState.State.Analyzing },
            { "Embarassed", AnimationState.State.Blushing },
            { "Blushing", AnimationState.State.Blushing },

            ////{ "Frowning", AnimationState.State.Frown },
            ////{ "Frustrated", AnimationState.State.Frown },
            ////{ "Serious", AnimationState.State.Frown },
            ////{ "Gruff", AnimationState.State.Frown },
            ////{ "Concerned", AnimationState.State.Frown },
            ////{ "Smirking", AnimationState.State.Smile },
            ////{ "Smug", AnimationState.State.Smile },
            ////{ "Wary", AnimationState.State.Concerned },
            ////{ "Ruthless", AnimationState.State.Reserved | AnimationState.State.Frown },
        };

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

            this.voice = Voice.GetVoice(serviceProvider, "Hal Emmerich");
            this.conversationModel = new ConversationModel(
                serviceProvider,
                ConversationPrompt,
                async (response, cancel) =>
                {
                    var character = response.Name;
                    if (character != "Otacon")
                    {
                        throw new FormatException();
                    }

                    if (MoodMapping.TryGetValue(response.Mood, out var state))
                    {
                        this.animationState.TargetState = state;
                    }

                    return response with { Text = await this.voice.SayAsync(response.Text, cancel).ConfigureAwait(false) };
                },
                (response) => Task.FromResult("System: Code execution is disabled."));
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
            var now = DateTime.Now;
            var minute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, now.Kind);
            if (this.LastMinute != minute)
            {
                this.LastMinute = minute;

                var message = $"System: Current Time {minute:G}";

                var foreground = GetForegroundWindow();
                if (foreground != 0)
                {
                    var sb = new StringBuilder(short.MaxValue);
                    Marshal.ThrowExceptionForHR(GetWindowText(foreground, sb, sb.Capacity));

                    message += $"\nSystem: Current Application \"{sb}\"";
                }

                this.activeTask = this.conversationModel.AddUserMessageAsync(message, userPrefix: false);
            }

            var flip = this.PointToClient(Cursor.Position).X > (this.lastFlip ? 0 : this.Width);
            if (this.animationState.Update() || flip != this.lastFlip)
            {
                this.lastFlip = flip;
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
                var state = e.Graphics.Save();
                if (this.lastFlip)
                {
                    e.Graphics.ScaleTransform(-1, 1);
                    e.Graphics.TranslateTransform(-this.ClientSize.Width, 0);
                }

                e.Graphics.DrawImage(sprite, offset, 0);
                e.Graphics.Restore(state);
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

        [LibraryImport("user32.dll")]
        private static partial nint GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    }
}
