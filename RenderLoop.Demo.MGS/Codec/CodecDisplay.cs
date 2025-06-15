// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.Codec
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using AnimatedGif;
    using DiscUtils.Streams;
    using Microsoft.Extensions.DependencyInjection;
    using RenderLoop.Demo.MGS.Codec.Conversation;
    using ImageSet = System.Collections.Immutable.ImmutableDictionary<string, (int X, int Y, System.Drawing.Bitmap Image)>;

    internal partial class CodecDisplay : Form
    {
        private ConversationModel conversationModel;

        private static Dictionary<(string Mood, string Tags), double> MoodMappingScores = new()
        {
            { ("Frowning", "Frown"), 1.0 },
            { ("Frustrated", "Frown"), 1.0 },
            { ("Serious", "Frown"), 1.0 },
            { ("Gruff", "Frown"), 1.0 },
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

        private static Dictionary<string, (string Id, string Tags)[]> CharacterImages = new()
        {
            ["Solid Snake"] = [
                ("f73b", "Neutral"),
                ("ae23", "Frown"),
                ("a2ca", "Looking Down, Eyes Closed"),
                ("3e2d", "Baring Teeth"),
                ("7228", "Smile"),
                ("3108", "Laugh?"),
                ("3078", "Laugh?"),
                ("2272", "Yell"),
                ("0b7e", "Nude, Neutral"),
                ("c265", "Nude, Frown"),
                ("e6eb", "Nude, Looking Down, Eyes Closed"),
                ("36b4", "Nude, Angry / Yell"),
                ("2089", "Nude, Neutral (duplicate)"),
                ("59f8", "Suited, Neutral"),
                ("da69", "Looking Down, Eyes Closed"),
                ("1c7e", "Nude, Looking Down, Eyes Closed"),
                ("0d84", "Nude, Looking Down, Eyes Closed"),
                ("bc7b", "Looking Down, Eyes Closed"),
            ],
            ["Roy Campbell"] = [
                ("3320", "Neutral"),
                ("ae0c", "Smile"),
                ("7a11", "Surprised"),
                ("5e56", "Frown"),
                ("1a37", "Yell"),
                ("a927", "Sad"), // Eyes Closed
                ("a472", "Reserved"),
                ("bb69", "Dumbfounded"),
            ],
            ["Naomi Hunter"] = [
                ("21f3", "Neutral"),
                ("9cdf", "Smile"),
                ("68e4", "Surprised"),
                ("b96e", "Reserved"),
                ("fd17", "Concerned"),
                ("b176", "Sad"),
                ("de08", "Frown"), // Eyes Closed
                ("f1aa", "Frown"), // Eyes Closed
                ("2118", "Frown"), // Eyes Closed, Shake 'no'
                ("7c87", "Resigned"), // Eyes Closed
                ("25a1", "Pain"), // Eyes Closed
                ("f0ef", "Hide Pain"),
                ("6f74", "Defiant / Hold Back Tears"),
            ],
            ["Mei Ling"] = [
                ("5347", "Neutral"),
                ("6244", "Mischievous"),
                ("ce33", "Smile"),
                ("7e7d", "Enthusiastic"),
                ("2c6a", "Mid Blink"),
                ("1091", "Mid Blink"),
                ("dcf4", "Concerned"),
                ("c60f", "Left-eye Wink"),
                ("fe9f", "Tongue Out"),
                ("40b0", "Wiggle"),
            ],
            ["Hal Emmerich"] = [
                ("ad5d", "Neutral"),
                ("ec59", "Neutral"), // Lens Shine
                ("284a", "Smile"),
                ("9c70", "Frown"),
                ("3069", "Yell"), // Close-up
                ("74a7", "Concerned"),
            ],
            ["Liquid Snake"] = [
                ("9cc0", "Neutral"), // Miller
                ("17ad", "Smile"), // Miller
                ("d6ef", "Wince"), // Miller
                ("6a21", "Smirk"), // Miller
                ("99c1", "Neutral"), // Liquid
                ("80d8", "Frown"), // Liquid
                ("2f79", "Miller -> Liquid Reveal"),
            ],
            ["Nastasha Romanenko"] = [
                ("158d", "Neutral"),
                ("1e41", "Looking Down, Eyes Closed"),
                ("9079", "Smile"),
                ("40c3", "Concerned"),
            ],
            ["Meryl Silverburgh"] = [
                ("7702", "Masked"),
                ("7d66", "Doff Mask"),
                ("39c3", "Neutral"),
                ("6d84", "Don Mask"),
                ("b4af", "Smile"),
                ("1162", "Grin"),
                ("0cc2", "Looking Down, Eyes Closed"),
                ("64f9", "Frown"),
                ("3d59", "Looking Down, Eyes Closed"),
                ("8d32", "Turn To Screen Left"),
                ("dce9", "Facing Screen Left"),
            ],
            ["Sniper Wolf"] = [
                ("3d63", "Neutral"),
                ("b84f", "Smile"),
                ("124a", "Grin"),
                ("6899", "Frown"),
                ("a83c", "Neutral"),
            ],
            ["Jim Houseman"] = [
                ("93f9", "Neutral"),
                ("bf2f", "Frown"),
            ],
        };

        public CodecDisplay(IServiceProvider serviceProvider)
        {
            this.InitializeComponent();

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

            var activeCharacter = this.nameLabel.Text;
            var volume = 0.0;
            this.updateTimer.Tick += (s, e) =>
            {
                volume *= 0.9;
                this.RenderVolumeDisplay(vu, vuW, vuH, volume);
                this.volumeMeter.Invalidate();
            };

            var avatars = new Dictionary<string, AvatarState>();

            void Render()
            {
                if (!this.InvokeRequired)
                {
                    if (activeCharacter != null && avatars.TryGetValue(activeCharacter, out var avatarState))
                    {
                        var images = (from x in CharacterImages[activeCharacter]
                                      let s = source[x.Id]
                                      where s.ContainsKey("base") && s.Count > 1
                                      let score = avatarState.Mood == x.Tags ? 1 :
                                                  MoodMappingScores.TryGetValue((avatarState.Mood, x.Tags), out var sc) ? sc :
                                                  0
                                      orderby score descending, x.Tags == "Neutral" descending
                                      select s).First();
                        this.RenderFace(g, images, avatarState.Eyes, avatarState.Mouth);
                        this.display.Invalidate();
                    }
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
                    this.nameLabel.Text = activeCharacter = name;
                    this.captionLabel.Text = caption;
                    Render();
                }
                else
                {
                    this.Invoke(() => ShowAvatar(name, caption));
                }
            }

            foreach (var group in CharacterImages)
            {
                var name = group.Key;
                var avatarState = new AvatarState(codecOptions, name);
                this.updateTimer.Tick += (e, a) => avatarState.Update();
                avatarState.Updated += (e, a) =>
                {
                    volume = Math.Max(volume, avatarState.Volume);
                    if (name == activeCharacter)
                    {
                        Render();
                    }
                };
                avatars.Add(name, avatarState);
            }

            if (codecOptions.LMEndpoint != null)
            {
                this.conversationModel = new ConversationModel(
                    codecOptions,
                    async response =>
                    {
                        var character = response.Name;
                        if (avatars.TryGetValue(character, out var avatarState))
                        {
                            ShowAvatar(character, response.Text);

                            avatarState.Mood = response.Mood;
                            await avatarState.SayAsync(response.Text).ConfigureAwait(false);
                        }
                    },
                    RunCodeWithUserReview);
            }
        }

        private void SayButton_Click(object sender, EventArgs e)
        {
            this.conversationModel.AddUserMessage(this.speechBox.Text);
            this.speechBox.Text = string.Empty;
        }

        public Task<string> RunCodeWithUserReview(CodeResponse codeResponse)
        {
            var tcs = new TaskCompletionSource<string>();

            void ShowReviewForm()
            {
                var form = new Form
                {
                    Text = "Review Code",
                    Width = 800,
                    Height = 600,
                    StartPosition = FormStartPosition.CenterParent,
                };

                var textBox = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Both,
                    Dock = DockStyle.Fill,
                    Text = codeResponse.Code,
                    Font = new Font("Consolas", 10),
                };

                var approveButton = new Button
                {
                    Text = "Approve",
                    DialogResult = DialogResult.OK,
                };

                var denyButton = new Button
                {
                    Text = "Deny",
                    DialogResult = DialogResult.Cancel,
                };

                var buttonPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    FlowDirection = FlowDirection.RightToLeft,
                    Height = 40,
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

        private void RenderVolumeDisplay(Graphics g, int w, int h, double volume)
        {
            g.Clear(Color.Gray);
            var top = (int)((1 - volume) * h);
            g.FillRectangle(Brushes.White, 0, top, w, h - top);

            var points = new List<Point>();
            for (var px = 1; px < w; px++)
            {
                var x = (double)px / h;
                var py = (int)double.Lerp(0, h, 1.0 / (8 * x));
                points.Add(new Point(px, py));
            }

            points.Add(new Point(w, h));

            using (Brush brush = new SolidBrush(Color.FromArgb(0, 0, 0, 0)))
            {
                g.CompositingMode = CompositingMode.SourceCopy;

                g.FillPolygon(brush, points.ToArray());
                var bars = 8;
                for (var b = 1; b <= bars; b++)
                {
                    var y = b * (h / (bars + 1));
                    var dy = Math.Max(h / 80.0, 1);
                    g.FillRectangle(brush, 0, (int)(y - dy), w, (int)(2 * dy));
                }

                g.CompositingMode = CompositingMode.SourceOver;
            }
        }

        private void RenderFace(Graphics g, ImageSet components, string? eyes = null, string? mouth = null)
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

        private Image RenderAnimation(ImageSet frames)
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
    }
}
