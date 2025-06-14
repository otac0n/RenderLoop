// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.Codec
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;
    using AnimatedGif;
    using DiscUtils.Streams;
    using Microsoft.Extensions.DependencyInjection;
    using RenderLoop.Demo.MGS.Codec.Conversation;
    using ImageSet = System.Collections.Immutable.ImmutableDictionary<string, (int X, int Y, System.Drawing.Bitmap Image)>;

    internal class CodecDisplay : Form
    {
        private ConversationModel conversationModel;
        private Timer updateTimer;

        private static Dictionary<string, string> IdLookup = new()
        {
            { "f73b", "Solid Snake" },
            { "ae23", "Solid Snake" },
            { "a2ca", "Solid Snake" },
            { "3e2d", "Solid Snake" },
            { "7228", "Solid Snake" },
            { "3108", "Solid Snake" },
            { "3078", "Solid Snake" },
            { "2272", "Solid Snake" },
            { "0b7e", "Solid Snake" },
            { "c265", "Solid Snake" },
            { "e6eb", "Solid Snake" },
            { "36b4", "Solid Snake" },
            { "2089", "Solid Snake" },
            { "59f8", "Solid Snake" },
            { "da69", "Solid Snake" },
            { "1c7e", "Solid Snake" },
            { "0d84", "Solid Snake" },
            { "bc7b", "Solid Snake" },
            { "3320", "Roy Campbell" },
            { "ae0c", "Roy Campbell" },
            { "7a11", "Roy Campbell" },
            { "5e56", "Roy Campbell" },
            { "1a37", "Roy Campbell" },
            { "a927", "Roy Campbell" },
            { "a472", "Roy Campbell" },
            { "bb69", "Roy Campbell" },
            { "21f3", "Naomi Hunter" },
            { "9cdf", "Naomi Hunter" },
            { "68e4", "Naomi Hunter" },
            { "b96e", "Naomi Hunter" },
            { "fd17", "Naomi Hunter" },
            { "b176", "Naomi Hunter" },
            { "de08", "Naomi Hunter" },
            { "f1aa", "Naomi Hunter" },
            { "2118", "Naomi Hunter" },
            { "7c87", "Naomi Hunter" },
            { "25a1", "Naomi Hunter" },
            { "f0ef", "Naomi Hunter" },
            { "6f74", "Naomi Hunter" },
            { "5347", "Mei Ling" },
            { "6244", "Mei Ling" },
            { "ce33", "Mei Ling" },
            { "7e7d", "Mei Ling" },
            { "2c6a", "Mei Ling" },
            { "1091", "Mei Ling" },
            { "dcf4", "Mei Ling" },
            { "c60f", "Mei Ling" },
            { "fe9f", "Mei Ling" },
            { "40b0", "Mei Ling" },
            { "ad5d", "Hal Emmerich" },
            { "ec59", "Hal Emmerich" },
            { "284a", "Hal Emmerich" },
            { "9c70", "Hal Emmerich" },
            { "3069", "Hal Emmerich" },
            { "74a7", "Hal Emmerich" },
            { "9cc0", "Liquid Snake" },
            { "17ad", "Liquid Snake" },
            { "d6ef", "Liquid Snake" },
            { "6a21", "Liquid Snake" },
            { "99c1", "Liquid Snake" },
            { "80d8", "Liquid Snake" },
            { "2f79", "Liquid Snake" },
            { "158d", "Nastasha Romanenko" },
            { "1e41", "Nastasha Romanenko" },
            { "9079", "Nastasha Romanenko" },
            { "40c3", "Nastasha Romanenko" },
            { "7702", "Meryl Silverburgh" },
            { "7d66", "Meryl Silverburgh" },
            { "39c3", "Meryl Silverburgh" },
            { "6d84", "Meryl Silverburgh" },
            { "b4af", "Meryl Silverburgh" },
            { "1162", "Meryl Silverburgh" },
            { "0cc2", "Meryl Silverburgh" },
            { "64f9", "Meryl Silverburgh" },
            { "3d59", "Meryl Silverburgh" },
            { "8d32", "Meryl Silverburgh" },
            { "dce9", "Meryl Silverburgh" },
            { "3d63", "Sniper Wolf" },
            { "b84f", "Sniper Wolf" },
            { "124a", "Sniper Wolf" },
            { "6899", "Sniper Wolf" },
            { "a83c", "Sniper Wolf" },
            { "93f9", "Jim Houseman" },
            { "bf2f", "Jim Houseman" },
        };

        public CodecDisplay(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<Program.Options>();
            var codecOptions = serviceProvider.GetRequiredService<CodecOptions>();
            var facesStream = serviceProvider.GetRequiredKeyedService<SparseStream>((options.File, WellKnownPaths.CD1Path, WellKnownPaths.FaceDatPath));
            var source = ImageLoader.LoadImages(facesStream);

            this.Width = 500;
            this.Height = 500;

            var avatars = new Dictionary<string, (AvatarState State, Control Control)>();

            var captionLabel = new Label()
            {
                Text = "",
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font(this.Font, FontStyle.Bold),
            };

            if (codecOptions.LMEndpoint != null)
            {
                this.conversationModel = new ConversationModel(codecOptions, async response =>
                {
                    var character = response.Name;
                    if (avatars.TryGetValue(character, out var avatar))
                    {
                        ShowAvatar(character, response.Text);

                        // TODO: Set Mood.
                        await avatar.State.SayAsync(response.Text).ConfigureAwait(false);
                    }
                });
            }

            void ShowAvatar(string name, string caption)
            {
                if (!this.InvokeRequired)
                {
                    if (avatars.TryGetValue(name, out var selected))
                    {
                        foreach (var avatar in avatars.Values)
                        {
                            avatar.Control.Visible = avatar.Control == selected.Control;
                        }
                    }

                    captionLabel.Text = caption;
                }
                else
                {
                    this.Invoke(() => ShowAvatar(name, caption));
                }
            }

            this.updateTimer = new Timer()
            {
                Interval = 1000 / 30,
            };
            this.updateTimer.Enabled = true;

            var parent = new FlowLayoutPanel()
            {
                BackColor = Color.Black,
                ForeColor = Color.Green,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
            };

            var inputsPanel = new FlowLayoutPanel()
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
            };

            var speechBox = new TextBox()
            {
                Text = "Hey, snake! Get your head in the game.",
                Width = 300,
            };

            var sayButton = new Button()
            {
                Text = "Say",
                AutoSize = true,
            };

            sayButton.Click += (s, e) =>
            {
                this.conversationModel.AddUserMessage(speechBox.Text);
            };

            inputsPanel.Controls.Add(speechBox);
            inputsPanel.Controls.Add(sayButton);
            parent.Controls.Add(inputsPanel);

            var byRawId = source.GroupBy(i => i.Key, i => i.Value);

            foreach (var group in byRawId.GroupBy(g => IdLookup.TryGetValue(g.Key, out var id) ? id : g.Key, g => g.First()))
            {
                var panel = new FlowLayoutPanel()
                {
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = false,
                    AutoSize = true,
                };

                var avatarState = new AvatarState(codecOptions, group.Key);
                foreach (var set in group.Select((s, i) => (Images: s, Index: i)).OrderByDescending(x => x.Images.ContainsKey("base")).ThenByDescending(x => x.Images.Count).Take(1))
                {
                    var images = set.Images;

                    var maxX = images.Values.Max(v => v.X + v.Image.Width);
                    var maxY = images.Values.Max(v => v.Y + v.Image.Height);
                    var display = new PictureBox
                    {
                        Size = new Size(maxX * 2, maxY * 2),
                        SizeMode = PictureBoxSizeMode.Zoom,
                    };

                    if (images.TryGetValue("base", out var baseImage))
                    {
                        if (images.Count == 1)
                        {
                            display.Image = baseImage.Image;
                        }
                        else
                        {
                            var surface = new Bitmap(maxX, maxY);
                            var g = Graphics.FromImage(surface);

                            void Render(object? sender, EventArgs args)
                            {
                                if (!this.InvokeRequired)
                                {
                                    this.RenderFace(g, images, avatarState.Eyes, avatarState.Mouth);
                                    display.Invalidate();
                                }
                                else
                                {
                                    this.Invoke(() => Render(sender, args));
                                }
                            }

                            avatarState.Updated += Render;
                            this.updateTimer.Tick += (e, a) => avatarState.Update();
                            display.Image = surface;
                        }
                    }
                    else
                    {
                        display.Image = RenderAnimation(images);
                    }

                    panel.Controls.Add(display);
                }

                panel.Controls.Add(new Label()
                {
                    Text = group.Key,
                    AutoSize = true,
                });

                parent.Controls.Add(panel);
                avatars.Add(group.Key, (avatarState, panel));
            }

            parent.Controls.Add(captionLabel);

            this.Controls.Add(parent);
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
