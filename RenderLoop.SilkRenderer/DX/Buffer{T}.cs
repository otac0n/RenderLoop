namespace RenderLoop.SilkRenderer.DX
{
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D11;

    public sealed class Buffer<T> : IDisposable
        where T : unmanaged
    {
        private ComPtr<ID3D11Buffer> handle;

        public unsafe Buffer(ComPtr<ID3D11Device> device, T[] source, BindFlag bindFlag, Usage usage = Usage.Default, CpuAccessFlag accessFlags = CpuAccessFlag.None)
        {
            this.Length = (uint)source.Length;

            var bufferDesc = new BufferDesc
            {
                ByteWidth = (uint)(source.Length * sizeof(T)),
                Usage = usage,
                BindFlags = (uint)bindFlag,
                CPUAccessFlags = (uint)accessFlags,
            };

            fixed (T* bufferData = source)
            {
                var subresourceData = new SubresourceData
                {
                    PSysMem = bufferData,
                };

                SilkMarshal.ThrowHResult(device.CreateBuffer(in bufferDesc, in subresourceData, ref this.handle));
            }
        }

        public uint Length { get; }

        public ComPtr<ID3D11Buffer> Handle => this.handle;

        public void Update(ComPtr<ID3D11DeviceContext> deviceContext, T[] source) =>
            this.UpdateFrom(deviceContext, ref source);

        public unsafe void UpdateFrom(ComPtr<ID3D11DeviceContext> deviceContext, ref T[] source)
        {
            var mappedResource = new MappedSubresource();
            deviceContext.Map((ID3D11Resource*)this.handle.Handle, 0, Map.WriteDiscard, 0, ref mappedResource);
            try
            {
                fixed (void* c = source)
                {
                    Buffer.MemoryCopy(c, mappedResource.PData, this.Length * sizeof(T), source.Length * sizeof(T));
                }
            }
            finally
            {
                deviceContext.Unmap((ID3D11Resource*)this.handle.Handle, 0);
            }
        }

        public void Dispose()
        {
            this.handle.Dispose();
            this.handle = default;
        }
    }
}
