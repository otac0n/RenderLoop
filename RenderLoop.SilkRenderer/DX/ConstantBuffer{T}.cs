// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.SilkRenderer.DX
{
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D11;

    public sealed class ConstantBuffer<T> : IDisposable
        where T : unmanaged
    {
        private ComPtr<ID3D11Buffer> handle;

        public unsafe ConstantBuffer(ComPtr<ID3D11Device> device, T constantData = default)
        {
            var bufferDesc = new BufferDesc
            {
                ByteWidth = (uint)sizeof(T),
                Usage = Usage.Dynamic,
                BindFlags = (uint)BindFlag.ConstantBuffer,
                CPUAccessFlags = (uint)CpuAccessFlag.Write,
            };

            var subresourceData = new SubresourceData
            {
                PSysMem = &constantData,
            };

            SilkMarshal.ThrowHResult(device.CreateBuffer(in bufferDesc, in subresourceData, ref this.handle));
        }

        public unsafe void Bind(ComPtr<ID3D11DeviceContext> deviceContext)
        {
            deviceContext.VSSetConstantBuffers(0, 1, this.handle);
        }

        public void Update(ComPtr<ID3D11DeviceContext> deviceContext, T constantData) =>
            this.UpdateFrom(deviceContext, ref constantData);

        public unsafe void UpdateFrom(ComPtr<ID3D11DeviceContext> deviceContext, ref T constantData)
        {
            var mappedResource = new MappedSubresource();
            deviceContext.Map((ID3D11Resource*)this.handle.Handle, 0, Map.WriteDiscard, 0, ref mappedResource);
            try
            {
                fixed (void* c = &constantData)
                {
                    Buffer.MemoryCopy(c, mappedResource.PData, sizeof(T), sizeof(T));
                }
            }
            finally
            {
                deviceContext.Unmap((ID3D11Resource*)this.handle.Handle, 0);
            }
        }

        public ComPtr<ID3D11Buffer> Handle => this.handle;

        public void Dispose()
        {
            this.handle.Dispose();
            this.handle = default;
        }
    }
}
