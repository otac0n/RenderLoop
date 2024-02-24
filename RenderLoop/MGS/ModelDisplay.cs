namespace RenderLoop.MGS
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Numerics;
    using System.Windows.Forms;
    using Microsoft.Extensions.DependencyInjection;
    using RenderLoop.Input;
    using RenderLoop.SoftwareRenderer;

    public class ModelDisplay : Display
    {
        private static readonly double ModelDisplaySeconds = 10.0;
        private readonly Camera Camera = new();
        private int frame;
        private int frames = 90;
        private double nextModel = ModelDisplaySeconds;
        private int activeModel;
        private bool flying;
        private Vector3 center;
        private float size;
        private readonly ControlChangeTracker controlChangeTracker;
        private readonly StageDirVirtualFileSystem stageDir;

        private readonly IList<(string[] path, Model model)> models;
        private readonly Dictionary<string, (ushort id, Bitmap? texture)> textures = [];
        private readonly Dictionary<ushort, Bitmap?> textureLookup = [];

        public ModelDisplay(IServiceProvider serviceProvider)
        {
            this.controlChangeTracker = serviceProvider.GetRequiredService<ControlChangeTracker>();

            var options = serviceProvider.GetRequiredService<Program.Options>();
            this.stageDir = serviceProvider.GetRequiredKeyedService<StageDirVirtualFileSystem>((options.File, WellKnownPaths.CD1Path, WellKnownPaths.StageDirPath));
            this.models = Model.UnpackModels(this.stageDir).Select(m => (new[] { options.File, WellKnownPaths.CD1Path, WellKnownPaths.StageDirPath, m.file }, m.model)).ToList();
            this.activeModel = Random.Shared.Next(this.models.Count);

            this.Camera.Up = new Vector3(0, 1, 0);

            this.KeyPreview = true;
            this.ClientSize = new(640, 480);
            this.UpdateModel();
        }

        private void UpdateModel()
        {
            this.nextModel = ModelDisplaySeconds;
            this.activeModel = (this.activeModel + this.models.Count) % this.models.Count;
            this.flying = false;

            var (path, model) = this.models[this.activeModel];

            var file = path[^1];
            var folder = file[..(file.IndexOf('/') + 1)] + $"texture";
            foreach (var tx in this.stageDir.Directory.EnumerateFiles(folder, "*.pcx"))
            {
                var (id, texture) = this.EnsureTexture(tx);
                this.textureLookup[id] = texture;
            }

            var min = new Vector3(float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity);

            foreach (var mesh in model.Meshes)
            {
                foreach (var v in mesh.Vertices)
                {
                    min = Vector3.Min(min, v);
                    max = Vector3.Max(max, v);
                }
            }

            var size = max - min;
            this.center = min + size / 2;
            this.size = Math.Max(size.X, Math.Max(size.Y, size.Z));
            this.Camera.FarPlane = 2 * this.size;
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
                    texture.texture = Model.ReadMgsPcx(textureFile);
                }

                this.textures[file] = texture;
            }

            return texture;
        }

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            var updated = false;
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    this.flying = false;
                    break;

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
                this.activeModel++;
                this.UpdateModel();
            }

            var moveVector = Vector3.Zero;

            var bindings = new Bindings<Action<double>>();
            bindings.BindCurrent(
                c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Name == "X",
                v => moveVector += this.Camera.Right * (float)((v - 0.5) * 2));
            bindings.BindCurrent(
                c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Name == "Y",
                v => moveVector += this.Camera.Direction * -(float)((v - 0.5) * 2));
            bindings.BindCurrent(
                c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Name == "Ry",
                v => moveVector += this.Camera.Up * -(float)((v - 0.5) * 2));

            this.controlChangeTracker.ProcessChanges(bindings);

            if (moveVector != Vector3.Zero)
            {
                this.flying = true;

                if (moveVector.LengthSquared() > 1)
                {
                    moveVector = Vector3.Normalize(moveVector);
                }

                moveVector *= this.size / 100;
                this.Camera.Position += moveVector;
            }

            if (!this.flying)
            {
                var a = Math.Tau * this.frame / this.frames;
                var (x, z) = Math.SinCos(a);
                var t = Math.Sin(a / 3);
                var p = new Vector3((float)(this.size * x), (float)(this.size / 10 * t), (float)(this.size * z));
                this.Camera.Position = this.center + p;
                this.Camera.Direction = -p;
            }
        }

        protected override void DrawScene(Graphics g, Bitmap buffer, float[,] depthBuffer)
        {
            this.Camera.Width = buffer.Width;
            this.Camera.Height = buffer.Height;

            var (_, model) = this.models[this.activeModel];
            foreach (var mesh in model.Meshes)
            {
                var transformed = Array.ConvertAll(mesh.Vertices, this.Camera.TransformToScreenSpace);
                foreach (var face in mesh.Faces)
                {
                    this.textureLookup.TryGetValue(face.TextureId, out var texture);

                    var indices = face.VertexIndices.Select((i, j) => (index: i, textureCoords: mesh.TextureCoords[face.TextureIndices[j]])).ToArray();
                    DrawStrip(indices, s => transformed[s.index], (v, vertices) =>
                        FillTriangle(buffer, depthBuffer, vertices, BackfaceCulling.None, perspective =>
                        {
                            var uv = MapCoordinates(perspective, [
                                v[0].textureCoords,
                                v[1].textureCoords,
                                v[2].textureCoords,
                            ]);
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
                                return Color.Black.ToArgb();
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
