﻿// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.SilkRenderer.DX
{
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D11;
    using Silk.NET.DXGI;

    public static class DynamicDraw
    {
        public static unsafe void PaintFrame(this DxWindow dx, Action paint)
        {
            dx.Clear([0.0f, 0.0f, 0.0f, 1.0f], 1.0f);
            paint();
            dx.Present();
        }

        public static unsafe void DrawStrip<TVertex>(this DxWindow dx, TVertex[] vertices, ShaderHandle<TVertex> shader)
            where TVertex : unmanaged =>
            dx.DrawPrimitives(D3DPrimitiveTopology.D3D10PrimitiveTopologyTrianglestrip, vertices, shader);

        public static unsafe void DrawStrip<TVertex>(this DxWindow dx, TVertex[] vertices, uint[] indices, ShaderHandle<TVertex> shader)
            where TVertex : unmanaged =>
            dx.DrawPrimitives(D3DPrimitiveTopology.D3D10PrimitiveTopologyTrianglestrip, vertices, indices, shader);

        public static unsafe void DrawStrip<TVertex>(this DxWindow dx, Buffer<TVertex> vertexBuffer, ShaderHandle<TVertex> shader)
            where TVertex : unmanaged =>
            dx.DrawPrimitives(D3DPrimitiveTopology.D3D10PrimitiveTopologyTrianglestrip, vertexBuffer, shader);

        public static unsafe void DrawStrip<TVertex>(this DxWindow dx, Buffer<TVertex> vertexBuffer, Buffer<uint> indexBuffer, ShaderHandle<TVertex> shader)
            where TVertex : unmanaged =>
            dx.DrawPrimitives(D3DPrimitiveTopology.D3D10PrimitiveTopologyTrianglestrip, vertexBuffer, indexBuffer, shader);

        public static unsafe void DrawTriangles<TVertex>(this DxWindow dx, TVertex[] vertices, ShaderHandle<TVertex> shader)
            where TVertex : unmanaged =>
            dx.DrawPrimitives(D3DPrimitiveTopology.D3D10PrimitiveTopologyTrianglelist, vertices, shader);

        public static unsafe void DrawTriangles<TVertex>(this DxWindow dx, TVertex[] vertices, uint[] indices, ShaderHandle<TVertex> shader)
            where TVertex : unmanaged =>
            dx.DrawPrimitives(D3DPrimitiveTopology.D3D10PrimitiveTopologyTrianglelist, vertices, indices, shader);

        public static unsafe void DrawTriangles<TVertex>(this DxWindow dx, Buffer<TVertex> vertexBuffer, ShaderHandle<TVertex> shader)
            where TVertex : unmanaged =>
            dx.DrawPrimitives(D3DPrimitiveTopology.D3D10PrimitiveTopologyTrianglelist, vertexBuffer, shader);

        public static unsafe void DrawTriangles<TVertex>(this DxWindow dx, Buffer<TVertex> vertexBuffer, Buffer<uint> indexBuffer, ShaderHandle<TVertex> shader)
            where TVertex : unmanaged =>
            dx.DrawPrimitives(D3DPrimitiveTopology.D3D10PrimitiveTopologyTrianglelist, vertexBuffer, indexBuffer, shader);

        public static void DrawPrimitives<TVertex>(this DxWindow dx, D3DPrimitiveTopology primitives, TVertex[] vertices, ShaderHandle<TVertex> shader)
            where TVertex : unmanaged
        {
            using var vertexBuffer = new Buffer<TVertex>(dx.Device, vertices, BindFlag.VertexBuffer);
            dx.DrawPrimitives(primitives, vertexBuffer, shader);
        }

        public static void DrawPrimitives<TVertex>(this DxWindow dx, D3DPrimitiveTopology primitives, TVertex[] vertices, uint[] indices, ShaderHandle<TVertex> shader)
            where TVertex : unmanaged
        {
            using var vertexBuffer = new Buffer<TVertex>(dx.Device, vertices, BindFlag.VertexBuffer);
            using var indexBuffer = new Buffer<uint>(dx.Device, indices, BindFlag.IndexBuffer);
            dx.DrawPrimitives(primitives, vertexBuffer, indexBuffer, shader);
        }

        public static unsafe void DrawPrimitives<TVertex>(this DxWindow dx, D3DPrimitiveTopology primitives, Buffer<TVertex> vertexBuffer, ShaderHandle<TVertex> shader)
            where TVertex : unmanaged
        {
            var vertexOffset = 0U;
            var vertexStride = (uint)sizeof(TVertex);

            shader.Bind(dx.DeviceContext);

            dx.DeviceContext.IASetPrimitiveTopology(primitives);
            dx.DeviceContext.IASetVertexBuffers(0, 1, vertexBuffer.Handle, in vertexStride, in vertexOffset);

            dx.DeviceContext.Draw(vertexBuffer.Length, 0);
        }

        public static unsafe void DrawPrimitives<TVertex>(this DxWindow dx, D3DPrimitiveTopology primitives, Buffer<TVertex> vertexBuffer, Buffer<uint> indexBuffer, ShaderHandle<TVertex> shader)
            where TVertex : unmanaged
        {
            var vertexOffset = 0U;
            var vertexStride = (uint)sizeof(TVertex);

            shader.Bind(dx.DeviceContext);

            dx.DeviceContext.IASetPrimitiveTopology(primitives);
            dx.DeviceContext.IASetVertexBuffers(0, 1, vertexBuffer.Handle, in vertexStride, in vertexOffset);
            dx.DeviceContext.IASetIndexBuffer(indexBuffer.Handle, Format.FormatR32Uint, 0);

            dx.DeviceContext.DrawIndexed(indexBuffer.Length, 0, 0);
        }
    }
}
