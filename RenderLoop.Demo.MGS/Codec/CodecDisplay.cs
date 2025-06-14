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

    internal partial class CodecDisplay : Form
    {
        private ConversationModel conversationModel;

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
            this.InitializeComponent();

            var options = serviceProvider.GetRequiredService<Program.Options>();
            var codecOptions = serviceProvider.GetRequiredService<CodecOptions>();
            var facesStream = serviceProvider.GetRequiredKeyedService<SparseStream>((options.File, WellKnownPaths.CD1Path, WellKnownPaths.FaceDatPath));
            var source = ImageLoader.LoadImages(facesStream);

            var maxX = source.Values.SelectMany(x => x.Values.Select(v => v.X + v.Image.Width)).Max();
            var maxY = source.Values.SelectMany(x => x.Values.Select(v => v.Y + v.Image.Height)).Max();

            var surface = new Bitmap(maxX, maxY);
            var g = Graphics.FromImage(surface);
            this.display.Image = surface;

            string? activeCharacter = this.nameLabel.Text;

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
        }

        private void SayButton_Click(object sender, EventArgs e)
        {
            this.conversationModel.AddUserMessage(this.speechBox.Text);
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
