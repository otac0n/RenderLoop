namespace RenderLoop.MGS
{
    using System.Numerics;
    using Vertex = (int x, int y, int z, int w);

    public struct Mesh
    {
        public Mesh(Vertex[] vertices, Vector2[] textureCoords, Vector3[] normals, Face[] faces)
        {
            this.Vertices = vertices;
            this.TextureCoords = textureCoords;
            this.Normals = normals;
            this.Faces = faces;
        }

        public Vertex[] Vertices { get; }

        public Vector2[] TextureCoords { get; }

        public Vector3[] Normals { get; }

        public Face[] Faces { get; }
    }
}
