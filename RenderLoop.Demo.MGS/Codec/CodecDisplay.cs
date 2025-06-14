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

        private static Dictionary<string, string> IdLookup = new()
        {
            { "f73b", "Solid Snake" }, // Neutral
            { "ae23", "Solid Snake" }, // Frown
            { "a2ca", "Solid Snake" }, // Looking Down, Eyes Closed
            { "3e2d", "Solid Snake" }, // Baring Teeth
            { "7228", "Solid Snake" }, // Smiling
            { "3108", "Solid Snake" }, // Laugh?
            { "3078", "Solid Snake" }, // Laugh?
            { "2272", "Solid Snake" }, // Yelling
            { "0b7e", "Solid Snake" }, // Nude, Neutral
            { "c265", "Solid Snake" }, // Nude, Frown
            { "e6eb", "Solid Snake" }, // Nude, Looking Down, Eyes Closed
            { "36b4", "Solid Snake" }, // Nude, Angry / Yelling
            { "2089", "Solid Snake" }, // Nude, Neutral (duplicate)
            { "59f8", "Solid Snake" }, // Suited, Neutral
            { "da69", "Solid Snake" }, // Looking Down, Eyes Closed
            { "1c7e", "Solid Snake" }, // Nude, Looking Down, Eyes Closed
            { "0d84", "Solid Snake" }, // Nude, Looking Down, Eyes Closed
            { "bc7b", "Solid Snake" }, // Looking Down, Eyes Closed
            { "3320", "Roy Campbell" }, // Neutral
            { "ae0c", "Roy Campbell" }, // Smiling
            { "7a11", "Roy Campbell" }, // Surprised
            { "5e56", "Roy Campbell" }, // Frown
            { "1a37", "Roy Campbell" }, // Yelling
            { "a927", "Roy Campbell" }, // Sad, Eyes Closed
            { "a472", "Roy Campbell" }, // Reserved
            { "bb69", "Roy Campbell" }, // Dumbfounded
            { "21f3", "Naomi Hunter" }, // Neutral
            { "9cdf", "Naomi Hunter" }, // Smiling
            { "68e4", "Naomi Hunter" }, // Surprised
            { "b96e", "Naomi Hunter" }, // Reserved
            { "fd17", "Naomi Hunter" }, // Concerned
            { "b176", "Naomi Hunter" }, // Sad / Hurt
            { "de08", "Naomi Hunter" }, // Frowning / Eyes Closed
            { "f1aa", "Naomi Hunter" }, // Frowning / Eyes Closed
            { "2118", "Naomi Hunter" }, // Frowning / Eyes Closed, Shaking "no"
            { "7c87", "Naomi Hunter" }, // Resigned / Eyes Closed
            { "25a1", "Naomi Hunter" }, // Pain / Eyes Closed
            { "f0ef", "Naomi Hunter" }, // Hiding Pain
            { "6f74", "Naomi Hunter" }, // Defiant / Holding Back Tears
            { "5347", "Mei Ling" }, // Neutral
            { "6244", "Mei Ling" }, // Mischievous
            { "ce33", "Mei Ling" }, // Smiling
            { "7e7d", "Mei Ling" }, // Enthusiastic
            { "2c6a", "Mei Ling" }, // Mid Blink
            { "1091", "Mei Ling" }, // Mid Blink
            { "dcf4", "Mei Ling" }, // Concerned
            { "c60f", "Mei Ling" }, // Left-eye Wink
            { "fe9f", "Mei Ling" }, // Tongue Out
            { "40b0", "Mei Ling" }, // Wiggle
            { "ad5d", "Hal Emmerich" }, // Neutral
            { "ec59", "Hal Emmerich" }, // Neutral, Lens Shine
            { "284a", "Hal Emmerich" }, // Smiling
            { "9c70", "Hal Emmerich" }, // Frown
            { "3069", "Hal Emmerich" }, // Yelling, Close-up
            { "74a7", "Hal Emmerich" }, // Questioning
            { "9cc0", "Liquid Snake" }, // Miller, Neutral
            { "17ad", "Liquid Snake" }, // Miller, Smile
            { "d6ef", "Liquid Snake" }, // Miller, Wince
            { "6a21", "Liquid Snake" }, // Miller, Smirk
            { "99c1", "Liquid Snake" }, // Liquid, Neutral
            { "80d8", "Liquid Snake" }, // Liquid, Frown
            { "2f79", "Liquid Snake" }, // Miller -> Liquid Reveal
            { "158d", "Nastasha Romanenko" }, // Neutral
            { "1e41", "Nastasha Romanenko" }, // Looking Down, Eyes Closed
            { "9079", "Nastasha Romanenko" }, // Smiling
            { "40c3", "Nastasha Romanenko" }, // Concerned
            { "7702", "Meryl Silverburgh" }, // Masked
            { "7d66", "Meryl Silverburgh" }, // Doff Mask
            { "39c3", "Meryl Silverburgh" }, // Neutral
            { "6d84", "Meryl Silverburgh" }, // Don Mask
            { "b4af", "Meryl Silverburgh" }, // Smile
            { "1162", "Meryl Silverburgh" }, // Grin
            { "0cc2", "Meryl Silverburgh" }, // Looking Down, Eyes Closed
            { "64f9", "Meryl Silverburgh" }, // Frown
            { "3d59", "Meryl Silverburgh" }, // Looking Down, Eyes Closed
            { "8d32", "Meryl Silverburgh" }, // Turn To Screen Left
            { "dce9", "Meryl Silverburgh" }, // Facing Screen Left
            { "3d63", "Sniper Wolf" }, // Neutral
            { "b84f", "Sniper Wolf" }, // Smile
            { "124a", "Sniper Wolf" }, // Grin
            { "6899", "Sniper Wolf" }, // Frown
            { "a83c", "Sniper Wolf" }, // Neutral
            { "93f9", "Jim Houseman" }, // Neutral
            { "bf2f", "Jim Houseman" }, // Frown
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

            var reverseLookup = IdLookup.ToLookup(p => p.Value, p => p.Key);
            var avatars = new Dictionary<string, (AvatarState State, ImageSet Images)>();

            void Render()
            {
                if (!this.InvokeRequired)
                {
                    if (activeCharacter != null && avatars.TryGetValue(activeCharacter, out var avatar))
                    {
                        this.RenderFace(g, avatar.Images, avatar.State.Eyes, avatar.State.Mouth);
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

            foreach (var group in reverseLookup)
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
                var images = group.Select(id => source[id]).OrderByDescending(s => s.ContainsKey("base")).ThenByDescending(s => s.Count).First();
                avatars.Add(name, (avatarState, images));
            }

            if (codecOptions.LMEndpoint != null)
            {
                this.conversationModel = new ConversationModel(
                    codecOptions,
                    async response =>
                    {
                        var character = response.Name;
                        if (avatars.TryGetValue(character, out var avatar))
                        {
                            ShowAvatar(character, response.Text);

                            // TODO: Set Mood.
                            await avatar.State.SayAsync(response.Text).ConfigureAwait(false);
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
