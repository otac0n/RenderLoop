namespace RenderLoop.SilkRenderer.DX
{
    using System.Runtime.CompilerServices;
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D11;
    using Silk.NET.DXGI;

    public static class DynamicDraw
    {
        public static unsafe void PaintFrame(this DxWindow dx, Action paint)
        {
            var backgroundColour = new[] { 0.0f, 0.0f, 0.0f, 1.0f };

            using var framebuffer = dx.SwapChain.GetBuffer<ID3D11Texture2D>(0);
            ComPtr<ID3D11RenderTargetView> renderTargetView = default;
            try
            {
                SilkMarshal.ThrowHResult(dx.Device.CreateRenderTargetView(framebuffer, null, ref renderTargetView));

                var size = dx.Window.FramebufferSize;
                var viewport = new Viewport(0, 0, size.X, size.Y, 0, 1);

                var dc = dx.DeviceContext;
                dc.RSSetViewports(1, in viewport);
                dc.OMSetRenderTargets(1, ref renderTargetView, ref Unsafe.NullRef<ID3D11DepthStencilView>());
                dc.ClearRenderTargetView(renderTargetView, ref backgroundColour[0]);

                paint();
                dx.SwapChain.Present(1, 0);
            }
            finally
            {
                renderTargetView.Dispose();
            }
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
