// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS2
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Threading.Tasks;
    using DevDecoder.HIDDevices.Usages;
    using ImGuiNET;
    using Microsoft.Extensions.DependencyInjection;
    using RenderLoop.Input;
    using RenderLoop.SilkRenderer.GL;
    using Silk.NET.Input;
    using Silk.NET.OpenGL;
    using Silk.NET.OpenGL.Extensions.ImGui;
    using Silk.NET.Windowing;

    public class ModelDisplay : GameLoop
    {
        private static readonly double ModelDisplaySeconds = 10.0;
        private readonly MGS.Program.Options options;
        private readonly IWindow display;
        private GL gl;
        private ImGuiController controller = null;
        private ShaderHandle<(Vector3 position, Vector2 uv)> shader;
        private readonly Camera Camera = new();
        private int frame;
        private readonly int frames = 90;
        private double nextModel = ModelDisplaySeconds;
        private int activeModel;
        private bool flying;
        private Vector3 center;
        private float size;
        private readonly ControlChangeTracker controlChangeTracker;
        private readonly IList<string> models;
        private readonly Task<Dictionary<ulong, string>> imageIdLookup;
        private readonly Dictionary<string, Task<Model>> modelLookup = [];
        private readonly Dictionary<ulong, Task<Bitmap>> bitmapLookup = [];
        private readonly Dictionary<ulong, TextureHandle> textureLookup = [];

        public ModelDisplay(IServiceProvider serviceProvider, IWindow display)
            : base(display)
        {
            this.display = display;
            this.controlChangeTracker = serviceProvider.GetRequiredService<ControlChangeTracker>();
            this.options = serviceProvider.GetRequiredService<MGS.Program.Options>();
            var basePath = Path.Combine(this.options.SteamApps, WellKnownPaths.MGS2Assets);
            this.models = Directory.EnumerateFiles(basePath, "*.kms", SearchOption.AllDirectories).ToList();
            this.imageIdLookup = Task.Run(() =>
            {
                var result = new Dictionary<ulong, string>();

                foreach (var file in Directory.EnumerateFiles(basePath, "*.tri", SearchOption.AllDirectories))
                {
                    foreach (var id in TriFile.List(file))
                    {
                        result[id] = file;
                    }
                }

                return result;
            });

            this.activeModel = Random.Shared.Next(this.models.Count);

            this.Camera.Up = new Vector3(0, 1, 0);

            this.display.Size = new(640, 480);
        }

        private void UpdateModel()
        {
            this.nextModel = ModelDisplaySeconds;
            this.activeModel = (this.activeModel + this.models.Count) % this.models.Count;
            this.flying = false;
        }

        private Model? EnsureModel(string file)
        {
            static Model Get(string file)
            {
                using var stream = File.OpenRead(file);
                var model = Model.FromStream(stream);
                return model;
            }

            if (!this.modelLookup.TryGetValue(file, out var task))
            {
                this.modelLookup[file] = Task.Run(() => Get(file));
                return null;
            }

            return task.Status == TaskStatus.RanToCompletion
                ? task.Result
                : null;
        }

        private TextureHandle? EnsureTexture(ulong id)
        {
            if (!this.textureLookup.TryGetValue(id, out var handle))
            {
                if (!this.bitmapLookup.TryGetValue(id, out var task))
                {
                    this.bitmapLookup[id] = task = this.imageIdLookup.ContinueWith(lookup =>
                    {
                        try
                        {
                            using var stream = File.OpenRead(lookup.Result[id]);
                            return TriFile.Load(stream, (uint)id);
                        }
                        catch
                        {
                            throw;
                        }
                    });
                }

                if (task.Status == TaskStatus.RanToCompletion && task.Result is Bitmap bitmap)
                {
                    handle = this.textureLookup[id] = new TextureHandle(this.gl, bitmap);
                    this.bitmapLookup.Remove(id);
                }
                else
                {
                    handle = null;
                }
            }

            return handle;
        }

        protected override void Initialize()
        {
            this.gl = GL.GetApi(this.display);
            this.display.FramebufferResize += size => this.gl.Viewport(size);
            this.controller = new ImGuiController(this.gl, this.display, this.display.CreateInput());

            this.shader = new ShaderHandle<(Vector3 position, Vector2 uv)>(
                this.gl,
                [
                    (3, VertexAttribPointerType.Float, sizeof(float)),
                    (2, VertexAttribPointerType.Float, sizeof(float)),
                ],
                () => """
                    #version 330 core
                    layout (location = 0) in vec3 vertex_position;
                    layout (location = 1) in vec2 vertex_textureCoords;
                    uniform mat4 uniform_cameraMatrix;
                    out vec2 fragment_textureCoords;
                    void main()
                    {
                        gl_Position = uniform_cameraMatrix * vec4(vertex_position, 1.0);
                        fragment_textureCoords = vertex_textureCoords;
                    }
                """,
                () => """
                    #version 330 core
                    uniform sampler2D uniform_texture;
                    uniform int uniform_textureAvailable;
                    in vec2 fragment_textureCoords;
                    out vec4 color;
                    void main()
                    {
                        color = uniform_textureAvailable > 0.5
                            ? texture(uniform_texture, fragment_textureCoords)
                            : vec4(fragment_textureCoords.x, fragment_textureCoords.y, 0.0, 1.0);
                        if (color.a == 0)
                        {
                            discard;
                        }
                    }
                """);

            this.gl.Enable(EnableCap.Blend);
            this.gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            this.UpdateModel();
        }

        protected override void AdvanceFrame(TimeSpan elapsed)
        {
            this.frame++;
            this.controller.Update((float)elapsed.TotalSeconds);

            if (!this.flying && this.EnsureModel(this.models[this.activeModel]) is Model model)
            {
                var min = new Vector3(float.PositiveInfinity);
                var max = new Vector3(float.NegativeInfinity);

                foreach (var mesh in model.Meshes)
                {
                    foreach (var v in mesh.Vertices)
                    {
                        min = Vector3.Min(min, v);
                        max = Vector3.Max(max, v);
                    }

                    foreach (var f in mesh.Faces)
                    {
                        if (f.TextureId != 0)
                        {
                            this.EnsureTexture(f.TextureId);
                        }
                    }
                }

                var size = max - min;
                this.center = min + size / 2;
                this.size = Math.Max(size.X, Math.Max(size.Y, size.Z));
                this.Camera.NearPlane = this.size / 10;
                this.Camera.FarPlane = 2 * Math.Max(this.Camera.NearPlane, this.size);
            }

            var targetModel = this.activeModel;
            var moveVector = Vector3.Zero;
            var right = 0.0;
            var up = 0.0;

            this.nextModel -= elapsed.TotalSeconds;
            if (this.nextModel <= 0 && false)
            {
                this.activeModel++;
            }

            var bindings = new Bindings<Action<double>>();
            bindings.BindCurrent(
                [(c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Usages.Any(u => u == (uint)GenericDesktopPage.X), v => (v - 0.5) * 2)],
                v => moveVector.X += (float)v);
            bindings.BindCurrent(
                [(c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Usages.Any(u => u == (uint)GenericDesktopPage.Y), v => (v - 0.5) * 2)],
                v => moveVector.Y += (float)v);
            bindings.BindCurrent(
                [(c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Usages.Any(u => u == (uint)GenericDesktopPage.Ry), v => (v - 0.5) * 2)],
                v => up -= v);
            bindings.BindCurrent(
                [(c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Usages.Any(u => u == (uint)GenericDesktopPage.Rx), v => (v - 0.5) * 2)],
                v => right -= v);

            bindings.BindEach(
                [c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Usages.Any(u => u == (uint)ButtonPage.Button1)],
                v => this.flying = false);

            bindings.BindEach(
                [c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Usages.Any(u => u == (uint)ButtonPage.Button4)],
                v => this.activeModel--);
            bindings.BindEach(
                [c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Usages.Any(u => u == (uint)ButtonPage.Button5)],
                v => this.activeModel++);

            this.controlChangeTracker.ProcessChanges(bindings);

            if (this.activeModel != targetModel)
            {
                this.UpdateModel();
            }

            var moveLength = moveVector.Length();
            if (moveLength > 0.1)
            {
                this.flying = true;

                var scale = moveLength >= 1
                    ? 1f / moveLength
                    : (moveLength - 0.1f) / (0.9f * moveLength);

                moveVector *= scale;
                this.Camera.Position += (this.Camera.Right * moveVector.X - this.Camera.Direction * moveVector.Y) * (float)(elapsed.TotalSeconds * this.size);
            }

            if (Math.Abs(right) > 0.1)
            {
                this.flying = true;

                right *= elapsed.TotalSeconds / 10 * Math.Tau;

                var (sin, cos) = Math.SinCos(right);
                var v = this.Camera.Direction;
                var k = this.Camera.Up;
                this.Camera.Direction = v * (float)cos + Vector3.Cross(k, v) * (float)sin + k * Vector3.Dot(k, v) * (float)(1 - cos);
            }

            if (Math.Abs(up) > 0.1)
            {
                this.flying = true;

                up *= elapsed.TotalSeconds / 10 * Math.Tau;

                var (sin, cos) = Math.SinCos(up);
                var v = this.Camera.Direction;
                var k = this.Camera.Right;
                this.Camera.Direction = v * (float)cos + Vector3.Cross(k, v) * (float)sin + k * Vector3.Dot(k, v) * (float)(1 - cos);
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

        protected override void DrawScene(TimeSpan elapsed)
        {
            this.gl.PaintFrame(() =>
            {
                this.Camera.Width = this.display.FramebufferSize.X;
                this.Camera.Height = this.display.FramebufferSize.Y;
                this.shader.SetUniform("uniform_cameraMatrix", this.Camera.Matrix);
                this.gl.Disable(EnableCap.CullFace);

                if (this.EnsureModel(this.models[this.activeModel]) is Model model)
                {
                    foreach (var mesh in model.Meshes)
                    {
                        foreach (var face in mesh.Faces)
                        {
                            if (face.TextureId != 0 && this.EnsureTexture(face.TextureId) is TextureHandle texture)
                            {
                                texture.Activate();
                                this.shader.SetUniform("uniform_texture", 0);
                                this.shader.SetUniform("uniform_textureAvailable", 1);
                            }
                            else
                            {
                                this.shader.SetUniform("uniform_textureAvailable", 0);
                            }

                            var vertices = face.VertexIndices.Select((i, j) => (position: mesh.Vertices[i], uv: (face.TextureIndices is uint[] uv ? mesh.TextureCoords?[uv[j]] : null) ?? new(0, 0))).ToArray();
                            this.gl.DrawStrip(vertices, this.shader);
                        }
                    }
                }
            });

            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(new Vector2(this.display.FramebufferSize.X, this.display.FramebufferSize.Y));
            ImGui.Begin("Path", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.DockNodeHost);
            ImGui.BeginChild("PathsMultilineLabel", Vector2.Zero);
            ImGui.TextUnformatted(this.models[this.activeModel]);
            ImGui.EndChild();
            ImGui.End();
            this.controller.Render();
        }
    }
}
