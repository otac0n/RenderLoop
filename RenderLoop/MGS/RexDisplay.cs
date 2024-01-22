namespace RenderLoop.MGS
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Numerics;
    using System.Windows.Forms;
    using Microsoft.Extensions.DependencyInjection;
    using RenderLoop.SoftwareRenderer;

    public class RexDisplay : Display
    {
        private static readonly Dictionary<string, (string[] versions, (string attachTo, int atIndex)? attach, (int index, Vector3 min, Vector3 max)[] freedoms)>[] Sources =
        [
            new()
            {
                ["leg_a"] = (
                    ["s17a/model/835c.kmd"],
                    null,
                    [
                        (1, Angles(1 / 8f, 1 / 32f, 1 / 16f), Angles(1 / 4f, 1 / 32f, 1 / 16f)), // Legs, proper
                        (2, Angles(1 / 8f, 1 / 32f, 0), Angles(1 / 8f, 1 / 32f, 0)), // Leg armor
                        (3, Angles(0, 0, 0), Angles(1 / 32f, 0, 0)), // Shin
                        //(4, Angles(0, 0, 0), Angles(0, 0, 0)), // Foot
                        (5, Angles(1 / 16f, 1 / 64f, 1 / 32f), Angles(3 / 16f, 1 / 64f, 1 / 32f)), // Heel
                        (8, Angles(1 / 16f, 1 / 64f, 1 / 32f), Angles(1 / 32f, 1 / 64f, 1 / 32f)), // Toe
                        //(7, Angles(0, 0, 0), Angles(0, 0, 0)), // Arch
                    ]
                ),
                ["leg_b"] = (
                    ["s17a/model/841c.kmd"],
                    null,
                    [
                        (1, Angles(1 / 8f, -1 / 32f, -1 / 16f), Angles(1 / 4f, -1 / 32f, -1 / 16f)), // Legs, proper
                        (2, Angles(1 / 8f, -1 / 32f, -0), Angles(1 / 8f, -1 / 32f, -0)), // Leg armor
                        (3, Angles(0, -0, -0), Angles(1 / 32f, -0, -0)), // Shin
                        //(4, Angles(0, -0, -0), Angles(0, -0, -0)), // Foot
                        (5, Angles(1 / 16f, -1 / 64f, -1 / 32f), Angles(3 / 16f, -1 / 64f, -1 / 32f)), // Heel
                        (8, Angles(1 / 16f, -1 / 64f, -1 / 32f), Angles(1 / 32f, -1 / 64f, -1 / 32f)), // Toe
                        //(7, Angles(0, -0, -0), Angles(0, -0, -0)), // Arch
                    ]
                ),
                ["knee_a"] = (
                    [
                        "s17a/model/32f5.kmd",
                        "s17a/model/32f6.kmd",
                        "s17a/model/32f7.kmd",
                        "s17a/model/32f8.kmd",
                    ],
                    ("leg_a", 1),
                    []
                ),
                ["knee_b"] = (
                    [
                        "s17a/model/3325.kmd",
                        "s17a/model/3326.kmd",
                        "s17a/model/3327.kmd",
                        "s17a/model/3328.kmd",
                    ],
                    ("leg_b", 1),
                    []
                ),
                ["body"] = (
                    ["s17a/model/8422.kmd"],
                    null,
                    [
                        (1, Angles(1 / 128f, 1 / 32f, 1 / 16f), Angles(1 / 16f, 1 / 32f, 1 / 16f)), // Neck
                        (2, Angles(0, 1 / 32f, 1 / 16f), Angles(1 / 16f, 1 / 32f, 1 / 16f)), // Head
                        (3, Angles(0, 0, 0), Angles(1 / 16f, 0, 0)), // Jaw
                        (4, Angles(1 / 16f, 1 / 8f, 1 / 16f), Angles(1 / 16f, 1 / 8f, 1 / 16f)), // Back
                        (5, Angles(1 / 16f, 0, 1 / 8f), Angles(1 / 16f, 0, 1 / 8f)), // Radar Arm
                        (6, Angles(1 / 16f, 0, 1 / 8f), Angles(1 / 16f, 1 / 16f, 1 / 8f)), // Radar Boom
                        (7, Angles(1 / 16f, -0, -1 / 8f), Angles(1 / 16f, -0, -1 / 8f)), // Railgun Arm
                        (8, Angles(1 / 16f, -0, -1 / 8f), Angles(1 / 16f, -1 / 16f, -1 / 8f)), // Railgun
                        (9, Angles(1 / 16f, 1 / 8f, 1 / 16f), Angles(1 / 16f, 1 / 8f, 1 / 16f)), // Crotch Gun
                    ]
                ),
                ["palate"] = (
                    ["s17a/model/638a.kmd"],
                    ("body", 3),
                    []
                ),
                ["cockpit"] = (
                    [
                        "s17a/model/638b.kmd",
                        "s17a/model/cd95.kmd", // Liquid
                    ],
                    ("body", 3),
                    []
                ),
                ["radome"] = (
                    [
                        "s17a/model/9ae4.kmd",
                        "s17a/model/9ae5.kmd",
                        "s17a/model/9ae6.kmd", // + "s17a/model/9ae7.kmd" antennae?
                    ],
                    ("body", 6),
                    [
                        (0, Angles(1 / 32f, 1 / 32f, 1 / 32f), Angles(1 / 32f, 1 / 32f, 1 / 32f)),
                    ]
                ),
            },
        ];

        private double t;
        private ulong animate;
        private bool flying;
        private Vector3 center;
        private float size;
        private int activeModel;
        private readonly StageDirVirtualFileSystem stageDir;

        private readonly List<(Dictionary<string, (string[] versions, (string attachTo, int atIndex)? attach, (int index, Vector3 min, Vector3 max)[] freedoms)> source, Dictionary<string, Model[]> parts)> models;
        private readonly Dictionary<string, (ushort id, Bitmap? texture)> textures = [];
        private readonly Dictionary<ushort, Bitmap?> textureLookup = [];

        public RexDisplay(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<Program.Options>();
            this.stageDir = serviceProvider.GetRequiredKeyedService<StageDirVirtualFileSystem>((options.File, WellKnownPaths.CD1Path, WellKnownPaths.StageDirPath));

            this.models = new();
            foreach (var source in Sources)
            {
                var parts = new Dictionary<string, Model[]>();

                foreach (var (name, info) in source)
                {
                    var versions = new Model[info.versions.Length];

                    for (var f = 0; f < info.versions.Length; f++)
                    {
                        var file = info.versions[f];
                        using var stream = this.stageDir.File.OpenRead(file);
                        var model = Model.FromStream(stream);
                        versions[f] = model;

                        var folder = file[..(file.IndexOf('/') + 1)] + $"texture";
                        foreach (var tx in this.stageDir.Directory.EnumerateFiles(folder, "*.pcx"))
                        {
                            var (id, texture) = this.EnsureTexture(tx);
                            this.textureLookup[id] = texture;
                        }
                    }

                    parts.Add(name, versions);
                }

                foreach (var (name, info) in source)
                {
                    if (info.attach is (string attachTo, int atIndex) attach)
                    {
                        var mesh = parts[attach.attachTo].Single().Meshes[attach.atIndex];
                        var toAttach = parts[name];
                        foreach (var model in toAttach)
                        {
                            model.Meshes[0].RelativeMesh = mesh;
                        }
                    }
                }

                this.models.Add((source, parts));
            }

            this.UpdateModel();

            this.Camera.Up = new Vector3(0, 1, 0);

            this.KeyPreview = true;
            this.ClientSize = new(640, 480);
        }

        private static Vector3 Angles(double x, double y, double z) =>
            new((float)(x * Math.Tau), (float)(y * Math.Tau), (float)(z * Math.Tau));

        private static double Demonstrate(double t, double min, double max)
        {
            static double T(double t) => (t * 2 - 1) * Math.Tau / 2;
            static double A(double x) => 1 / (1 + Math.Exp(x));
            static double B(double x) => Math.Sin(x);
            static double AB(double x) => A(x) * B(x);
            static double V(double x) => AB(x) * Math.Abs(AB(-x));

            double M(double t) => t <= 0 ? min : max;
            double VM(double t) => V(t) * M(t);

            return VM(T(t)) / 0.156312;
        }

        private void UpdateModel()
        {
            this.activeModel = (this.activeModel + this.models.Count) % this.models.Count;
            this.flying = false;

            var min = new Vector3(float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity);

            var (_, parts) = this.models[this.activeModel];

            foreach (var (_, versions) in parts)
            {
                foreach (var model in versions)
                {
                    foreach (var mesh in model.Meshes)
                    {
                        foreach (var v in mesh.Vertices)
                        {
                            min = Vector3.Min(min, v);
                            max = Vector3.Max(max, v);
                        }
                    }
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
            var animateFrame = this.animate;
            var animateT = this.t - (int)this.t;

            var (source, parts) = this.models[this.activeModel];
            foreach (var (name, versions) in parts)
            {
                var freedoms = source[name].freedoms;
                if (freedoms.Length == 0)
                {
                    continue;
                }

                for (var m = 0; m < versions[0].Meshes.Length; m++)
                {
                    var mesh = versions[0].Meshes[m];

                    var freedomIndex = Array.FindIndex(freedoms, f => f.index == m);
                    if (freedomIndex >= 0)
                    {
                        var (_, min, max) = freedoms[freedomIndex];

                        mesh.Rotation = (animateFrame % 4) switch
                        {
                            1 => Matrix4x4.CreateRotationX((float)Demonstrate(animateT, min.X, max.X)),
                            2 => Matrix4x4.CreateRotationY((float)Demonstrate(animateT, min.Y, max.Y)),
                            3 => Matrix4x4.CreateRotationZ((float)Demonstrate(animateT, min.Z, max.Z)),
                            _ => Matrix4x4.Identity,
                        };

                        animateFrame /= 4;
                    }
                }
            }

            if (this[Keys.W] || this[Keys.S] || this[Keys.A] || this[Keys.D] || this[Keys.C] || this[Keys.Space])
            {
                this.flying = true;
            }

            if (this.flying)
            {
                var moveVector = Vector3.Zero;

                if (this[Keys.W])
                {
                    moveVector += this.Camera.Direction;
                }

                if (this[Keys.S])
                {
                    moveVector += -this.Camera.Direction;
                }

                if (this[Keys.A])
                {
                    moveVector += -this.Camera.Right;
                }

                if (this[Keys.D])
                {
                    moveVector += this.Camera.Right;
                }

                if (this[Keys.C])
                {
                    moveVector += -this.Camera.Up;
                }

                if (this[Keys.Space])
                {
                    moveVector += this.Camera.Up;
                }

                if (moveVector != Vector3.Zero)
                {
                    moveVector = Vector3.Normalize(moveVector);
                    moveVector *= this.size / 100;
                    this.Camera.Position += moveVector;
                }
            }
            else
            {
                var a = Math.Tau * this.t / 10;
                var (x, z) = Math.SinCos(a);
                var t = Math.Sin(a / 3);
                var p = new Vector3((float)(this.size * x), (float)(this.size / 10 * t), (float)(this.size * z));
                this.Camera.Position = this.center + p;
                this.Camera.Direction = -p;
            }

            var before = (ulong)this.t;
            this.t += elapsed.TotalSeconds;
            if ((ulong)this.t != before)
            {
                this.animate = (ulong)Random.Shared.NextInt64();
            }

            base.AdvanceFrame(elapsed);
        }

        protected override void DrawScene(Graphics g, Bitmap buffer, float[,] depthBuffer)
        {
            var (_, parts) = this.models[this.activeModel];
            foreach (var (_, versions) in parts)
            {
                foreach (var mesh in versions[0].Meshes)
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
            }

            base.DrawScene(g, buffer, depthBuffer);
        }
    }
}
