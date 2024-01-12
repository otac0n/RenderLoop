namespace RenderLoop.MGS
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Windows.Forms;
    using ImageMagick;
    using Microsoft.Extensions.DependencyInjection;
    using RenderLoop.Archives;
    using RenderLoop.SofwareRenderer;

    public class ModelDisplay : Display
    {
        private int frame = 0;
        private int frames = 90;
        private Vector3[] points;
        private (int index, Vector2 textureCoords)[][] shapes;
        private double nextModel = 10.0;
        private int activeModel = 0;
        private Vector3 center;
        private float size;

        private readonly StageDirVirtualFileSystem stageDir;

        private readonly IList<(string[] path, Model model)> models;
        private readonly Dictionary<string, Bitmap?> textures = new();
        private readonly List<Bitmap> texturesUsed = new();

        public ModelDisplay(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<Program.Options>();
            this.stageDir = serviceProvider.GetRequiredKeyedService<StageDirVirtualFileSystem>((options.File, WellKnownPaths.CD1Path, WellKnownPaths.StageDirPath));
            var models = Model.UnpackModels(this.stageDir).Select(m => (new[] { options.File, WellKnownPaths.CD1Path, WellKnownPaths.StageDirPath, m.file }, m.model)).ToList();

            this.KeyPreview = true;
            this.models = models;
            this.BackColor = Color.Gray;
            this.ReadMesh();
        }

        private static readonly Color[] Palette = [
            Color.Red,
            Color.ForestGreen,
            Color.CornflowerBlue,
            Color.Goldenrod,
            Color.PaleTurquoise,
            Color.HotPink,
        ];

        private void ReadMesh()
        {
            var (path, model) = this.models[this.activeModel];
            this.texturesUsed.Clear();

            var file = path[path.Length - 1];
            var folder = file[..(file.IndexOf('/') + 1)] + $"texture";
            foreach (var tx in this.stageDir.Directory.EnumerateFiles(folder, "*.pcx"))
            {
                var bmp = this.EnsureTexture(tx);
                if (bmp != null)
                {
                    this.texturesUsed.Add(bmp);
                    break;
                }
            }

            var points = new List<Vector3>();
            var shapes = new List<(int indices, Vector2 textureCoords)[]>();

            var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            var fakeUV = new[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1),
            };

            foreach (var mesh in model.Meshes)
            {
                var start = points.Count;
                for (var i = 0; i < mesh.Vertices.Length; i++)
                {
                    var v = mesh.Vertices[i];
                    var p = new Vector3(v.x, v.z, v.y);
                    points.Add(p);
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                }

                foreach (var shape in mesh.Faces)
                {
                    var indices = shape.VertexIndices.Select((i, j) => (i + start, fakeUV[j])).ToArray();
                    (indices[2], indices[3]) = (indices[3], indices[2]);
                    Color color;
                    //if (mesh.Normals.Length > shape.NormalIndices[0])
                    //{
                    //    var normal = mesh.Normals[shape.NormalIndices[0]];
                    //    normal = normal * (1 / normal.Length());
                    //    color = Color.FromArgb(
                    //        (int)Math.Clamp(normal.X * 128 + 128, 0, 255),
                    //        (int)Math.Clamp(normal.Y * 128 + 128, 0, 255),
                    //        (int)Math.Clamp(normal.Z * 128 + 128, 0, 255));
                    //}
                    //else
                    {
                        color = Palette[shape.TextureId % Palette.Length];

                        if (shape.TextureId != 0)
                        {
                            //var texture = file[..(file.IndexOf('/') + 1)] + $"texture/{shape.TextureId:x4}.pcx";
                        }
                    }
                    shapes.Add(indices);
                }
            }

            var size = max - min;
            this.center = min + size / 2;
            this.size = Math.Max(size.X, Math.Max(size.Y, size.Z));
            this.points = points.ToArray();
            this.shapes = shapes.ToArray();
        }

        private Bitmap? EnsureTexture(string file)
        {
            if (!this.textures.TryGetValue(file, out var texture))
            {
                if (this.stageDir.File.Exists(file))
                {
                    using var textureFile = this.stageDir.File.OpenRead(file);
                    texture = ReadMgsPcx(textureFile);
                }

                this.textures[file] = texture;
            }

            return texture;
        }

        private static Bitmap? ReadMgsPcx(Stream stream)
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

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            var updated = false;
            switch (e.KeyCode)
            {
                case Keys.Left:
                    this.activeModel--;
                    updated = true;
                    break;

                case Keys.Right:
                    this.activeModel++;
                    updated = true;
                    break;
            }

            if (updated)
            {
                this.activeModel = (this.activeModel + this.models.Count) % this.models.Count;
                this.ReadMesh();
            }

            base.OnPreviewKeyDown(e);
        }

        protected override void AdvanceFrame(TimeSpan elapsed)
        {
            this.frame++;

            this.nextModel -= elapsed.TotalSeconds;
            if (this.nextModel <= 0 && false)
            {
                this.nextModel = 10.0;
                this.activeModel = (this.activeModel + 1) % this.models.Count;
                this.ReadMesh();
            }

            var a = (Math.Tau * this.frame) / this.frames;
            var (x, y) = Math.SinCos(a);
            var p = new Vector3((float)(this.size * x), (float)(this.size * y), 0);
            this.Camera.Position = this.center + p;
            this.Camera.Direction = -p;

            base.AdvanceFrame(elapsed);
        }

        protected override void DrawScene(Graphics g, Bitmap buffer, float[,] depthBuffer)
        {
            var width = buffer.Width;
            var height = buffer.Height;

            var texture = this.texturesUsed.FirstOrDefault();

            var transformed = Array.ConvertAll(this.points, p =>
            {
                p = this.Camera.Transform(p);
                p.X = (p.X + 1) * 0.5f * width;
                p.Y = (1 - p.Y) * 0.5f * height;
                return p;
            });

            foreach (var shape in this.shapes)
            {
                DrawShape(shape, s => transformed[s.index], (v, vertices) =>
                {
                    FillTriangle(buffer, depthBuffer, vertices, (barycenter, z) =>
                    {
                        if (barycenter == Vector3.Zero)
                        {
                            return Color.Magenta;
                        }

                        var uv = MapCoordinates(
                            barycenter,
                            [
                                new Vector3(v[0].textureCoords.X / z[0], v[0].textureCoords.Y / z[0], 1 / z[0]),
                                new Vector3(v[1].textureCoords.X / z[1], v[1].textureCoords.Y / z[1], 1 / z[1]),
                                new Vector3(v[2].textureCoords.X / z[2], v[2].textureCoords.Y / z[2], 1 / z[2]),
                            ]);
                        uv *= z[3];
                        if (texture != null)
                        {
                            return texture.GetPixel(
                                (int)(uv.X % 1.0 * texture.Width),
                                (int)(uv.Y % 1.0 * texture.Height));
                        }
                        else
                        {
                            return Color.FromArgb(
                                (int)(uv.X % 1.0 * 255),
                                (int)(uv.Y % 1.0 * 255),
                                0);
                        }
                    });
                    DrawWireFrame(g, vertices);
                });
            }

            using var textBrush = new SolidBrush(this.ForeColor);
            var paths = string.Join(Environment.NewLine, this.models[this.activeModel].path.Select((p, i) => (i > 0 ? new string(' ', i * 2) + "└" : "") + p));
            g.DrawString(paths, this.Font, textBrush, PointF.Empty);
        }
    }
}
