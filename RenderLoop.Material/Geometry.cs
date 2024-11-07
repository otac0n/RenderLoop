// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.Material
{
    using System.Numerics;

    public static class Geometry
    {
        /// <remarks>
        /// 0 -- 2<br/>
        /// |  / |<br/>
        /// | /  |<br/>
        /// 1 -- 3
        /// </remarks>
        public static readonly Vector2[] UV = [
            new(0, 0),
            new(0, 1),
            new(1, 0),
            new(1, 1),
        ];

        public static readonly Vector3[] Vertices = [
            new Vector3(-1, -1, +1) / 2, // L, F, T
            new Vector3(-1, +1, +1) / 2, // L, B, T
            new Vector3(+1, +1, +1) / 2, // R, B, T
            new Vector3(+1, -1, +1) / 2, // R, F, T
            new Vector3(-1, -1, -1) / 2, // L, F, B
            new Vector3(-1, +1, -1) / 2, // L, B, B
            new Vector3(+1, +1, -1) / 2, // R, B, B
            new Vector3(+1, -1, -1) / 2, // R, F, B
        ];

        /// <remarks>
        /// Same order as <see cref="UV"/>.
        /// </remarks>
        public static readonly uint[][] Shapes = [
            [0, 1, 3, 2], // TOP
            [5, 4, 6, 7], // BOTTOM
            [0, 3, 4, 7], // FRONT
            [2, 1, 6, 5], // BACK
            [1, 0, 5, 4], // LEFT
            [3, 2, 7, 6], // RIGHT
        ];
    }
}
