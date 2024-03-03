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
        private readonly DXGI dxgi;
        private readonly D3D11 d3d;
        private ComPtr<ID3D11Device> device;
        private ComPtr<ID3D11DeviceContext> deviceContext;
        private ComPtr<IDXGISwapChain1> swapChain;

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
                FrontCounterClockwise = false,
                CullMode = CullMode.Back,
                FillMode = FillMode.Solid,
                DepthClipEnable = true,
            };

            ComPtr<ID3D11RasterizerState> rasterizerState = default;
            SilkMarshal.ThrowHResult(
                this.device.CreateRasterizerState(in rasterizerDesc, ref rasterizerState));

            this.deviceContext.RSSetState(rasterizerState);
            rasterizerState.Dispose();

            var swapChainDesc = new SwapChainDesc1
            {
                BufferCount = 2,
                Format = Format.FormatB8G8R8A8Unorm,
                BufferUsage = DXGI.UsageRenderTargetOutput,
                SwapEffect = SwapEffect.FlipDiscard,
                SampleDesc = new SampleDesc(1, 0)
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

            window.FramebufferResize += this.Window_FramebufferResize;
        }

        private void Window_FramebufferResize(Vector2D<int> newSize)
        {
            SilkMarshal.ThrowHResult(
                this.swapChain.ResizeBuffers(0, (uint)newSize.X, (uint)newSize.Y, Format.FormatB8G8R8A8Unorm, 0));
        }

        public ComPtr<ID3D11Device> Device => this.device;

        public ComPtr<ID3D11DeviceContext> DeviceContext => this.deviceContext;

        public ComPtr<IDXGISwapChain1> SwapChain => this.swapChain;

        public IWindow Window { get; }

        public void Dispose()
        {
            this.Window.FramebufferResize -= this.Window_FramebufferResize;
            this.swapChain.Dispose();
            this.device.Dispose();
            this.deviceContext.Dispose();
            this.d3d.Dispose();
            this.dxgi.Dispose();
        }
    }
}
