// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.Codec
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using AnimatedGif;
    using DiscUtils.Streams;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.Extensions.DependencyInjection;

    internal class CodecDisplay : Form
    {
        private static class BlinkBehavior
        {
            private static readonly double MinSeconds = 2.0;
            private static readonly double MaxSeconds = 8.0;
            private static readonly double AttentionExponent = 2.0;

            public static bool StartAutomaticBlink(DateTime now, DateTime? lastBlinkTime, double attention)
            {
                if (lastBlinkTime == null)
                {
                    return true;
                }

                attention = Math.Clamp(attention, 0.0, 1.0);

                var blinkInterval = TimeSpan.FromSeconds(double.Lerp(MinSeconds, MaxSeconds, Math.Pow(1.0 - attention, AttentionExponent)));
                blinkInterval *= double.Lerp(0.95, 1.05, Random.Shared.NextDouble());

                return now - lastBlinkTime >= blinkInterval;
            }
        }

        private sealed class AvatarState : IDisposable
        {
            private readonly SpeechSynthesizer synth;
            private int eyeState;
            private DateTime? lastBlinkTime;
            private uint lastViseme;

            public AvatarState(string speechEndpoint, string speechKey, string voiceName)
            {
                var speechConfig = SpeechConfig.FromEndpoint(new Uri(speechEndpoint), speechKey);
                speechConfig.SpeechSynthesisVoiceName = voiceName;
                this.synth = new(speechConfig);
                this.synth.VisemeReceived += this.Synth_VisemeReceived;
            }

            public event EventHandler<EventArgs>? Updated;

            public double Attention { get; set; } = 0.5;

            public string? Eyes => this.eyeState == 0 ? null : this.eyeState % 2 == 0 ? "eyes-blink" : "eyes-droop";

            public string? Mouth => this.lastViseme switch
            {
                1 or 2 or 3 or 4 or 5 or 6 or 15 or 16 => "mouth-a",
                8 or 9 or 10 or 11 or 12 or 13 or 14 or 17 or 20 => "mouth-e",
                0 or 7 or 18 or 19 or 21 => null,
            };

            public async Task SayAsync(string text)
            {
                await this.synth.SpeakTextAsync(text).ConfigureAwait(true);
            }

            public void Update()
            {
                var now = DateTime.Now;
                var updated = false;

                if (this.eyeState == 0)
                {
                    if (BlinkBehavior.StartAutomaticBlink(now, this.lastBlinkTime, this.Attention))
                    {
                        this.eyeState = 1;
                        updated = true;
                    }
                }
                else
                {
                    this.eyeState = (this.eyeState + 1) % 4;
                    this.lastBlinkTime = now;
                    updated = true;
                }

                if (updated)
                {
                    this.Updated?.Invoke(this, new());
                }
            }

            public void Dispose()
            {
                this.synth.Dispose();
            }

            private void Synth_VisemeReceived(object? sender, SpeechSynthesisVisemeEventArgs e)
            {
                var shape = this.Mouth;
                this.lastViseme = e.VisemeId;
                if (this.Mouth != shape)
                {
                    this.Updated?.Invoke(this, new());
                }
            }
        }

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

        private static Dictionary<string, string> VoiceData = new()
        {
            { "Solid Snake", "en-US-DerekMultilingualNeural" },
            { "Roy Campbell", "en-US-LewisMultilingualNeural" },
            { "Naomi Hunter", "en-US-LunaNeural" },
            { "Mei Ling", "en-US-AmberNeural" },
            { "Hal Emmerich", "en-US-TonyNeural" },
            { "Liquid Snake", "en-US-AndrewMultilingualNeural" },
            { "Nastasha Romanenko", "en-US-CoraNeural" },
            { "Meryl Silverburgh", "en-US-AvaNeural" },
            { "Sniper Wolf", "en-US-NancyNeural" },
            { "Jim Houseman", "en-US-DavisNeural" },
        };

        public CodecDisplay(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<Program.Options>();
            var codecOptions = serviceProvider.GetRequiredService<CodecOptions>();
            var facesStream = serviceProvider.GetRequiredKeyedService<SparseStream>((options.File, WellKnownPaths.CD1Path, WellKnownPaths.FaceDatPath));
            var source = UnpackFaces(facesStream);

            this.Width = 2200;
            this.Height = 2000;

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
            inputsPanel.Controls.Add(speechBox);
            parent.Controls.Add(inputsPanel);

            var byRawId = source.GroupBy(i => i.id, i => i.images);

            foreach (var group in byRawId.GroupBy(g => IdLookup.TryGetValue(g.Key, out var id) ? id : g.Key, g => g.First()))
            {
                var panel = new FlowLayoutPanel()
                {
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = false,
                    AutoSize = true,
                };

                panel.Controls.Add(new Label()
                {
                    Text = group.Key,
                    AutoSize = true,
                });

                VoiceData.TryGetValue(group.Key, out var voiceData);

                var avatarState = new AvatarState(codecOptions.SpeechEndpoint!, codecOptions.SpeechKey!, voiceData!);
                foreach (var set in group.Select((s, i) => (images: s, index: i)))
                {
                    var images = set.images;

                    var maxX = images.Values.Max(v => v.x + v.image.Width);
                    var maxY = images.Values.Max(v => v.y + v.image.Height);
                    var display = new PictureBox
                    {
                        Size = new Size(maxX * 2, maxY * 2),
                        SizeMode = PictureBoxSizeMode.Zoom,
                    };

                    if (images.TryGetValue("base", out var baseImage))
                    {
                        if (images.Count == 1)
                        {
                            display.Image = baseImage.image;
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

                var speakButton = new Button()
                {
                    Text = "Speak",
                    AutoSize = true,
                };
                speakButton.Click += (e, a) => avatarState.SayAsync(speechBox.Text);
                panel.Controls.Add(speakButton);

                parent.Controls.Add(panel);
            }

            this.Controls.Add(parent);
        }

        private void RenderFace(Graphics g, ImmutableDictionary<string, (int x, int y, Bitmap image)> components, string? eyes = null, string? mouth = null)
        {
            if (components.TryGetValue("base", out var baseComponent))
            {
                g.DrawImageUnscaled(baseComponent.image, baseComponent.x, baseComponent.y);
            }

            if (eyes != null && components.TryGetValue(eyes, out var eyesComponent))
            {
                g.DrawImageUnscaled(eyesComponent.image, eyesComponent.x, eyesComponent.y);
            }

            if (mouth != null && components.TryGetValue(mouth, out var mouthComponent))
            {
                g.DrawImageUnscaled(mouthComponent.image, mouthComponent.x, mouthComponent.y);
            }
        }

        private Image RenderAnimation(ImmutableDictionary<string, (int x, int y, Bitmap image)> frames)
        {
            var gifStream = new MemoryStream();

            var maxX = frames.Values.Max(v => v.x + v.image.Width);
            var maxY = frames.Values.Max(v => v.y + v.image.Height);
            using var surface = new Bitmap(maxX, maxY);
            using var g = Graphics.FromImage(surface);
            using var gif = new AnimatedGifCreator(gifStream, delay: 100);
            {
                var shown = 0;
                for (var f = 0; shown < frames.Count; f++)
                {
                    if (frames.TryGetValue($"frame{f}", out var frameImage))
                    {
                        g.DrawImageUnscaled(frameImage.image, new Point(frameImage.x, frameImage.y));
                        gif.AddFrame(surface);
                        shown++;
                    }
                }
            }

            gifStream.Seek(0, SeekOrigin.Begin);
            return Image.FromStream(gifStream);
        }

        public static IEnumerable<(string id, ImmutableDictionary<string, (int x, int y, Bitmap image)> images)> UnpackFaces(Stream source)
        {
            const int PALETTE_SIZE = 256;

            var buffer = new byte[PALETTE_SIZE * 2];
            var palette = new Color[PALETTE_SIZE];
            var imageKeys = new[] { "base", "eyes-droop", "eyes-blink", "unknown", "mouth-e", "mouth-a" };

            for (var ix = 0; source.Position < source.Length; ix++)
            {
                source.ReadExactly(buffer, 4);
                var total = BitConverter.ToInt32(buffer, 0);

                var position = source.Position;
                var headers = new (string id, uint size, uint offset, bool animation)[total];
                for (var h = 0; h < total; h++)
                {
                    source.ReadExactly(buffer, 12);

                    var animation = BitConverter.ToUInt16(buffer, 0);
                    var id = BitConverter.ToUInt16(buffer, 2).ToString("x4");
                    var size = BitConverter.ToUInt32(buffer, 4);
                    var offset = BitConverter.ToUInt32(buffer, 8);

                    headers[h] = (id, size, offset, animation > 0);
                }

                int Intensity(int c) =>
                    ((c & 0b00010000) >> 4) * 80 +
                    ((c & 0b00001000) >> 3) * 40 +
                    ((c & 0b00000100) >> 2) * 20 +
                    ((c & 0b00000010) >> 1) * 10 +
                    ((c & 0b00000001) >> 0) * 8 +
                    16;

                for (var h = 0; h < total; h++)
                {
                    var header = headers[h];

                    void ReadPalette(uint paletteOffset)
                    {
                        source.Seek(position + header.offset + paletteOffset, SeekOrigin.Begin);
                        source.ReadExactly(buffer, palette.Length * 2);
                        for (var i = 0; i < palette.Length; i++)
                        {
                            var color = BitConverter.ToUInt16(buffer, i * 2);
                            var r = Intensity((color & 0b0000000000011111) >> 0);
                            var g = Intensity((color & 0b0000001111100000) >> 5);
                            var b = Intensity((color & 0b1111110000000000) >> 10);
                            palette[i] = Color.FromArgb(r, g, b);
                        }
                    }

                    (int x, int y, Bitmap image) GetBitmap(uint offset)
                    {
                        source.Seek(position + header.offset + offset, SeekOrigin.Begin);
                        source.ReadExactly(buffer, 4);
                        var u = (sbyte)buffer[0];
                        var v = (sbyte)buffer[1];
                        var w = (sbyte)buffer[2];
                        var h = (sbyte)buffer[3];

                        if (buffer.Length < w)
                        {
                            Array.Resize(ref buffer, w);
                        }

                        var bmp = new Bitmap(w, h, PixelFormat.Format8bppIndexed);

                        var p = bmp.Palette;
                        for (var i = 0; i < palette.Length; i++)
                        {
                            p.Entries[i] = palette[i];
                        }
                        bmp.Palette = p;

                        var bmpData = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                        try
                        {
                            var scan = bmpData.Scan0;
                            for (var y = 0; y < h; y++, scan += bmpData.Stride)
                            {
                                source.ReadExactly(buffer, w);
                                Marshal.Copy(buffer, 0, scan, w);
                            }
                        }
                        finally
                        {
                            bmp.UnlockBits(bmpData);
                        }

                        return (u, v, bmp);
                    }

                    var builder = ImmutableDictionary.CreateBuilder<string, (int u, int v, Bitmap image)>();

                    source.Seek(position + header.offset, SeekOrigin.Begin);
                    source.ReadExactly(buffer, 4);

                    if (!header.animation)
                    {
                        var paletteOffset = BitConverter.ToUInt32(buffer, 0);

                        source.ReadExactly(buffer, 28);
                        var images = new uint[imageKeys.Length];
                        for (var i = 0; i < images.Length; i++)
                        {
                            if (imageKeys[i] != null)
                            {
                                images[i] = BitConverter.ToUInt32(buffer, i * 4);
                            }
                        }

                        ReadPalette(paletteOffset);

                        for (var i = 0; i < images.Length; i++)
                        {
                            if (images[i] != 0)
                            {
                                builder.Add(imageKeys[i]!, GetBitmap(images[i]));
                            }
                        }
                    }
                    else
                    {
                        var frames = BitConverter.ToUInt32(buffer, 0);

                        var frameHeaders = new (uint paletteOffset, uint frameOffset, uint unknown)[frames];
                        for (var f = 0; f < frames; f++)
                        {
                            source.ReadExactly(buffer, 12);

                            var paletteOffset = BitConverter.ToUInt32(buffer, 0);
                            var frameOffset = BitConverter.ToUInt32(buffer, 4);
                            var unknown = BitConverter.ToUInt32(buffer, 8);

                            frameHeaders[f] = (paletteOffset, frameOffset, unknown);
                        }

                        for (var f = 0; f < frames; f++)
                        {
                            var frame = frameHeaders[f];

                            if (frame.paletteOffset == 0 || frame.frameOffset == 0)
                            {
                                continue;
                            }

                            ReadPalette(frame.paletteOffset);

                            builder.Add($"frame{f}", GetBitmap(frame.frameOffset));
                        }
                    }

                    yield return (header.id, builder.ToImmutable());
                }

                position = position + headers.Max(h => h.offset + h.size);
                if (position % 2048 != 0)
                {
                    position += 2048 - position % 2048;
                }

                source.Position = position;
            }
        }
    }
}
