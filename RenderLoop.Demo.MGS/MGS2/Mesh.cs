// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS2
{
    using System.Numerics;
    using Bounds = (System.Numerics.Vector3 Start, System.Numerics.Vector3 End);

    public class Mesh : RenderLoop.Mesh
    {
        public Mesh(uint flags, Bounds bounds, Vector3 relativeOrigin, Mesh? relativeMesh, Vector3[] relativeVertices, Vector3[] normals, Vector2[] textureCoords, Face[] faces)
            : base(relativeOrigin, relativeVertices, textureCoords, normals, faces, relativeMesh)
        {
            this.Flags = flags;
        }

        public uint Flags { get; }

        public new Face[] Faces => (Face[])base.Faces;
    }
}
