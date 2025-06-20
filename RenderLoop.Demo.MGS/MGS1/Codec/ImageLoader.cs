// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS1.Codec
{
    using System;
    using System.Collections.Immutable;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using ImageSet = System.Collections.Immutable.ImmutableDictionary<string, (int X, int Y, System.Drawing.Bitmap Image)>;

    internal static class ImageLoader
    {
        public static ImmutableDictionary<string, ImageSet> LoadImages(Stream source)
        {
            var result = ImmutableDictionary.CreateBuilder<string, ImageSet>();

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

                    (int X, int Y, Bitmap Image) GetBitmap(uint offset)
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

                    var builder = ImmutableDictionary.CreateBuilder<string, (int X, int Y, Bitmap Image)>();

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

                    if (result.ContainsKey(header.id))
                    {
                        // TODO: Log warning.
                        continue;
                    }

                    result.Add(header.id, builder.ToImmutable());
                }

                position += headers.Max(h => h.offset + h.size);
                if (position % 2048 != 0)
                {
                    position += 2048 - position % 2048;
                }

                source.Position = position;
            }

            return result.ToImmutable();
        }
    }
}
