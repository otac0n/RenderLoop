// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.SilkRenderer.GL
{
    using Silk.NET.OpenGL;
    using System.Drawing;
    using System.Drawing.Imaging;

    public sealed class TextureHandle : IDisposable
    {
        private readonly GL gl;

        public unsafe TextureHandle(GL gl, Bitmap source)
        {
            this.gl = gl;

            this.Handle = gl.GenTexture();
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, this.Handle);

            BitmapData? bmp = null;
            try
            {
                bmp = source.LockBits(new Rectangle(Point.Empty, source.Size), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                var ptr = (byte*)bmp.Scan0;
                gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)bmp.Width, (uint)bmp.Height, 0, Silk.NET.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, ptr);
            }
            finally
            {
                if (bmp != null)
                {
                    source.UnlockBits(bmp);
                }
            }

            gl.TextureParameter(this.Handle, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            gl.TextureParameter(this.Handle, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            gl.TextureParameter(this.Handle, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            gl.TextureParameter(this.Handle, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            gl.GenerateMipmap(TextureTarget.Texture2D);
            gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        public uint Handle { get; private set; }

        public void Activate(TextureUnit textureUnit = TextureUnit.Texture0)
        {
            this.gl.ActiveTexture(textureUnit);
            this.gl.BindTexture(TextureTarget.Texture2D, this.Handle);
        }

        public void Dispose()
        {
            if (this.Handle != 0)
            {
                this.gl.DeleteBuffer(this.Handle);
                this.Handle = 0;
            }
        }
    }
}
