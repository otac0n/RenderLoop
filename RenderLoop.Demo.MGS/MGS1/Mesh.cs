// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS1
{
    using System;
    using System.Numerics;
    using Bounds = (System.Numerics.Vector3 Start, System.Numerics.Vector3 End);

    [Flags]
    public enum DrawingFlags : uint
    {
        Visible = 0b00000000000000000001,
        Trasparent = 0b00000000000000000010,
        NoLight = 0b00000000000000000100,
        TwoSided = 0b00000000010000000000,
        Indirect = 0b00010000000000000000,
    }

    public class Mesh : RenderLoop.Mesh
    {
        public Mesh(DrawingFlags flags, Bounds bounds, Vector3 relativeOrigin, Mesh? relativeMesh, uint unknown1, Vector3[] relativeVertices, Vector3[] normals, Vector2[] textureCoords, Face[] faces, uint unknown2)
            : base(relativeOrigin, relativeVertices, textureCoords, normals, faces, relativeMesh)
        {
            this.Flags = flags;
            this.Unknown1 = unknown1;
            this.Unknown2 = unknown2;
        }

        public DrawingFlags Flags { get; }

        public uint Unknown1 { get; }

        public uint Unknown2 { get; }
    }
}
