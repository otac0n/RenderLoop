// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.SilkRenderer.DX
{
    using System;
    using System.Runtime.CompilerServices;
    using Microsoft.Extensions.Logging;
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D11;
    using Silk.NET.DXGI;
    using Silk.NET.Maths;
    using Silk.NET.Windowing;

    public sealed class DxWindow : IDisposable
    {
        private const int BufferCount = 2;
        private const Format FrameBufferFormat = Format.FormatB8G8R8A8Unorm;
        private readonly DXGI dxgi;
        private readonly D3D11 d3d;
        private ComPtr<ID3D11Device> device;
        private ComPtr<ID3D11DeviceContext> deviceContext;
        private ComPtr<IDXGISwapChain1> swapChain;
        private ComPtr<ID3D11Texture2D> depthBuffer;
        private ComPtr<ID3D11RenderTargetView> renderTargetView;
        private ComPtr<ID3D11DepthStencilView> depthStencilView;

        public unsafe DxWindow(IWindow window, CreateDeviceFlag createFlags = CreateDeviceFlag.None, ILogger? logger = null)
        {
            this.Window = window;
            this.dxgi = DXGI.GetApi(window);
            this.d3d = D3D11.GetApi(window);

            SilkMarshal.ThrowHResult(
                this.d3d.CreateDevice(
                    default(ComPtr<IDXGIAdapter>),
                    D3DDriverType.Hardware,
                    Software: default,
                    (uint)createFlags,
                    null,
                    0,
                    D3D11.SdkVersion,
                    ref this.device,
                    null,
                    ref this.deviceContext));

            if (OperatingSystem.IsWindows() && logger != null && (createFlags & CreateDeviceFlag.Debug) != 0)
            {
                _ = this.device.SetInfoQueueLogger(logger);
            }

            var rasterizerDesc = new RasterizerDesc
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Back,
                FrontCounterClockwise = false,
                DepthClipEnable = true,
            };
            ComPtr<ID3D11RasterizerState> rasterizerState = default;
            SilkMarshal.ThrowHResult(this.device.CreateRasterizerState(in rasterizerDesc, ref rasterizerState));
            this.deviceContext.RSSetState(rasterizerState);
            rasterizerState.Dispose();

            var swapChainDesc = new SwapChainDesc1
            {
                BufferCount = BufferCount,
                Format = FrameBufferFormat,
                BufferUsage = DXGI.UsageRenderTargetOutput,
                SwapEffect = SwapEffect.FlipDiscard,
                SampleDesc = new SampleDesc(1, 0),
            };

            using var factory = this.dxgi.CreateDXGIFactory<IDXGIFactory2>();

            SilkMarshal.ThrowHResult(
                factory.CreateSwapChainForHwnd(
                    this.device,
                    window.Native!.DXHandle!.Value,
                    in swapChainDesc,
                    null,
                    ref Unsafe.NullRef<IDXGIOutput>(),
                    ref this.swapChain));

            this.CreateFrameResources(window.FramebufferSize.X, window.FramebufferSize.Y);

            window.FramebufferResize += this.Window_FramebufferResize;
        }

        public ComPtr<ID3D11Device> Device => this.device;

        public ComPtr<ID3D11DeviceContext> DeviceContext => this.deviceContext;

        public ComPtr<IDXGISwapChain1> SwapChain => this.swapChain;

        public IWindow Window { get; }

        public unsafe void Clear(Span<float> backgroundColor, float depth)
        {
            using var frameBuffer = this.swapChain.GetBuffer<ID3D11Texture2D>(0);
            SilkMarshal.ThrowHResult(this.device.CreateRenderTargetView(frameBuffer, null, ref this.renderTargetView));
            this.deviceContext.OMSetRenderTargets(1, ref this.renderTargetView, this.depthStencilView);
            this.deviceContext.ClearRenderTargetView(this.renderTargetView, ref backgroundColor[0]);
            this.deviceContext.ClearDepthStencilView(this.depthStencilView, (uint)ClearFlag.Depth, depth, 0);
        }

        internal void Present()
        {
            this.swapChain.Present(1, 0);
            this.renderTargetView.Dispose();
            this.renderTargetView = default;
        }

        private void Window_FramebufferResize(Vector2D<int> newSize)
        {
            ComPtr<ID3D11RenderTargetView> emptyTarget = default;
            this.deviceContext.OMSetRenderTargets(1, ref emptyTarget, ref Unsafe.NullRef<ID3D11DepthStencilView>());
            this.renderTargetView.Dispose();
            this.renderTargetView = emptyTarget;
            this.depthStencilView.Dispose();
            this.depthStencilView = default;
            this.deviceContext.Flush();

            SilkMarshal.ThrowHResult(this.swapChain.ResizeBuffers(BufferCount, (uint)newSize.X, (uint)newSize.Y, FrameBufferFormat, 0));

            this.CreateFrameResources(newSize.X, newSize.Y);
        }

        private void CreateFrameResources(int width, int height) => this.CreateFrameResources((uint)width, (uint)height);

        private unsafe void CreateFrameResources(uint width, uint height)
        {
            var depthStencilDesc = new Texture2DDesc
            {
                Format = Format.FormatD32Float,
                Width = width,
                Height = height,
                ArraySize = 1,
                MipLevels = 1,
                BindFlags = (uint)BindFlag.DepthStencil,
                SampleDesc = new SampleDesc(1, 0),
            };
            SilkMarshal.ThrowHResult(this.device.CreateTexture2D(in depthStencilDesc, Unsafe.NullRef<SubresourceData>(), ref this.depthBuffer));

            var depthStencilViewDesc = new DepthStencilViewDesc
            {
                ViewDimension = DsvDimension.Texture2D,
                Format = depthStencilDesc.Format,
                Anonymous =
                {
                    Texture2D =
                    {
                        MipSlice = 0,
                    },
                },
            };
            SilkMarshal.ThrowHResult(this.device.CreateDepthStencilView(this.depthBuffer, in depthStencilViewDesc, ref this.depthStencilView));

            var viewport = new Viewport
            {
                TopLeftX = 0.0f,
                TopLeftY = 0.0f,
                Width = width,
                Height = height,
                MinDepth = 0.0f,
                MaxDepth = 1.0f,
            };
            this.deviceContext.RSSetViewports(1, in viewport);
        }

        public void Dispose()
        {
            this.Window.FramebufferResize -= this.Window_FramebufferResize;
            this.renderTargetView.Dispose();
            this.depthStencilView.Dispose();
            this.depthBuffer.Dispose();
            this.swapChain.Dispose();
            this.device.Dispose();
            this.deviceContext.Dispose();
            this.d3d.Dispose();
            this.dxgi.Dispose();
        }
    }
}
