// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop
{
    public struct Face
    {
        public Face(ushort textureId, uint[] vertexIndices, uint[] normalIndices, uint[] textureIndices)
        {
            this.TextureId = textureId;
            this.VertexIndices = vertexIndices;
            this.NormalIndices = normalIndices;
            this.TextureIndices = textureIndices;
        }

        public ushort TextureId { get; set; }

        public uint[] VertexIndices { get; set; }

        public uint[] NormalIndices { get; set; }

        public uint[] TextureIndices { get; set; }
    }
}
