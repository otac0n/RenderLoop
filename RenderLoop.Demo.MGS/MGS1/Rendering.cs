// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS1
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using RenderLoop.SilkRenderer.GL;
    using Silk.NET.OpenGL;
    using ShaderHandle = RenderLoop.SilkRenderer.GL.ShaderHandle<(System.Numerics.Vector3 Position, System.Numerics.Vector2 UV)>;

    internal static class Rendering
    {
        public static ShaderHandle MakeDefaultShader(GL gl) => new(
            gl,
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
                uniform float fragment_opacity;
                in vec2 fragment_textureCoords;
                out vec4 color;
                void main()
                {
                    color = texture(uniform_texture, fragment_textureCoords);
                    if (color.a <= 0.5)
                    {
                        discard;
                    }
                    color.a = fragment_opacity;
                }
            """);

        public static void RenderMeshes(GL gl, Camera camera, ShaderHandle shader, Func<ushort, TextureHandle?> getTexture, IEnumerable<Mesh> meshes)
        {
            shader.SetUniform("uniform_cameraMatrix", camera.Matrix);
            shader.SetUniform("fragment_opacity", 1.0f);

            var anyTransparent = false;
            foreach (var mesh in meshes)
            {
                if ((mesh.Flags & DrawingFlags.Visible) == DrawingFlags.Visible)
                {
                    if ((mesh.Flags & DrawingFlags.Trasparent) == DrawingFlags.Trasparent)
                    {
                        anyTransparent = true;
                        continue;
                    }

                    if ((mesh.Flags & DrawingFlags.TwoSided) == DrawingFlags.TwoSided)
                    {
                        gl.Disable(GLEnum.CullFace);
                    }
                    else
                    {
                        gl.Enable(GLEnum.CullFace);
                    }

                    foreach (var face in mesh.Faces)
                    {
                        var texture = getTexture(face.TextureId);
                        texture?.Activate();
                        shader.SetUniform("uniform_texture", 0);

                        var vertices = face.VertexIndices.Select((i, j) => (position: mesh.Vertices[i], uv: mesh.TextureCoords[face.TextureIndices[j]])).ToArray();
                        gl.DrawStrip(vertices, shader);
                    }
                }
            }

            if (anyTransparent)
            {
                shader.SetUniform("fragment_opacity", 0.5f);

                foreach (var mesh in meshes)
                {
                    if ((mesh.Flags & DrawingFlags.Visible) == DrawingFlags.Visible &&
                        (mesh.Flags & DrawingFlags.Trasparent) == DrawingFlags.Trasparent)
                    {
                        if ((mesh.Flags & DrawingFlags.TwoSided) == DrawingFlags.TwoSided)
                        {
                            gl.Disable(GLEnum.CullFace);
                        }
                        else
                        {
                            gl.Enable(GLEnum.CullFace);
                        }

                        foreach (var face in mesh.Faces)
                        {
                            var texture = getTexture(face.TextureId);
                            texture?.Activate();
                            shader.SetUniform("uniform_texture", 0);

                            var vertices = face.VertexIndices.Select((i, j) => (position: mesh.Vertices[i], uv: mesh.TextureCoords[face.TextureIndices[j]])).ToArray();
                            gl.DrawStrip(vertices, shader);
                        }
                    }
                }
            }
        }
    }
}
