namespace RenderLoop.MGS
{
    using System;
    using System.Numerics;

    public class Mesh
    {
        private Vector3[] absoluteVertices;
        private Vector3? absoluteOrigin;

        public Mesh(Vector3 relativeOrigin, Vector3[] relativeVertices, Vector2[] textureCoords, Vector3[] normals, Face[] faces, Mesh? relativeMesh = null)
        {
            this.RelativeMesh = relativeMesh;
            this.RelativeOrigin = relativeOrigin;
            this.RelativeVertices = relativeVertices;
            this.TextureCoords = textureCoords;
            this.Normals = normals;
            this.Faces = faces;
        }

        public Mesh? RelativeMesh { get; }

        public Vector3 RelativeOrigin { get; }

        public Vector3 Origin => this.absoluteOrigin ??= (this.RelativeMesh?.Origin ?? Vector3.Zero) + this.RelativeOrigin;

        public Vector3[] RelativeVertices { get; }

        public Vector3[] Vertices => this.absoluteVertices ??= Array.ConvertAll(this.RelativeVertices, v => this.Origin + v);

        public Vector2[] TextureCoords { get; }

        public Vector3[] Normals { get; }

        public Face[] Faces { get; }
    }
}
