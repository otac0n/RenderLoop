// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.Codec
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.Drawing.Text;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using AnimatedGif;
    using DiscUtils.Streams;
    using Microsoft.Extensions.DependencyInjection;
    using RenderLoop.Demo.MGS.Codec.Conversation;
    using static RenderLoop.Demo.MGS.Codec.CharacterMetadata;
    using ImageSet = System.Collections.Immutable.ImmutableDictionary<string, (int X, int Y, System.Drawing.Bitmap Image)>;

    internal partial class CodecDisplay : Form
    {
        private static FontFamily Digital7 = LoadEmbeddedFont("digital-7.ttf");

        private ConversationModel conversationModel;

        private static Dictionary<(string Mood, string Tags), double> MoodMappingScores = new()
        {
            { ("Frowning", "Frown"), 1.0 },
            { ("Frustrated", "Frown"), 1.0 },
            { ("Serious", "Frown"), 1.0 },
            { ("Gruff", "Frown"), 1.0 },
            { ("Yelling", "Yell"), 1.0 },
            { ("Angry", "Yell"), 1.0 },
            { ("Angry", "Frown"), 0.8 },
            { ("Concerned", "Frown"), 0.7 },
            { ("Cheerful", "Enthusiastic"), 1.0 },
            { ("Smiling", "Smile"), 1.0 },
            { ("Smirking", "Smile"), 1.0 },
            { ("Jokingly", "Smile"), 1.0 },
            { ("Cheerful", "Smile"), 0.9 },
            { ("Happy", "Smile"), 1.0 },
            { ("Amused", "Mischievous"), 1.0 },
            { ("Amused", "Smile"), 0.9 },
            { ("Grinning", "Smile"), 0.9 },
            { ("Smug", "Smile"), 0.9 },
            { ("Impressed", "Surprised"), 0.9 },
            { ("Impressed", "Smile"), 0.8 },
            { ("Questioning", "Concerned"), 0.9 },
            { ("Puzzled", "Concerned"), 0.9 },
            { ("Uncertain", "Concerned"), 0.9 },
            { ("Wary", "Concerned"), 0.9 },
            { ("Ruthless", "Reserved"), 1.0 },
            { ("Ruthless", "Frown"), 0.5 },
            { ("Saddened", "Sad"), 1.0 },
            { ("Hurt", "Sad"), 1.0 },
            { ("Hurt", "Pain"), 0.5 },
            { ("Tragic", "Sad"), 1.0 },
            { ("Disappointed", "Sad"), 1.0 },
            { ("Disappointed", "Concerned"), 0.9 },
            { ("Disappointed", "Frown"), 0.8 },
        };

        private string ActiveCharacter
        {
            get => this.nameLabel.Text;
            set => this.nameLabel.Text = value;
        }

        public CodecDisplay(IServiceProvider serviceProvider)
        {
            this.InitializeComponent();
            this.EnableDrag();
            MoveToRightmostBottomCorner(this);

            var options = serviceProvider.GetRequiredService<Program.Options>();
            var codecOptions = serviceProvider.GetRequiredService<CodecOptions>();
            var facesStream = serviceProvider.GetRequiredKeyedService<SparseStream>((options.File, WellKnownPaths.CD1Path, WellKnownPaths.FaceDatPath));
            var source = ImageLoader.LoadImages(facesStream);

            var maxX = source.Values.SelectMany(x => x.Values.Select(v => v.X + v.Image.Width)).Max();
            var maxY = source.Values.SelectMany(x => x.Values.Select(v => v.Y + v.Image.Height)).Max();
            var vuW = this.volumeMeter.Width;
            var vuH = this.volumeMeter.Height;

            var surface = new Bitmap(maxX, maxY);
            var g = Graphics.FromImage(surface);
            this.display.Image = surface;

            var vuSurface = new Bitmap(vuW, vuH);
            var vu = Graphics.FromImage(vuSurface);
            this.volumeMeter.Image = vuSurface;

            var volume = 0.0;
            var avatars = new Dictionary<string, AvatarState>();

            void Render()
            {
                if (!this.InvokeRequired)
                {
                    if (this.ActiveCharacter != null && avatars.TryGetValue(this.ActiveCharacter, out var avatarState) && CharacterImages.TryGetValue(this.ActiveCharacter, out var imageSet))
                    {
                        var images = (from x in imageSet
                                      let s = source[x.Id]
                                      where s.ContainsKey("base") && s.Count > 1
                                      let score = avatarState.Mood == x.Tags ? 1 :
                                                  MoodMappingScores.TryGetValue((avatarState.Mood, x.Tags), out var sc) ? sc :
                                                  0
                                      orderby score descending, x.Tags == "Neutral" descending
                                      select s).First();
                        RenderFace(g, images, avatarState.Eyes, avatarState.Mouth);
                    }
                    else
                    {
                        RenderStatic(g, source["f73b"]["base"].Image.Palette, maxX / 3, maxY / 3);
                    }

                    this.display.Invalidate();
                }
                else
                {
                    this.Invoke(Render);
                }
            }

            void ShowAvatar(string name, string caption)
            {
                if (!this.InvokeRequired)
                {
                    this.ActiveCharacter = name;
                    this.captionLabel.Text = caption;
                    Render();
                }
                else
                {
                    this.Invoke(() => ShowAvatar(name, caption));
                }
            }

            this.updateTimer.Tick += (s, e) =>
            {
                volume *= 0.9;
                if (this.ActiveCharacter == null || !DisplayedFrequency.TryGetValue(this.ActiveCharacter, out var frequency))
                {
                    frequency = "000.00";
                }

                RenderVolumeDisplay(vu, vuW, vuH, volume, frequency);
                this.volumeMeter.Invalidate();
                this.progressIndicator.Invalidate();

                if (this.ActiveCharacter == null || !CharacterImages.ContainsKey(this.ActiveCharacter))
                {
                    Render();
                }
            };

            foreach (var name in CharacterImages.Keys)
            {
                var avatarState = new AvatarState(serviceProvider, name);
                this.updateTimer.Tick += (e, a) => avatarState.Update();
                avatarState.Updated += (e, a) =>
                {
                    volume = Math.Max(volume, avatarState.Volume);
                    if (name == this.ActiveCharacter)
                    {
                        Render();
                    }
                };
                avatarState.IndexReached += (e, a) =>
                {
                    if (name == this.ActiveCharacter)
                    {
                        this.Invoke(() =>
                        {
                            this.captionLabel.Select(a.Index, a.Length);
                            this.captionLabel.ScrollToCaret();
                        });
                    }
                };
                avatars.Add(name, avatarState);
            }

            if (codecOptions.LMEndpoint != null)
            {
                var defaultVoice = new AvatarState(serviceProvider, "Unknown");
                this.conversationModel = new ConversationModel(
                    serviceProvider,
                    async (response, cancel) =>
                    {
                        var character = response.Name;
                        ShowAvatar(character, response.Text);

                        if (!avatars.TryGetValue(character, out var avatarState))
                        {
                            avatarState = defaultVoice;
                        }

                        avatarState.Mood = response.Mood;
                        return await avatarState.SayAsync(response.Text, cancel).ConfigureAwait(false);
                    },
                    this.RunCodeWithUserReview);
                this.conversationModel.TokenReceived += this.ConversationModel_TokenReceived;
            }
        }

        public Task<string> RunCodeWithUserReview(CodeResponse codeResponse)
        {
            var tcs = new TaskCompletionSource<string>();

            void ShowReviewForm()
            {
                var form = new Form
                {
                    Text = "Review Code",
                    Width = this.Width,
                    Height = this.Height * 8 / 10,
                    StartPosition = FormStartPosition.CenterParent,
                };

                var textBox = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Both,
                    Dock = DockStyle.Fill,
                    Text = codeResponse.Code,
                    Font = new Font("Consolas", 8),
                };

                var approveButton = new Button
                {
                    Text = "Approve",
                    AutoSize = true,
                    DialogResult = DialogResult.OK,
                };

                var denyButton = new Button
                {
                    Text = "Deny",
                    AutoSize = true,
                    DialogResult = DialogResult.Cancel,
                };

                var buttonPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    FlowDirection = FlowDirection.RightToLeft,
                    AutoSize = true,
                };

                buttonPanel.Controls.Add(approveButton);
                buttonPanel.Controls.Add(denyButton);

                form.Controls.Add(textBox);
                form.Controls.Add(buttonPanel);

                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _ = Task.Run(() =>
                    {
                        var tempFile = Path.GetTempFileName() + ".ps1";
                        try
                        {
                            File.WriteAllText(tempFile, codeResponse.Code, Encoding.UTF8);

                            var startInfo = new ProcessStartInfo
                            {
                                FileName = "powershell",
                                Arguments = $"-File \"{tempFile}\"",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true,
                            };

                            using var process = new Process
                            {
                                StartInfo = startInfo,
                            };
                            var outputBuilder = new StringBuilder();

                            process.OutputDataReceived += (s, e) =>
                            {
                                if (e.Data != null)
                                {
                                    outputBuilder.Append("Output: ").AppendLine(e.Data);
                                }
                            };

                            process.ErrorDataReceived += (s, e) =>
                            {
                                if (e.Data != null)
                                {
                                    outputBuilder.Append("Error: ").AppendLine(e.Data);
                                }
                            };

                            process.Start();
                            process.BeginOutputReadLine();
                            process.BeginErrorReadLine();

                            var timeout = TimeSpan.FromSeconds(10);
                            var exited = process.WaitForExit((int)timeout.TotalMilliseconds);

                            if (!exited)
                            {
                                process.Kill();
                                outputBuilder.AppendLine("System: Execution Timed Out");
                            }

                            if (outputBuilder.Length == 0)
                            {
                                outputBuilder.AppendLine("Output:");
                            }

                            tcs.TrySetResult(outputBuilder.ToString());
                        }
                        catch (Exception ex)
                        {
                            tcs.TrySetException(ex);
                        }
                        finally
                        {
                            File.Delete(tempFile);
                        }
                    });
                }
                else
                {
                    tcs.TrySetResult("System: The User declined to execute this code.");
                }
            }

            if (this.InvokeRequired)
            {
                this.Invoke(ShowReviewForm);
            }
            else
            {
                ShowReviewForm();
            }

            return tcs.Task;
        }

        private static void RenderVolumeDisplay(Graphics g, int width, int height, double volume, string frequency)
        {
            var offColor = Color.FromArgb(45, 71, 60);
            var onColor = Color.FromArgb(226, 255, 255);
            using var off = new SolidBrush(offColor);
            using var on = new SolidBrush(onColor);
            using var bg = new SolidBrush(Color.FromArgb(19, 31, 27));

            g.Clear(Color.Black);
            var w = width * 0.85f;
            var l = (width - w) / 2;
            var h = (float)height;

            g.FillRectangle(off, l, 0, w, h);
            var top = (int)((1 - volume) * h);
            g.FillRectangle(on, l, top, w, h - top);

            var bars = 8;
            var barGap = h / (bars + 1);
            for (var b = 1; b <= bars; b++)
            {
                var y = b * barGap;
                var dy = Math.Max(h / 80, 1);
                g.FillRectangle(bg, l, (int)Math.Round(y - dy), w, (int)Math.Round(2 * dy));
            }

            var points = new List<PointF>();
            for (var px = 1; px < w; px++)
            {
                var x = px / h;
                var py = float.Lerp(0, h, 1 / (8 * x));
                points.Add(new PointF(px + l, Math.Max(py, barGap)));
            }

            points.Add(new PointF(w + l, h));
            g.FillPolygon(bg, points.ToArray());

            var sizeRatioBig = 0.25f;
            var sizeRatioMed = sizeRatioBig * 6 / 7f;

            var family = Digital7;
            var style = FontStyle.Regular;
            using var fontMed = new Font(family, h * sizeRatioMed, style);
            var major = frequency[0..2];
            var majorSize = g.MeasureString(major, fontMed, 0, StringFormat.GenericTypographic);

            using var fontBig = new Font(family, h * sizeRatioBig, style);
            var minor = frequency[2..];
            var minorSize = g.MeasureString(minor, fontBig, 0, StringFormat.GenericTypographic);

            var scalingBig = fontBig.GetHeight(g) / family.GetLineSpacing(style);
            var scalingMed = fontMed.GetHeight(g) / family.GetLineSpacing(style);
            var ascentBig = family.GetCellAscent(style) * scalingBig;
            var descentBig = family.GetCellDescent(style) * scalingBig;
            var ascentMed = family.GetCellAscent(style) * scalingMed;
            var descentMed = family.GetCellDescent(style) * scalingMed;
            minorSize.Height = ascentBig + descentBig;
            majorSize.Height = ascentMed + descentMed;

            var minorPosition = new PointF(w - minorSize.Width - descentBig + l, h - minorSize.Height);
            var majorPosition = new PointF(minorPosition.X - majorSize.Width, h - (ascentMed + descentBig));

            g.DrawString("8.88", fontBig, off, minorPosition, StringFormat.GenericTypographic);
            g.DrawString(minor, fontBig, on, minorPosition, StringFormat.GenericTypographic);

            g.DrawString("88", fontMed, off, majorPosition, StringFormat.GenericTypographic);
            g.DrawString(major, fontMed, on, majorPosition, StringFormat.GenericTypographic);
        }

        private static void RenderStatic(Graphics g, ColorPalette palette, int width, int height)
        {
            using var bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            bmp.Palette = palette;
            BitmapData? data = null;
            try
            {
                data = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                var buffer = new byte[data.Width];
                var scan = data.Scan0;
                for (var y = 0; y < data.Height; y++, scan += data.Stride)
                {
                    Random.Shared.NextBytes(buffer);
                    Marshal.Copy(buffer, 0, scan, buffer.Length);
                }
            }
            finally
            {
                if (data != null)
                {
                    bmp.UnlockBits(data);
                }
            }

            g.DrawImage(bmp, g.VisibleClipBounds);
        }

        private static void RenderFace(Graphics g, ImageSet components, string? eyes = null, string? mouth = null)
        {
            if (components.TryGetValue("base", out var baseComponent))
            {
                g.DrawImageUnscaled(baseComponent.Image, baseComponent.X, baseComponent.Y);
            }

            if (eyes != null && components.TryGetValue(eyes, out var eyesComponent))
            {
                g.DrawImageUnscaled(eyesComponent.Image, eyesComponent.X, eyesComponent.Y);
            }

            if (mouth != null && components.TryGetValue(mouth, out var mouthComponent))
            {
                g.DrawImageUnscaled(mouthComponent.Image, mouthComponent.X, mouthComponent.Y);
            }
        }

        private static Image RenderAnimation(ImageSet frames)
        {
            var gifStream = new MemoryStream();

            var maxX = frames.Values.Max(v => v.X + v.Image.Width);
            var maxY = frames.Values.Max(v => v.Y + v.Image.Height);
            using var surface = new Bitmap(maxX, maxY);
            using var g = Graphics.FromImage(surface);
            using var gif = new AnimatedGifCreator(gifStream, delay: 100);
            {
                var shown = 0;
                for (var f = 0; shown < frames.Count; f++)
                {
                    if (frames.TryGetValue($"frame{f}", out var frameImage))
                    {
                        g.DrawImageUnscaled(frameImage.Image, new Point(frameImage.X, frameImage.Y));
                        gif.AddFrame(surface);
                        shown++;
                    }
                }
            }

            gifStream.Seek(0, SeekOrigin.Begin);
            return Image.FromStream(gifStream);
        }

        private void SpeechBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                this.sayButton.PerformClick();
            }
        }

        private async void SayButton_Click(object sender, EventArgs e)
        {
            this.ActiveCharacter = "User";
            var text = this.captionLabel.Text = this.speechBox.Text;
            this.speechBox.Text = string.Empty;
            this.speechBox.Focus();

            try
            {
                this.progressIndicator.Visible = true;
                await this.conversationModel.AddUserMessageAsync(text).ConfigureAwait(true);
            }
            catch (Exception)
            {
            }
            finally
            {
                this.progressIndicator.Visible = false;
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Abort;
            this.Close();
        }

        private void ConversationModel_TokenReceived(object? sender, ConversationModel.TokenReceivedArgs e)
        {
            var bogies = (this.progressIndicator.Tag as ImmutableList<Bogie>) ?? [];
            float x, y;
            do
            {
                (x, y) = (Random.Shared.NextSingle(), Random.Shared.NextSingle());
            }
            while (((x - 0.5f) * (x - 0.5f)) + ((y - 0.5f) * (y - 0.5f)) > 0.5f);

            bogies = bogies.Add(new Bogie(DateTime.UtcNow, new(x, y)));
            this.progressIndicator.Tag = bogies;
        }

        private void ProgressIndicator_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var now = DateTime.UtcNow;
            var spinTime = TimeSpan.FromSeconds(2);

            var w = g.VisibleClipBounds.Width;
            var h = g.VisibleClipBounds.Height;
            var center = new PointF(w / 2f, h / 2f);
            var radius = Math.Min(w, h) / 3f;
            var angle = (now - DateTime.UnixEpoch) / spinTime % 1.0 * Math.Tau;
            var end = new PointF(
                center.X + radius * (float)Math.Cos(angle),
                center.Y + radius * (float)Math.Sin(angle));

            var bounds = new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2);
            using var path = new GraphicsPath();
            path.AddPie(bounds.X, bounds.Y, bounds.Width, bounds.Height, (float)((angle - 0.8f) * 360 / Math.Tau), (float)(0.8f * 360 / Math.Tau));
            g.FillPath(Brushes.Green, path);

            var lineWidth = 2;
            using var pen = new Pen(Color.Gray, lineWidth);
            g.DrawEllipse(pen, bounds);
            g.DrawLine(Pens.Gray, center, end);

            var bogieSize = 2f;
            var innerRadius = radius - lineWidth - bogieSize;
            var bogies = (this.progressIndicator.Tag as ImmutableList<Bogie>) ?? [];
            bogies = bogies.RemoveAll(b => now - b.Appeared > spinTime);
            foreach (var bogie in bogies)
            {
                var bogieAngle = (bogie.Appeared - DateTime.UnixEpoch) / spinTime % 1.0 * Math.Tau;
                var bogiePoint = new PointF(
                    float.Lerp(center.X - innerRadius, center.X + innerRadius, bogie.Location.X),
                    float.Lerp(center.Y - innerRadius, center.Y + innerRadius, bogie.Location.Y));

                var brightness = 1 - (now - bogie.Appeared) / spinTime;
                var alpha = (int)Math.Clamp(brightness * 255, 0, 255);
                using var brush = new SolidBrush(Color.FromArgb(alpha, Color.White));
                g.FillEllipse(brush, bogiePoint.X - bogieSize / 2, bogiePoint.Y - bogieSize / 2, bogieSize, bogieSize);
            }

            this.progressIndicator.Tag = bogies;
        }

        private static void MoveToRightmostBottomCorner(Form form)
        {
            ArgumentNullException.ThrowIfNull(form);

            var rightmost = Screen.AllScreens
                .OrderByDescending(s => s.Bounds.Right)
                .Select(s => s.WorkingArea)
                .First();

            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(
                rightmost.Right - form.Width,
                rightmost.Bottom - form.Height);
        }

        [LibraryImport("gdi32.dll", SetLastError = true)]
        private static partial IntPtr AddFontMemResourceEx(
            IntPtr pbFont,
            uint cbFont,
            IntPtr pdv,
            ref uint pcFonts);

        public static FontFamily LoadEmbeddedFont(string fontName)
        {
            using var stream = typeof(CodecDisplay).Assembly.GetManifestResourceStream($"{typeof(CodecDisplay).Namespace}.{fontName}")!;

            var fontData = new byte[stream.Length];
            stream.Read(fontData, 0, (int)stream.Length);

            var fontPtr = Marshal.AllocCoTaskMem(fontData.Length);
            Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
            uint fonts = 0;
            AddFontMemResourceEx(fontPtr, (uint)fontData.Length, 0, ref fonts);

            var privateFonts = new PrivateFontCollection();
            privateFonts.AddMemoryFont(fontPtr, fontData.Length);

            return privateFonts.Families[0];
        }

        private record class Bogie(DateTime Appeared, PointF Location);
    }
}
