// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS
{
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using AnimatedGif;
    using System.Windows.Forms;
    using System.Collections.Immutable;
    using System.Drawing.Imaging;
    using System.Runtime.InteropServices;
    using System;
    using DiscUtils.Streams;
    using Microsoft.Extensions.DependencyInjection;

    internal class CodecDisplay : Form
    {
        public CodecDisplay(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<Program.Options>();
            var facesStream = serviceProvider.GetRequiredKeyedService<SparseStream>((options.File, WellKnownPaths.CD1Path, WellKnownPaths.FaceDatPath));
            var source = UnpackFaces(facesStream);

            var parent = new FlowLayoutPanel()
            {
                BackColor = Color.Black,
                ForeColor = Color.Green,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
            };

            foreach (var group in source.GroupBy(i => i.id, i => i.images))
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

                foreach (var set in group.Select((s, i) => (images: s, index: i)))
                {
                    var gifStream = new MemoryStream();
                    using (var gif = new AnimatedGifCreator(gifStream))
                    {
                        var animation = set.images;

                        var maxX = animation.Values.Max(v => v.x + v.image.Width);
                        var maxY = animation.Values.Max(v => v.y + v.image.Height);
                        var surface = new Bitmap(maxX, maxY);
                        using var g = Graphics.FromImage(surface);

                        if (animation.TryGetValue("base", out var baseImage))
                        {
                            var frames = new List<string>()
                            {
                                "mouth1",
                                "eyes1",
                                "eyes2",
                                "mouth2",
                            };
                            frames.RemoveAll(f => !animation.ContainsKey(f));

                            g.DrawImageUnscaled(baseImage.image, new Point(baseImage.x, baseImage.y));
                            gif.AddFrame(surface);

                            foreach (var frame in frames)
                            {
                                var frameImage = animation[frame];
                                g.DrawImageUnscaled(frameImage.image, new Point(frameImage.x, frameImage.y));
                                gif.AddFrame(surface);
                                g.DrawImageUnscaled(baseImage.image, new Point(baseImage.x, baseImage.y));
                                gif.AddFrame(surface);
                            }
                        }
                        else
                        {
                            var shown = 0;
                            for (var f = 0; shown < animation.Count; f++)
                            {
                                if (animation.TryGetValue($"frame{f}", out var frameImage))
                                {
                                    g.DrawImageUnscaled(frameImage.image, new Point(frameImage.x, frameImage.y));
                                    gif.AddFrame(surface);
                                    shown++;
                                }
                            }
                        }
                    }

                    gifStream.Seek(0, SeekOrigin.Begin);
                    var outputImage = Image.FromStream(gifStream);

                    panel.Controls.Add(new PictureBox
                    {
                        Image = outputImage,
                        Size = outputImage.Size * 2,
                        SizeMode = PictureBoxSizeMode.Zoom,
                    });
                }

                parent.Controls.Add(panel);
            }

            this.Controls.Add(parent);
        }

        public static IEnumerable<(string id, ImmutableDictionary<string, (int x, int y, Bitmap image)> images)> UnpackFaces(Stream source)
        {
            const int PALETTE_SIZE = 256;

            var buffer = new byte[PALETTE_SIZE * 2];
            var palette = new Color[PALETTE_SIZE];
            var imageKeys = new[] { "base", "eyes1", "eyes2", null, "mouth1", "mouth2" };

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
