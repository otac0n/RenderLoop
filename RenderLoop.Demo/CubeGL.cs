﻿namespace RenderLoop.Demo
{
    using System;
    using System.Linq;
    using System.Numerics;
    using RenderLoop.SilkRenderer.GL;
    using Silk.NET.OpenGL;
    using Silk.NET.Windowing;

    internal class CubeGL : GameLoop<CubeGL.AppState>
    {
        public record class AppState(double T);

        protected readonly IWindow display;
        protected GL gl;
        protected readonly Camera Camera = new();

        protected ShaderHandle<(Vector3 position, Vector2 uv)> shader;

        /// <remarks>
        /// 0 -- 1<br/>
        /// |  / |<br/>
        /// | /  |<br/>
        /// 2 -- 3
        /// </remarks>
        protected static readonly Vector2[] UV = [
            new(0, 0),
            new(0, 1),
            new(1, 0),
            new(1, 1),
        ];

        protected static readonly Vector3[] Vertices = [
            new Vector3(-1, -1, +1) / 2, // L, F, T
            new Vector3(-1, +1, +1) / 2, // L, B, T
            new Vector3(+1, +1, +1) / 2, // R, B, T
            new Vector3(+1, -1, +1) / 2, // R, F, T
            new Vector3(-1, -1, -1) / 2, // L, F, B
            new Vector3(-1, +1, -1) / 2, // L, B, B
            new Vector3(+1, +1, -1) / 2, // R, B, B
            new Vector3(+1, -1, -1) / 2, // R, F, B
        ];

        /// <remarks>
        /// Same order as <see cref="UV"/>
        /// </remarks>
        protected static readonly uint[][] Shapes = [
            [1, 0, 2, 3], // TOP
            [4, 5, 7, 6], // BOTTOM
            [3, 0, 7, 4], // FRONT
            [1, 2, 5, 6], // BACK
            [0, 1, 4, 5], // LEFT
            [2, 3, 6, 7], // RIGHT
        ];

        protected (Vector3 position, Vector2 uv)[][] shapes = Array.ConvertAll(Shapes, shape => shape.Select((i, j) => (index: Vertices[i], uv: UV[j])).ToArray());

        public CubeGL(IWindow display)
            : base(display, new AppState(0))
        {
            this.display = display;
        }

        protected override void Initialize()
        {
            this.gl = GL.GetApi(this.display);

            this.shader = new ShaderHandle<(Vector3 position, Vector2 uv)>(
                this.gl,
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
                    in vec2 fragment_textureCoords;
                    out vec4 color;
                    void main()
                    {
                        color = (int(fragment_textureCoords.x * 4) + int(fragment_textureCoords.y * 4)) % 2 == 0
                            ? vec4(1.0, 1.0, 1.0, 1.0)
                            : vec4(0.6, 0.6, 0.6, 1.0);
                    }
                """);
        }

        protected sealed override void AdvanceFrame(ref AppState state, TimeSpan elapsed)
        {
            var dist = 2;

            var a = Math.Tau * state.T / 3;
            var (x, y) = Math.SinCos(a);
            var z = Math.Sin(a / 3);
            var p = new Vector3((float)(dist * x), (float)(dist * y), (float)(dist / 2 * z));

            this.Camera.Position = p;
            this.Camera.Direction = -p;

            state = state with
            {
                T = state.T + elapsed.TotalSeconds,
            };
        }

        protected override void DrawScene(AppState state, TimeSpan elapsed)
        {
            this.gl.PaintFrame(() =>
            {
                this.Camera.Width = this.display.FramebufferSize.X;
                this.Camera.Height = this.display.FramebufferSize.Y;

                this.shader.SetUniform("uniform_cameraMatrix", this.Camera.Matrix);

                foreach (var shape in this.shapes)
                {
                    this.gl.DrawStrip(shape, this.shader);
                }
            });
        }
    }
}