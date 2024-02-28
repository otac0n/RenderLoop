namespace RenderLoop.SilkRenderer.GL
{
    using System;
    using System.Numerics;
    using Silk.NET.OpenGL;

    public sealed class ShaderHandle<TVertex> : IDisposable
    {
        private readonly uint handle;
        private readonly GL gl;

        public ShaderHandle(GL gl, Func<string> getVertexShader, Func<string> getFragmentShader)
        {
            this.gl = gl;

            this.handle =
                this.gl.WithShader(ShaderType.VertexShader, getVertexShader, vertex =>
                    this.gl.WithShader(ShaderType.FragmentShader, getFragmentShader, fragment =>
                        this.gl.LinkShaders(vertex, fragment)));
        }

        public void Use() => this.gl.UseProgram(this.handle);

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
