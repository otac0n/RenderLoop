namespace RenderLoop.SilkRenderer.GL
{
    using System;
    using Silk.NET.OpenGL;

    public static class ShaderCompiler
    {
        public static uint LinkShaders(this GL gl, uint vertex, uint fragment)
        {
            var handle = gl.CreateProgram();
            gl.AttachShader(handle, vertex);
            gl.AttachShader(handle, fragment);
            try
            {
                gl.LinkProgram(handle);
                gl.GetProgram(handle, GLEnum.LinkStatus, out var status);
                if (status == 0)
                {
                    throw new InvalidProgramException($"Program failed to link with error:{Environment.NewLine}{gl.GetProgramInfoLog(handle)}");
                }

                return handle;
            }
            finally
            {
                gl.DetachShader(handle, vertex);
                gl.DetachShader(handle, fragment);
            }
        }

        public static T WithShader<T>(this GL gl, ShaderType type, Func<string> getShader, Func<uint, T> action)
        {
            var src = getShader();
            var handle = gl.CreateShader(type);
            try
            {
                gl.ShaderSource(handle, src);
                gl.CompileShader(handle);
                var infoLog = gl.GetShaderInfoLog(handle);
                if (!string.IsNullOrWhiteSpace(infoLog))
                {
                    throw new InvalidProgramException($"Error compiling '{type}', failed with error:{Environment.NewLine}{infoLog}");
                }

                return action(handle);
            }
            finally
            {
                gl.DeleteShader(handle);
            }
        }
    }
}
