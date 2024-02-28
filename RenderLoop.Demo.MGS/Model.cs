namespace RenderLoop.Demo.MGS
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using ImageMagick;
    using RenderLoop.Demo.MGS.Archives;
    using Point = (int x, int y, int z);

    public class Model
    {
        public Model(Mesh[] meshes)
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

        public static Bitmap? ReadMgsPcx(Stream stream)
        {
            try
            {
                using var pcxStream = new OffsetStreamSpan(stream, 8, stream.Length - 8);
                return new MagickImage(pcxStream).ToBitmap();
            }
            catch
            {
                return null;
            }
        }

        public static Model FromStream(Stream stream)
        {
            var buffer = new byte[128];
            stream.ReadExactly(buffer, 32);

            var totalFaces = BitConverter.ToUInt32(buffer, 0);
            var meshCount = BitConverter.ToUInt32(buffer, 4);
            ////var totalBounds = (
            ////    start: (
            ////        x: BitConverter.ToInt32(buffer, 8),
            ////        y: BitConverter.ToInt32(buffer, 12),
            ////        z: BitConverter.ToInt32(buffer, 16)),
            ////    end: (
            ////        x: BitConverter.ToInt32(buffer, 20),
            ////        y: BitConverter.ToInt32(buffer, 24),
            ////        z: BitConverter.ToInt32(buffer, 28)));

            var meshes = new List<Mesh>((int)meshCount);
            var offsets = new List<Point>();
            for (var m = 0; m < meshCount; m++)
            {
                stream.ReadExactly(buffer, 88);

                var flags = BitConverter.ToUInt32(buffer, 0);
                var faceCount = BitConverter.ToUInt32(buffer, 4);
                ////var bounds = (
                ////    start: (
                ////        x: BitConverter.ToInt32(buffer, 8),
                ////        y: BitConverter.ToInt32(buffer, 12),
                ////        z: BitConverter.ToInt32(buffer, 16)),
                ////    end: (
                ////        x: BitConverter.ToInt32(buffer, 20),
                ////        y: BitConverter.ToInt32(buffer, 24),
                ////        z: BitConverter.ToInt32(buffer, 28)));
                var relativePoint = new Vector3(
                    x: BitConverter.ToInt32(buffer, 32),
                    y: BitConverter.ToInt32(buffer, 36),
                    z: BitConverter.ToInt32(buffer, 40));
                var baseCoord = BitConverter.ToInt32(buffer, 44);
                var vertexCount = BitConverter.ToUInt32(buffer, 52);
                var vertexAddress = BitConverter.ToUInt32(buffer, 56);
                var vertexIndexAddress = BitConverter.ToUInt32(buffer, 60);
                var normalCount = BitConverter.ToUInt32(buffer, 64);
                var normalAddress = BitConverter.ToUInt32(buffer, 68);
                var normalIndexAddress = BitConverter.ToUInt32(buffer, 72);
                var texCoordAddress = BitConverter.ToUInt32(buffer, 76);
                var textureAddress = BitConverter.ToUInt32(buffer, 80);

                var relativeMesh = baseCoord != -1 && !(baseCoord == 0 && m == 0) ? meshes[baseCoord] : null;
                var baseOffset = stream.Position;

                var vertices = new Vector3[vertexCount];
                stream.Seek(vertexAddress, SeekOrigin.Begin);
                for (var v = 0; v < vertexCount; v++)
                {
                    stream.ReadExactly(buffer, 8);
                    // Not using W.
                    vertices[v] = new Vector3(
                        BitConverter.ToInt16(buffer, 0),
                        BitConverter.ToInt16(buffer, 2),
                        BitConverter.ToInt16(buffer, 4));
                }

                var normals = new Vector3[normalCount];
                stream.Seek(normalAddress, SeekOrigin.Begin);
                for (var n = 0; n < normalCount; n++)
                {
                    stream.ReadExactly(buffer, 8);
                    normals[n] = new Vector3(
                        BitConverter.ToInt16(buffer, 0) / -(float)short.MinValue,
                        BitConverter.ToInt16(buffer, 2) / -(float)short.MinValue,
                        BitConverter.ToInt16(buffer, 4) / -(float)short.MinValue);
                }

                var texCoords = new Vector2[(textureAddress - texCoordAddress) / 2];
                stream.Seek(texCoordAddress, SeekOrigin.Begin);
                for (var t = 0; t < texCoords.Length; t++)
                {
                    stream.ReadExactly(buffer, 2);
                    texCoords[t] = new Vector2(
                        buffer[0] / 256.0f,
                        buffer[1] / 256.0f);
                }

                var faces = new Face[faceCount];

                stream.Seek(vertexIndexAddress, SeekOrigin.Begin);
                for (var v = 0; v < faceCount; v++)
                {
                    var indices = new byte[4];
                    stream.ReadExactly(indices, 4);
                    (indices[0], indices[1]) = (indices[1], indices[0]);
                    faces[v].VertexIndices = Array.ConvertAll(indices, i => (uint)i);
                }

                stream.Seek(normalIndexAddress, SeekOrigin.Begin);
                for (var v = 0; v < faceCount; v++)
                {
                    var indices = new byte[4];
                    stream.ReadExactly(indices, 4);
                    (indices[0], indices[1]) = (indices[1], indices[0]);
                    faces[v].NormalIndices = Array.ConvertAll(indices, i => (uint)i);
                }

                stream.Seek(textureAddress, SeekOrigin.Begin);
                for (var v = 0; v < faceCount; v++)
                {
                    stream.ReadExactly(buffer, 2);
                    faces[v].TextureId = BitConverter.ToUInt16(buffer, 0);
                }

                for (var v = 0u; v < faceCount; v++)
                {
                    uint[] indices = [
                        4 * v + 0,
                        4 * v + 1,
                        4 * v + 2,
                        4 * v + 3
                    ];
                    (indices[0], indices[1]) = (indices[1], indices[0]);
                    faces[v].TextureIndices = indices;
                }

                meshes.Add(
                    new Mesh(
                        relativePoint,
                        vertices,
                        texCoords,
                        normals,
                        faces,
                        relativeMesh));

                stream.Seek(baseOffset, SeekOrigin.Begin);
            }

            return new Model([.. meshes]);
        }
    }
}
