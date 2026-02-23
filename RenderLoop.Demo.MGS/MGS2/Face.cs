// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS2
{
    public class Face(ulong textureId, uint[] vertexIndices, uint[] normalIndices, uint[] textureIndices) : RenderLoop.Face(vertexIndices, normalIndices, textureIndices)
    {
        public ulong TextureId { get; set; } = textureId;
    }
}
