// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS1
{
    public class Face(ushort textureId, uint[] vertexIndices, uint[] normalIndices, uint[] textureIndices) : RenderLoop.Face(vertexIndices, normalIndices, textureIndices)
    {
        public ushort TextureId { get; set; } = textureId;
    }
}
