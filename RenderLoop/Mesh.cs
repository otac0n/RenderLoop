namespace RenderLoop
{
    using System;
    using System.Numerics;
    using Whisk;

    public class Mesh
    {
        private MutableDependency<Mesh?> relativeMesh;
        private MutableDependency<Matrix4x4> rotation;
        private PureDependency<Matrix4x4> modelToWorld;
        private PureDependency<Vector3[]> vertices;

        public Mesh(Vector3 relativeOrigin, Vector3[] relativeVertices, Vector2[] textureCoords, Vector3[] normals, Face[] faces, Mesh? relativeMesh = null)
        {
            this.RelativeOrigin = relativeOrigin;
            this.RelativeVertices = relativeVertices;
            this.TextureCoords = textureCoords;
            this.Normals = normals;
            this.Faces = faces;

            this.relativeMesh = D.Mutable(relativeMesh);
            this.rotation = D.Mutable(Matrix4x4.Identity);
            var parentToWorld = D.Unwrap(this.relativeMesh.Pure(v => v?.modelToWorld.Pure(m => (Matrix4x4?)m)));
            this.modelToWorld = D.Pure(D.All(parentToWorld, this.rotation), () => this.rotation.Value * Matrix4x4.CreateTranslation(this.RelativeOrigin) * (parentToWorld.Value ?? Matrix4x4.Identity));
            this.vertices = this.modelToWorld.Pure(m => Array.ConvertAll(this.RelativeVertices, v => Vector3.Transform(v, m)));
        }

        public Mesh? RelativeMesh
        {
            get => this.relativeMesh.Value;
            set => this.relativeMesh.Value = value;
        }

        public Vector3 RelativeOrigin { get; }

        public Vector3[] RelativeVertices { get; }

        public Vector3[] Vertices => this.vertices.Value;

        public Vector2[] TextureCoords { get; }

        public Vector3[] Normals { get; }

        public Face[] Faces { get; }

        public Matrix4x4 Rotation
        {

            get => this.rotation.Value;
            set => this.rotation.Value = value;
        }

        public Matrix4x4 ModelToWorld => this.modelToWorld.Value;
    }
}
