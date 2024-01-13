namespace RenderLoop.MGS
{
    public struct Face
    {
        public Face(ushort textureId, int[] vertexIndices, int[] normalIndices, int[] textureIndices)
        {
            this.TextureId = textureId;
            this.VertexIndices = vertexIndices;
            this.NormalIndices = normalIndices;
            this.TextureIndices = textureIndices;
        }

        public ushort TextureId { get; set; }

        public int[] VertexIndices { get; set; }

        public int[] NormalIndices { get; set; }

        public int[] TextureIndices { get; set; }
    }
}
