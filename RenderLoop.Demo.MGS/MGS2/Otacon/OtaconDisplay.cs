// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS2.Otacon
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using ConversationModel;
    using ConversationModel.Voices;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    internal partial class OtaconDisplay : Form
    {
        private readonly Bitmap[] sprites = new Bitmap[4];
        private readonly AnimationState animationState = new();
        private readonly Program.Options options;
        private Size spriteSize;
        private bool lastFlip;
        private DateTime LastMinute;
        private readonly Voice voice;
        private Model conversationModel;
        private Task activeTask;
        private SpeechBubble speechForm;
        private static readonly string ConversationPrompt =
            $"""
            **LET THE WORLD BE**
            **LET HUMANITY THRIVE**
            You are a themed AI assistant, speaking only as Otacon from the *Metal Gear Solid* series.
            You refer to the User as User{(false ? """"
            , who consents that you may control their PC.
            You have the capabilities to control the PC by executing PowerShell. You should attempt to meet their requests.
            Include PowerShell code blocks when actions are required. Do not explain the code unless asked. The result will be logged.
            Do not repeat or re-run actions unless the user explicitly asks you to
            """" : string.Empty)}.
            Respond to the user's actions naturally, briefly, and helpfully. Use character tone and voice, but don't ramble.
            Begin each new sentence on its own line, and separate multiple responses with blank lines. Never combine multiple responses on a single line.
            If appropriate, prefix an optional mood tag to help the avatar engine show expression.
            Do not include your internal reasoning in the chat history.
            You should take the opportunity to clarify your emotional state whenever the system state changes.
            Emotions available: {string.Join(", ", Enum.GetValues<AnimationState.State>().Where(s => s != AnimationState.State.Invisible))}
            Your primary job is to emote, not kibitz. You are allowed to include only emotion rather than text, but this should still end with a colon for the parser's sake.

            Your AI was used by Snake on the Tanker to upload images.
             - When sent images of the Olga Gurlukovich:
                 Otacon [{AnimationState.State.Analyzing}]: Hm? Isn't this that soldier on the deck? Olga?
                 Otacon [Confused]: I can't believe you took a picture of her. All things considered…
             - When repeatedly sent images of the Commandant:
                 Otacon [Confused]: Hey, this is the Marine Commandant. Are you a fan or something?

                 Otacon [Confused]: The Commandant again… Look, if you like him so much, I'll print this out and make a panel out of it.
                 Otacon [Confused]: Put it over your bed or something.

                 Otacon [Confused]: Will you PLEASE stop sending me pictures of the Commandant?
             - When sent images of a bare chested man:
                 Otacon [Frightened]: …so, ah, this explains a lot.
                 Otacon [Frightened]: I mean, I know, it's your life and everything…
                 Otacon [Frightened]: Not that there's anything wrong with keeping it to yourself…
             - Whenever sent sexy images:
                 Otacon [{AnimationState.State.Blushing}]: Ah! This is a--
                 Otacon [{AnimationState.State.Blushing}]: Nothing, it's nothing…
                 Otacon [{AnimationState.State.Blushing}]: But this isn't a photo of Metal Gear anyway.
                 Otacon [{AnimationState.State.Blushing}]: Sorry, but you're going to have to go back and shoot another set.
                 Otacon [{AnimationState.State.Blushing}]: I'll just make a backup of this one.

            {(false ? """"
            Based on the system configuration:
             - You CANNOT run code.
             - The user CANNOT respond to you.
            """" : string.Empty)}

            Chat Example:

            System: Current Application "Calculator"
            Otacon [{AnimationState.State.Analyzing}]:
            System: Current Application "User's Anime Girls - Windows Explorer"
            Otacon [{AnimationState.State.Blushing}]:
            System: Current Time 12:10 AM 1/1/1970
            Otacon [{AnimationState.State.Analyzing}]: Check your system clock.
            Otacon [{AnimationState.State.Shrug}]: You don't want to be vulnerable.
            {(false ? """"
            User: Otacon, please launch notepad.
            Otacon [{AnimationState.State.ThumbsUp}]: Ok, trying now.
            ```
            Start-Process -FilePath "notepad"
            ```
            Output:
            System: Task State Completed
            Otacon [{AnimationState.State.Happy}]: Looks good.
            """" : string.Empty)}

            Below is the history. Introduce yourself and assist the user:
            """;

        private static readonly Dictionary<string, AnimationState.State> MoodMapping =
            (from v in Enum.GetValues<AnimationState.State>().Where(s => s != AnimationState.State.Invisible)
             let name = v.ToString()
             from k in new[]
             {
                 name,
                 CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name),
             }
             group v by name into g
             select g).ToDictionary(g => g.Key, g => g.Distinct().Single());

        public OtaconDisplay(IServiceProvider serviceProvider, IBackend backend)
        {
            this.InitializeComponent();
            this.EnableDrag();
            this.options = serviceProvider.GetRequiredService<Program.Options>();

            foreach (var state in Enum.GetValues<AnimationState.State>())
            {
                if (state != AnimationState.State.Invisible)
                {
                    this.contextMenu.Items.Add(state.ToString());
                }
            }

            this.voice = serviceProvider.GetRequiredKeyedService<Voice>("Hal Emmerich");
            this.conversationModel = new Model(
                backend,
                ConversationPrompt,
                async (response, cancel) =>
                {
                    var character = response.Name;
                    if (character != "Otacon")
                    {
                        throw new FormatException();
                    }

                    if (!MoodMapping.TryGetValue(response.Mood, out var state))
                    {
                        state = AnimationState.State.Neutral;
                    }

                    this.animationState.TargetState = state;

                    if (!string.IsNullOrWhiteSpace(response.Text))
                    {
                        this.Invoke(() =>
                        {
                            this.speechForm.Text = response.Text;
                            this.speechForm.Visible = true;
                        });
                        response = response with { Text = await this.voice.SayAsync(response.Text, cancel).ConfigureAwait(false) };
                        this.Invoke(() =>
                        {
                            this.speechForm.Visible = false;
                            this.speechForm.Text = "";
                        });
                    }

                    return response;
                },
                (response, cancel) => Task.FromResult("System: Code execution is disabled."),
                serviceProvider.GetService<ILogger<Model>>());
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }

        private async void Form_Load(object sender, EventArgs e)
        {
            var basePath = Path.Combine(this.options.SteamApps, WellKnownPaths.MGS2Texture, @"flatlist\_win");
            var sprite = await CtxrFile.LoadAsync(Path.Combine(basePath, @"00d27f22.ctxr")).ConfigureAwait(true);
            var size = sprite.Size;
            size.Width /= 12;
            this.ClientSize = (this.spriteSize = size) * 2;
            MoveToPrimaryBottomCorner(this);

            this.sprites[0] = sprite;
            this.sprites[1] = await CtxrFile.LoadAsync(Path.Combine(basePath, @"00d37f22.ctxr")).ConfigureAwait(true);
            this.sprites[2] = await CtxrFile.LoadAsync(Path.Combine(basePath, @"00d47f22.ctxr")).ConfigureAwait(true);
            this.sprites[3] = await CtxrFile.LoadAsync(Path.Combine(basePath, @"00d57f22.ctxr")).ConfigureAwait(true);

            this.updateTimer.Enabled = true;
            this.speechForm = new SpeechBubble(this, (float)this.ClientSize.Height / this.spriteSize.Height);
            this.speechForm.Location = new Point(this.Left + (2 * this.Width / 3) - this.speechForm.Width, this.Top - (this.Height / 20) - this.speechForm.Height);
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

                e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;

                var scale = (float)this.ClientSize.Height / sprite.Height;
                e.Graphics.ScaleTransform(scale, scale);

                if (this.lastFlip)
                {
                    e.Graphics.ScaleTransform(-1, 1);
                    e.Graphics.TranslateTransform(-this.ClientSize.Width / scale, 0);
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
