namespace RenderLoop.MGS
{
    public struct Face
    {
        public Face(uint textureId, int[] vertexIndices, int[] normalIndices)
        {
            this.TextureId = textureId;
            this.VertexIndices = vertexIndices;
            this.NormalIndices = normalIndices;
        }

        public uint TextureId { get; set; }

        public int[] VertexIndices { get; set; }

        public int[] NormalIndices { get; set; }
    }
}
