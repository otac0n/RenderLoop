﻿namespace RenderLoop.Demo.MGS
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Numerics;
    using System.Windows.Forms;
    using Microsoft.Extensions.DependencyInjection;
    using RenderLoop.SoftwareRenderer;
    using RenderLoop.Input;

    public class VehicleDisplay : GameLoop
    {
        private static readonly Dictionary<string, (string[] versions, (string attachTo, int atIndex)? attach, (int index, Vector3 min, Vector3 max)[] freedoms)>[] Sources =
        [
            new()
            {
                ["projectile"] = (
                    ["s05a/model/3dcc.kmd"],
                    ("chassis", 5),
                    []
                ),
                ["chassis"] = (
                    ["s05a/model/5108.kmd"],
                    null,
                    [
                        (3, Angles(0, 3 / 8f, 0), Angles(0, 3 / 8f, 0)), // Turret
                        (5, Angles(1 / 16f, 0, 0), Angles(1 / 32f, 0, 0)), // Cannon
                        (6, Angles(0, 1 / 4f, 0), Angles(0, 1 / 32f, 0)), // MG1 Seat
                        (8, Angles(1 / 4f, 0, 0), Angles(0, 0, 0)), // MG1 Lid
                        (9, Angles(1 / 8f, 0, 0), Angles(1 / 32f, 0, 0)), // MG1
                        (11, Angles(1 / 4f, 0, 0), Angles(0, 0, 0)), // MG2 Lid
                        //(12, Angles(0, 0, 0), Angles(0, 0, 0)), // MG2 Seat
                        (13, Angles(1 / 8f, 1 / 16f, 0), Angles(1 / 32f, 1 / 8f, 0)), // MG2
                    ]
                ),
                ["track_a"] = (
                    [
                        "s05a/model/c236.kmd",
                        "s05a/model/c237.kmd",
                        "s05a/model/c238.kmd",
                    ],
                    ("chassis", 1),
                    []
                ),
                ["track_b"] = (
                    [
                        "s05a/model/c2f6.kmd",
                        "s05a/model/c2f7.kmd",
                        "s05a/model/c2f8.kmd",
                    ],
                    ("chassis", 1),
                    []
                ),
            },
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
            new()
            {
                ["airframe"] = (
                    ["s11h/model/c7dd.kmd"],
                    null,
                    [
                        (1, Angles(0, 1 / 2f, 0), Angles(0, 1 / 2f, 0)), // Main Rotor
                        (2, Angles(1 / 2f, 0, 0), Angles(1 / 2f, 0, 0)), // Tail Rotor
                        (3, Angles(0, 1 / 8f, 0), Angles(0, 1 / 8f, 0)), // Turret
                        (4, Angles(0, 0, 0), Angles(3 / 16f, 0, 0)), // Cannon
                    ]
                ),
                ["canopy"] = (
                    ["s11h/model/57fc.kmd"],
                    null,
                    [
                    ]
                ),
                ["pilot"] = (
                    [
                        "s11h/model/d85b.kmd", // Liquid
                    ],
                    null,
                    [
                    ]
                ),
                ["gunner"] = (
                    [
                        "s11h/model/6fba.kmd",
                    ],
                    null,
                    [
                    ]
                ),
                ["missile_1"] = (
                    ["s11h/model/7f5f.kmd"],
                    ("airframe", 5),
                    [
                    ]
                ),
                ["trail_1"] = (
                    [
                        "s00a/model/8075.kmd",
                        "s00a/model/8076.kmd",
                        "s00a/model/8077.kmd",
                    ],
                    ("missile_1", 0),
                    [
                    ]
                ),
                ["missile_2"] = (
                    ["s11h/model/7f5f.kmd"],
                    ("airframe", 6),
                    [
                    ]
                ),
                ["trail_2"] = (
                    [
                        "s00a/model/8075.kmd",
                        "s00a/model/8076.kmd",
                        "s00a/model/8077.kmd",
                    ],
                    ("missile_2", 0),
                    [
                    ]
                ),
                ["missile_3"] = (
                    ["s11h/model/7f5f.kmd"],
                    ("airframe", 7),
                    [
                    ]
                ),
                ["trail_3"] = (
                    [
                        "s00a/model/8075.kmd",
                        "s00a/model/8076.kmd",
                        "s00a/model/8077.kmd",
                    ],
                    ("missile_3", 0),
                    [
                    ]
                ),
                ["missile_4"] = (
                    ["s11h/model/7f5f.kmd"],
                    ("airframe", 8),
                    [
                    ]
                ),
                ["trail_4"] = (
                    [
                        "s00a/model/8075.kmd",
                        "s00a/model/8076.kmd",
                        "s00a/model/8077.kmd",
                    ],
                    ("missile_4", 0),
                    [
                    ]
                ),
                //["gear_a"] = (
                //    ["s11h/model/647f.kmd"],
                //    null,
                //    [
                //    ]
                //),
                //["gear_b"] = (
                //    ["s11h/model/6485.kmd"],
                //    null,
                //    [
                //    ]
                //),
                //["gear_c"] = (
                //    ["s11h/model/6479.kmd"],
                //    null,
                //    [
                //    ]
                //),
            },
        ];

        private readonly Display display;
        private readonly Camera Camera = new();
        private double t;
        private ulong animate;
        private Vector3 center;
        private float size;
        private int activeModel;
        private readonly ControlChangeTracker controlChangeTracker;
        private readonly StageDirVirtualFileSystem stageDir;

        private readonly List<(Dictionary<string, (string[] versions, (string attachTo, int atIndex)? attach, (int index, Vector3 min, Vector3 max)[] freedoms)> source, Dictionary<string, Model[]> parts)> models;
        private readonly Dictionary<string, (ushort id, Bitmap? texture)> textures = [];
        private readonly Dictionary<ushort, Bitmap?> textureLookup = [];

        public VehicleDisplay(IServiceProvider serviceProvider, Display display)
            : base(display)
        {
            this.display = display;
            this.controlChangeTracker = serviceProvider.GetRequiredService<ControlChangeTracker>();

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

            this.display.KeyPreview = true;
            this.display.ClientSize = new(640, 480);
            this.display.PreviewKeyDown += this.PreviewKeyDown;
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
            var p = new Vector3(this.size, this.size / 10, this.size);
            this.Camera.Position = this.center + p;
            this.Camera.Direction = -p;
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

        protected void PreviewKeyDown(object? sender, PreviewKeyDownEventArgs e)
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
                this.UpdateModel();
            }
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

            var moveVector = Vector3.Zero;
            var right = 0.0;
            var up = 0.0;

            var bindings = new Bindings<Action<double>>();
            bindings.BindCurrent(
                [(c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Name == "X", v => (v - 0.5) * 2)],
                v => moveVector.X += (float)v);
            bindings.BindCurrent(
                [(c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Name == "Y", v => (v - 0.5) * 2)],
                v => moveVector.Y += (float)v);
            bindings.BindCurrent(
                [(c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Name == "Ry", v => (v - 0.5) * 2)],
                v => up -= v);
            bindings.BindCurrent(
                [(c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Name == "Rx", v => (v - 0.5) * 2)],
                v => right -= v);

            this.controlChangeTracker.ProcessChanges(bindings);

            var moveLength = moveVector.Length();
            if (moveLength > 0.1)
            {
                var scale = moveLength >= 1
                    ? 1f / moveLength
                    : (moveLength - 0.1f) / (0.9f * moveLength);

                moveVector *= scale;
                this.Camera.Position += (this.Camera.Right * moveVector.X - this.Camera.Direction * moveVector.Y) * (float)(elapsed.TotalSeconds * this.size);
            }

            if (Math.Abs(right) > 0.1)
            {
                right *= elapsed.TotalSeconds / 10 * Math.Tau;

                var (sin, cos) = Math.SinCos(right);
                var v = this.Camera.Direction;
                var k = this.Camera.Up;
                this.Camera.Direction = v * (float)cos + Vector3.Cross(k, v) * (float)sin + k * Vector3.Dot(k, v) * (float)(1 - cos);
            }

            if (Math.Abs(up) > 0.1)
            {
                up *= elapsed.TotalSeconds / 10 * Math.Tau;

                var (sin, cos) = Math.SinCos(up);
                var v = this.Camera.Direction;
                var k = this.Camera.Right;
                this.Camera.Direction = v * (float)cos + Vector3.Cross(k, v) * (float)sin + k * Vector3.Dot(k, v) * (float)(1 - cos);
            }

            var before = (ulong)this.t;
            this.t += elapsed.TotalSeconds;
            if ((ulong)this.t != before)
            {
                this.animate = (ulong)Random.Shared.NextInt64();
            }
        }

        protected override void DrawScene(TimeSpan elapsed)
        {
            this.display.PaintFrame(elapsed, (Graphics g, Bitmap buffer, float[,] depthBuffer) =>
            {
                this.Camera.Width = buffer.Width;
                this.Camera.Height = buffer.Height;

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
                            Display.DrawStrip(indices, s => transformed[s.index], (v, vertices) =>
                                Display.FillTriangle(buffer, depthBuffer, vertices, BackfaceCulling.None, perspective =>
                                {
                                    var uv = Display.MapCoordinates(perspective, [
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
            });
        }
    }
}
