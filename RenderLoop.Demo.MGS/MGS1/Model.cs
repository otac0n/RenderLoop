// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS1
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using ImageMagick;
    using Bounds = (System.Numerics.Vector3 Start, System.Numerics.Vector3 End);

    public class Model
    {
        public Model(Bounds bounds, Mesh[] meshes)
        {
            this.Meshes = meshes;
        }

        public Mesh[] Meshes { get; }

        public static IEnumerable<(string file, Model model)> UnpackModels(StageDirVirtualFileSystem stage)
        {
            foreach (var file in stage.Directory.EnumerateFiles("", "*.kmd", SearchOption.AllDirectories).Where(file => file.Contains("/model/")))
            {
                using var stream = stage.File.OpenRead(file);
                yield return (file, FromStream(stream));
            }
        }

        public static Bitmap? ReadMgsPcx(Stream pcxStream)
        {
            try
            {
                using var image = new MagickImage(pcxStream);
                using var bitmap = image.ToBitmap();

                // MGS treats black as fully transparent.
                var result = new Bitmap(bitmap.Width, bitmap.Height);
                var bmp1 = bitmap.LockBits(new Rectangle(default, result.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                var bmp2 = result.LockBits(new Rectangle(default, result.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                var scanIn = bmp1.Scan0;
                var scanOut = bmp2.Scan0;
                var buffer = new byte[bmp2.Width * 4];
                for (var y = 0; y < bmp2.Height; y++, scanIn += bmp1.Stride, scanOut += bmp2.Stride)
                {
                    Marshal.Copy(scanIn, buffer, 0, buffer.Length);

                    for (var x = 0; x < bmp2.Width; x++)
                    {
                        var ix = x * 4;
                        if (buffer[ix] == 0 &&
                            buffer[ix + 1] == 0 &&
                            buffer[ix + 2] == 0)
                        {
                            buffer[ix + 3] = 0;
                        }
                    }

                    Marshal.Copy(buffer, 0, scanOut, buffer.Length);
                }

                bitmap.UnlockBits(bmp1);
                result.UnlockBits(bmp2);

                return result;
            }
            catch
            {
                return null;
            }
        }

        public static Model FromStream(Stream stream)
        {
            static Vector3 V(Point p) => new(p.X, p.Y, p.Z);

            var buffer = new byte[stream.Length];
            var fileSpan = buffer.AsSpan();
            stream.ReadExactly(fileSpan);
            var header = MemoryMarshal.Cast<byte, Header>(fileSpan)[0];
            var meshDefinitions = MemoryMarshal.Cast<byte, MeshDefinition>(fileSpan[32..])[0..(int)header.MeshCount];

            var meshes = new List<Mesh>((int)header.MeshCount);
            for (var m = 0; m < header.MeshCount; m++)
            {
                var relativeMesh = meshDefinitions[m].RelativeIndex != -1 && !(meshDefinitions[m].RelativeIndex == 0 && m == 0) ? meshes[meshDefinitions[m].RelativeIndex] : null;

                var vertexCount = meshDefinitions[m].VertexCount;
                var vertices = new Vector3[vertexCount];
                var vertexData = MemoryMarshal.Cast<byte, (short X, short Y, short Z, short W)>(fileSpan[(int)meshDefinitions[m].VertexOffset..])[0..(int)vertexCount];
                for (var v = 0; v < vertexCount; v++)
                {
                    // Not using W.
                    vertices[v] = new(
                        vertexData[v].X,
                        vertexData[v].Y,
                        vertexData[v].Z);
                }

                var normalCount = meshDefinitions[m].NormalCount;
                var normalData = MemoryMarshal.Cast<byte, (short X, short Y, short Z, short W)>(fileSpan[(int)meshDefinitions[m].NormalOffset..])[0..(int)normalCount];
                var normals = new Vector3[normalCount];
                for (var n = 0; n < normalCount; n++)
                {
                    normals[n] = new Vector3(
                        normalData[n].X / 4096f,
                        normalData[n].Y / 4096f,
                        normalData[n].Z / 4096f);
                }

                var faceCount = meshDefinitions[m].FaceCount;
                var faces = new Face[faceCount];
                var vertexIndexData = MemoryMarshal.Cast<byte, (byte A, byte B, byte C, byte D)>(fileSpan[(int)meshDefinitions[m].VertexIndexOffset..])[0..(int)faceCount];
                var normalIndexData = MemoryMarshal.Cast<byte, (byte A, byte B, byte C, byte D)>(fileSpan[(int)meshDefinitions[m].NormalIndexOffset..])[0..(int)faceCount];
                var texCoordCount = (meshDefinitions[m].TextureOffset - meshDefinitions[m].TextureCoordOffset) / 2;
                var textureCoordData = MemoryMarshal.Cast<byte, (byte U, byte V)>(fileSpan[(int)meshDefinitions[m].TextureCoordOffset..])[0..(int)texCoordCount];
                var textureData = MemoryMarshal.Cast<byte, ushort>(fileSpan[(int)meshDefinitions[m].TextureOffset..])[0..(int)faceCount];

                for (var v = 0; v < faceCount; v++)
                {
                    var (a, b, c, d) = vertexIndexData[v];
                    faces[v].VertexIndices = [b, a, c, d];
                }

                for (var v = 0; v < faceCount; v++)
                {
                    var (a, b, c, d) = normalIndexData[v];
                    faces[v].NormalIndices = [b, a, c, d];
                }

                var texCoords = new Vector2[texCoordCount];
                for (var t = 0; t < texCoords.Length; t++)
                {
                    texCoords[t] = new Vector2(
                        textureCoordData[t].U / 255f,
                        textureCoordData[t].V / 255f);
                }

                for (var v = 0; v < faceCount; v++)
                {
                    faces[v].TextureId = textureData[v];
                }

                for (var v = 0u; v < faceCount; v++)
                {
                    faces[v].TextureIndices = [4 * v + 1, 4 * v + 0, 4 * v + 2, 4 * v + 3];
                }

                meshes.Add(
                    new Mesh(
                        (DrawingFlags)meshDefinitions[m].Flags,
                        (V(meshDefinitions[m].Min), V(meshDefinitions[m].Max)),
                        V(meshDefinitions[m].RelativeOrigin),
                        relativeMesh,
                        vertices,
                        normals,
                        texCoords,
                        faces));
            }

            return new Model((V(header.Min), V(header.Max)), [.. meshes]);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Point
        {
            public int X;
            public int Y;
            public int Z;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Header
        {
            public uint FaceCount;
            public uint MeshCount;
            public Point Min;
            public Point Max;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MeshDefinition
        {
            public uint Flags;
            public uint FaceCount;
            public Point Min;
            public Point Max;
            public Point RelativeOrigin;
            public int RelativeIndex;
            public uint UnkownA;
            public uint VertexCount;
            public uint VertexOffset;
            public uint VertexIndexOffset;
            public uint NormalCount;
            public uint NormalOffset;
            public uint NormalIndexOffset;
            public uint TextureCoordOffset;
            public uint TextureOffset;
            public uint UnkownB;
        }
    }
}
