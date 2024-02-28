namespace RenderLoop.SilkRenderer.GL
{
    using System.Numerics;
    using Silk.NET.OpenGL;

    public static class DynamicDraw
    {
        public static unsafe void DrawTriangles(this GL gl, (Vector3, Vector2)[] vertices, ShaderHandle<(Vector3, Vector2)> shader)
        {
            gl.DrawPrimitives(vertices, sizeof(float), VertexAttribPointerType.Float, sizeof(float), VertexAttribPointerType.Float, PrimitiveType.Triangles, shader);
        }

        public static unsafe void DrawStrip(this GL gl, (Vector3, Vector2)[] vertices, ShaderHandle<(Vector3, Vector2)> shader)
        {
            gl.DrawPrimitives(vertices, sizeof(float), VertexAttribPointerType.Float, sizeof(float), VertexAttribPointerType.Float, PrimitiveType.TriangleStrip, shader);
        }

        public static void PaintFrame(this GL gl, Action paint)
        {
            gl.Enable(EnableCap.DepthTest);
            gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
            paint();
        }

        private static unsafe void DrawPrimitives<T1, T2>(this GL gl, (T1, T2)[] vertices, int t1Size, VertexAttribPointerType t1Type, int t2Size, VertexAttribPointerType t2Type, PrimitiveType primitiveType, ShaderHandle<(T1, T2)> shader)
            where T1 : unmanaged
            where T2 : unmanaged
        {
            var tSize = sizeof((T1, T2));

            shader.Use();
            var vao = gl.GenVertexArray();
            try
            {
                gl.BindVertexArray(vao);

                var vbo = gl.GenBuffer();
                try
                {
                    gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

                    fixed (void* v = &vertices[0])
                    {
                        gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * tSize), v, BufferUsageARB.DynamicDraw);
                    }

                    gl.VertexAttribPointer(0, sizeof(T1) / t1Size, t1Type, false, (uint)tSize, (void*)0);
                    gl.EnableVertexAttribArray(0);

                    gl.VertexAttribPointer(1, sizeof(T2) / t2Size, t2Type, false, (uint)tSize, (void*)sizeof(T1));
                    gl.EnableVertexAttribArray(1);

                    gl.DrawArrays(primitiveType, 0, (uint)vertices.Length);
                }
                finally
                {
                    gl.DeleteBuffer(vbo);
                }
            }
            finally
            {
                gl.DeleteVertexArray(vao);
            }
        }
    }
}
