// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop
{
    public class Face
    {
        public Face(uint[] vertexIndices, uint[] normalIndices, uint[] textureIndices)
        {
            this.VertexIndices = vertexIndices;
            this.NormalIndices = normalIndices;
            this.TextureIndices = textureIndices;
        }

        public uint[] VertexIndices { get; set; }

        public uint[] NormalIndices { get; set; }

        public uint[] TextureIndices { get; set; }
    }
}
