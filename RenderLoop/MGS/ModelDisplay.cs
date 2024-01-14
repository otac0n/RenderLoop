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
    using RenderLoop.SoftwareRenderer;

    public class ModelDisplay : Display
    {
        private static readonly double ModelDisplaySeconds = 10.0;
        private int frame;
        private int frames = 90;
        private double nextModel = ModelDisplaySeconds;
        private int activeModel;
        private Vector3 center;
        private float size;

        private readonly StageDirVirtualFileSystem stageDir;

        private readonly IList<(string[] path, Model model)> models;
        private readonly Dictionary<string, (ushort id, Bitmap? texture)> textures = [];
        private readonly Dictionary<ushort, Bitmap?> textureLookup = [];

        public ModelDisplay(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<Program.Options>();
            this.stageDir = serviceProvider.GetRequiredKeyedService<StageDirVirtualFileSystem>((options.File, WellKnownPaths.CD1Path, WellKnownPaths.StageDirPath));
            this.models = Model.UnpackModels(this.stageDir).Select(m => (new[] { options.File, WellKnownPaths.CD1Path, WellKnownPaths.StageDirPath, m.file }, m.model)).ToList();

            this.Camera.Up = new Vector3(0, 1, 0);

            this.KeyPreview = true;
            this.BackColor = Color.Gray;
            this.UpdateModel();
        }

        private void UpdateModel()
        {
            var (path, model) = this.models[this.activeModel];

            var file = path[^1];
            var folder = file[..(file.IndexOf('/') + 1)] + $"texture";
            foreach (var tx in this.stageDir.Directory.EnumerateFiles(folder, "*.pcx"))
            {
                var (id, texture) = this.EnsureTexture(tx);
                this.textureLookup[id] = texture;
            }

            var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            foreach (var mesh in model.Meshes)
            {
                foreach (var v in mesh.Vertices)
                {
                    var p = new Vector3(v.x, v.y, v.z);
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                }
            }

            var size = max - min;
            this.center = min + size / 2;
            this.size = Math.Max(size.X, Math.Max(size.Y, size.Z));
        }

        private (ushort id, Bitmap? texture) EnsureTexture(string file)
        {
            if (!this.textures.TryGetValue(file, out var texture))
            {
                if (this.stageDir.File.Exists(file))
                {
                    var buffer = new byte[2];
                    using var textureFile = this.stageDir.File.OpenRead(file);
                    textureFile.ReadExactly(buffer, 2);
                    texture.id = BitConverter.ToUInt16(buffer, 0);
                    texture.texture = ReadMgsPcx(textureFile);
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
                this.nextModel = ModelDisplaySeconds;
                this.activeModel = (this.activeModel + this.models.Count) % this.models.Count;
                this.UpdateModel();
            }

            base.OnPreviewKeyDown(e);
        }

        protected override void AdvanceFrame(TimeSpan elapsed)
        {
            this.frame++;

            this.nextModel -= elapsed.TotalSeconds;
            if (this.nextModel <= 0 && false)
            {
                this.nextModel = ModelDisplaySeconds;
                this.activeModel = (this.activeModel + 1) % this.models.Count;
                this.UpdateModel();
            }

            var a = Math.Tau * this.frame / this.frames;
            var (x, z) = Math.SinCos(a);
            var t = Math.Sin(a / 3);
            var p = new Vector3((float)(this.size * x), (float)(this.size / 10 * t), (float)(this.size * z));
            this.Camera.Position = this.center + p;
            this.Camera.Direction = -p;

            base.AdvanceFrame(elapsed);
        }

        protected override void DrawScene(Graphics g, Bitmap buffer, float[,] depthBuffer)
        {
            var width = buffer.Width;
            var height = buffer.Height;

            var (_, model) = this.models[this.activeModel];
            foreach (var mesh in model.Meshes)
            {
                var transformed = Array.ConvertAll(mesh.Vertices, v => this.Camera.Transform(new Vector3(v.x, v.y, v.z)));
                foreach (var face in mesh.Faces)
                {
                    this.textureLookup.TryGetValue(face.TextureId, out var texture);

                    var indices = face.VertexIndices.Select((i, j) => (index: i, textureCoords: mesh.TextureCoords[face.TextureIndices[j]])).ToArray();
                    DrawShape(indices, s => transformed[s.index], (v, vertices) =>
                        FillTriangle(buffer, depthBuffer, vertices, BackfaceCulling.None, (barycenter, z) =>
                        {
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
                                // MGS textures use the last pixel as buffer
                                var tw = texture.Width - 1;
                                var th = texture.Height - 1;
                                var color = texture.GetPixel(
                                    (int)(((uv.X % 1.0) + 1) % 1.0 * tw),
                                    (int)(((uv.Y % 1.0) + 1) % 1.0 * th)).ToArgb();
                                // MGS treats pure black as transparent.
                                var masked = color & 0xFFFFFF;
                                return masked == 0x000000 ? masked : color;
                            }
                            else
                            {
                                return Color.FromArgb(
                                    (int)(uv.X % 1.0 * 255),
                                    (int)(uv.Y % 1.0 * 255),
                                    0).ToArgb();
                            }
                        }));
                }
            }

            using var textBrush = new SolidBrush(this.ForeColor);
            var paths = string.Join(Environment.NewLine, this.models[this.activeModel].path.Select((p, i) => (i > 0 ? new string(' ', i * 2) + "└" : "") + p));
            g.DrawString(paths, this.Font, textBrush, PointF.Empty);
        }
    }
}
