namespace RenderLoop.MGS
{
    using System.Numerics;

    public struct Mesh
    {
        public Mesh(Vector3[] vertices, Vector2[] textureCoords, Vector3[] normals, Face[] faces)
        {
            this.Vertices = vertices;
            this.TextureCoords = textureCoords;
            this.Normals = normals;
            this.Faces = faces;
        }

        public Vector3[] Vertices { get; }

        public Vector2[] TextureCoords { get; }

        public Vector3[] Normals { get; }

        public Face[] Faces { get; }
    }
}
