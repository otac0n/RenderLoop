// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.Demo
{
    using System;
    using System.Linq;
    using System.Numerics;
    using RenderLoop.SilkRenderer.GL;
    using Silk.NET.OpenGL;
    using Silk.NET.Windowing;
    using static Geometry;

    internal class CubeGL : CameraSpinner
    {
        protected readonly IWindow display;
        protected GL gl;

        protected ShaderHandle<(Vector3 position, Vector2 uv)> shader;

        protected (Vector3 position, Vector2 uv)[][] shapes = Array.ConvertAll(Shapes, shape => shape.Select((i, j) => (index: Vertices[i], uv: UV[j])).ToArray());

        public CubeGL(IWindow display)
            : base(display)
        {
            this.display = display;
        }

        protected override void Initialize()
        {
            this.gl = GL.GetApi(this.display);
            this.display.FramebufferResize += size => this.gl.Viewport(size);

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
                    in vec2 fragment_textureCoords;
                    out vec4 color;
                    void main()
                    {
                        color = (int(fragment_textureCoords.x * 4) + int(fragment_textureCoords.y * 4)) % 2 == 0
                            ? vec4(1.0, 1.0, 1.0, 1.0)
                            : vec4(0.5, 0.5, 0.5, 1.0);
                    }
                """);
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
