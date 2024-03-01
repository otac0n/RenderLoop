namespace RenderLoop.SilkRenderer.GL
{
    using System;
    using System.Numerics;
    using Silk.NET.OpenGL;

    public sealed class ShaderHandle<TVertex> : IDisposable
    {
        private readonly GL gl;
        private readonly (int count, VertexAttribPointerType type, uint stride, uint offset)[] attributes;
        private readonly uint handle;

        public ShaderHandle(GL gl, (int count, VertexAttribPointerType type, int size)[] attributes, Func<string> getVertexShader, Func<string> getFragmentShader)
        {
            this.gl = gl;

            var offset = 0u;
            var stride = attributes.Sum(a => a.count * a.size);
            this.attributes = new (int count, VertexAttribPointerType type, uint stride, uint offset)[attributes.Length];
            for (var i = 0u; i < this.attributes.Length; i++)
            {
                var (count, type, size) = attributes[i];
                var width = (uint)(count * size);
                this.attributes[i] = (count, type, (uint)stride, offset);
                offset += width;
            }

            this.handle =
                this.gl.WithShader(ShaderType.VertexShader, getVertexShader, vertex =>
                    this.gl.WithShader(ShaderType.FragmentShader, getFragmentShader, fragment =>
                        this.gl.LinkShaders(vertex, fragment)));
        }

        public unsafe void Bind()
        {
            this.gl.UseProgram(this.handle);

            for (var i = 0u; i < this.attributes.Length; i++)
            {
                var (count, type, stride, offset) = this.attributes[i];
                this.gl.VertexAttribPointer(i, count, type, false, stride, (void*)offset);
                this.gl.EnableVertexAttribArray(i);
            }
        }

        public void SetUniform(string name, int value) =>
            this.gl.Uniform1(this.GetUniformLocation(name), value);

        public unsafe void SetUniform(string name, Matrix4x4 value) =>
            this.gl.UniformMatrix4(this.GetUniformLocation(name), 1, false, (float*)&value);

        public void SetUniform(string name, float value) =>
            this.gl.Uniform1(this.GetUniformLocation(name), value);

        public void Dispose()
        {
            this.gl.DeleteProgram(this.handle);
            GC.SuppressFinalize(this);
        }

        private int GetUniformLocation(string name) =>
            this.gl.GetUniformLocation(this.handle, name) switch
            {
                -1 => throw new ArgumentException($"Uniform '{name}' not found on shader.", nameof(name)),
                var value => value,
            };
    }
}
