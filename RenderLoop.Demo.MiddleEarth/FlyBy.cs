﻿// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.Demo.MiddleEarth
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO.Compression;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using DevDecoder.HIDDevices.Usages;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using RenderLoop.Input;
    using RenderLoop.SilkRenderer.DX;
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D11;
    using Silk.NET.DXGI;
    using Silk.NET.Windowing;

    internal partial class FlyBy : GameLoop
    {
        private readonly IWindow display;
        protected DxWindow dx;
        private readonly ControlChangeTracker controlChangeTracker;
        private readonly ILogger<FlyBy> logger;
        private readonly ZipArchive archive;
        private readonly Camera Camera = new();
        private Task loading;
        private ConstantBuffer<Matrix4x4> cbuffer;
        protected ShaderHandle<(Vector3, Vector3)> shader;
        private Buffer<(Vector3, Vector3)> vertexBuffer;
        private Buffer<uint> indexBuffer;
        private ComPtr<ID3D11Texture2D> albedo;
        private ComPtr<ID3D11ShaderResourceView> albedoResourceView;
        private ComPtr<ID3D11SamplerState> textureSampler;
        private static readonly Size BakeSize = new(8192, 8192);
        private static readonly Size TextureSize = new(8128, 5764);

        public FlyBy([FromKeyedServices("Direct3D")] IWindow display, ControlChangeTracker controlChangeTracker, Program.Options options, IServiceProvider serviceProvider, ILogger<FlyBy> logger)
            : base(display)
        {
            this.display = display;
            this.controlChangeTracker = controlChangeTracker;
            this.logger = logger;
            this.archive = serviceProvider.GetRequiredKeyedService<ZipArchive>(options.File);
            this.display.FramebufferResize += newSize =>
            {
                this.Camera.Width = newSize.X;
                this.Camera.Height = newSize.Y;
            };
        }

        protected override unsafe void Initialize()
        {
            this.dx = new DxWindow(this.display, CreateDeviceFlag.Debug, this.logger);

            this.cbuffer = new ConstantBuffer<Matrix4x4>(this.dx.Device, Matrix4x4.Identity);
            this.cbuffer.Bind(this.dx.DeviceContext);

            this.shader = new ShaderHandle<(Vector3, Vector3)>(
                this.dx.Device,
                [
                    ("POS", 0, Format.FormatR32G32B32Float),
                    ("NORMAL", 0, Format.FormatR32G32B32Float),
                ],
                "vs_main",
                "ps_main",
                () => """
                    #pragma pack_matrix(row_major)
                    Texture2D albedo: register(t0);

                    SamplerState AlbedoSampler
                    {
                        Filter = MIN_MAG_MIP_LINEAR;
                        AddressU = Border;
                        AddressV = Border;
                    };

                    cbuffer camera : register(b0)
                    {
                        float4x4 camera_matrix;
                    };

                    struct vs_in {
                        float3 position : POS;
                        float3 normal : NORMAL0;
                    };

                    struct vs_out {
                        float4 position_clip : SV_POSITION;
                        float3 normal : NORMAL0;
                        float2 textureCoords : TEXCOORD0;
                    };

                    vs_out vs_main(vs_in input) {
                        vs_out output;
                        output.position_clip = mul(float4(input.position, 1.0), camera_matrix);
                        output.normal = input.normal;
                        float2 tx = float2(input.position.x, input.position.y * 8128.0 / 5764) / 8192;
                        output.textureCoords = float2(1 - tx.x, tx.y + (1 - 8128.0 / 5764) / 2);
                        return output;
                    }

                    float4 ps_main(vs_out input) : SV_TARGET {
                        float3 sun = float3(1.0, 1.0, 0.9);
                        float3 sunDir = normalize(float3(1, 1, 1));
                        float3 ambient = float3(0.1, 0.1, 0.2);
                        float3 diffuse = max(dot(normalize(input.normal), sunDir), 0.0) * sun;
                        float3 color = albedo.Sample(AlbedoSampler, input.textureCoords);
                        return float4((ambient + diffuse) * color, 1);
                    }
                """);

            // Create a sampler.
            var samplerDesc = new SamplerDesc
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Border,
                AddressV = TextureAddressMode.Border,
                AddressW = TextureAddressMode.Clamp,
                MipLODBias = 0,
                MaxAnisotropy = 1,
                MinLOD = float.MinValue,
                MaxLOD = float.MaxValue,
            };
            samplerDesc.BorderColor[0] = 1.0f;
            samplerDesc.BorderColor[1] = 1.0f;
            samplerDesc.BorderColor[2] = 1.0f;
            samplerDesc.BorderColor[3] = 1.0f;

            SilkMarshal.ThrowHResult(this.dx.Device.CreateSamplerState(in samplerDesc, ref this.textureSampler));
            this.dx.DeviceContext.PSSetSamplers(0, 1, this.textureSampler);

            Bitmap LoadImage(string path)
            {
                LogMessages.LoadingImage(this.logger, path);
                return new(this.archive.GetEntry(path)!.Open());
            }

            this.loading = Task.Factory.StartNew(() =>
            {
                (Vector3 Position, Vector3 Normal)[] vertices;

                using (var heightBmp = LoadImage("Raw_Bakes/Final Height.png"))
                using (var normalBmp = LoadImage("Raw_Bakes/Normal Map.png"))
                {
                    var i = 0;

                    LogMessages.BuildingVertices(this.logger);
                    vertices = new (Vector3, Vector3)[BakeSize.Width * BakeSize.Height];
                    var hBmp = heightBmp.LockBits(new Rectangle(Point.Empty, BakeSize), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    var nBmp = normalBmp.LockBits(new Rectangle(Point.Empty, BakeSize), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    try
                    {
                        var single = new int[1];
                        for (var y = 0; y < hBmp.Height; y++)
                        {
                            for (var x = 0; x < hBmp.Width; x++)
                            {
                                Marshal.Copy(hBmp.Scan0 + y * hBmp.Stride + x * sizeof(int), single, 0, single.Length);
                                vertices[i].Position = new Vector3(hBmp.Width - 1 - x, y, single[0] & 0xFF);

                                Marshal.Copy(nBmp.Scan0 + y * nBmp.Stride + x * sizeof(int), single, 0, single.Length);
                                vertices[i].Normal = new Vector3(
                                    ((single[0] & 0xFF0000) >> 16) / 255.0f * 2 - 1,
                                    ((single[0] & 0xFF00) >> 8) / 255.0f * 2 - 1,
                                    ((single[0] & 0xFF) >> 0) / 255.0f * 2 - 1);

                                i++;
                            }
                        }
                    }
                    finally
                    {
                        heightBmp.UnlockBits(hBmp);
                        normalBmp.UnlockBits(nBmp);
                    }

                    LogMessages.VerticesDone(this.logger, vertices.LongLength);
                }

                uint GetIndex(int x, int y) => (uint)(y * BakeSize.Width + x);

                LogMessages.BuildingIndices(this.logger);
                var indices = new uint[(BakeSize.Width - 1) * (BakeSize.Height - 1) * 6];
                for (int y = 0, i = 0; y < BakeSize.Height - 1; y++)
                {
                    var topLeft = GetIndex(0, y);
                    var bottomLeft = GetIndex(0, y + 1);

                    for (var x = 1; x < BakeSize.Width; x++)
                    {
                        var topRight = GetIndex(x, y);
                        var bottomRight = GetIndex(x, y + 1);

                        indices[i++] = topLeft;
                        indices[i++] = topRight;
                        indices[i++] = bottomLeft;
                        indices[i++] = topRight;
                        indices[i++] = bottomRight;
                        indices[i++] = bottomLeft;

                        topLeft = topRight;
                        bottomLeft = bottomRight;
                    }
                }

                LogMessages.IndicesDone(this.logger, indices.LongLength);

                this.vertexBuffer = new Buffer<(Vector3, Vector3)>(this.dx.Device, vertices, BindFlag.VertexBuffer);
                this.indexBuffer = new Buffer<uint>(this.dx.Device, indices, BindFlag.IndexBuffer);

                using var albedoBmp = LoadImage("Textured/ME_Terrain_albedo.png");

                LogMessages.InstallingTexture(this.logger);
                var bmp = albedoBmp.LockBits(new Rectangle(Point.Empty, albedoBmp.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    var textureDesc = new Texture2DDesc
                    {
                        Width = (uint)bmp.Width,
                        Height = (uint)bmp.Height,
                        Format = Format.FormatB8G8R8A8Unorm,
                        MipLevels = 1,
                        BindFlags = (uint)BindFlag.ShaderResource,
                        Usage = Usage.Default,
                        CPUAccessFlags = 0,
                        MiscFlags = (uint)ResourceMiscFlag.None,
                        SampleDesc = new SampleDesc(1, 0),
                        ArraySize = 1,
                    };

                    SilkMarshal.ThrowHResult(this.dx.Device.CreateTexture2D(in textureDesc, Unsafe.NullRef<SubresourceData>(), ref this.albedo));

                    var subresourceData = new SubresourceData
                    {
                        PSysMem = (void*)bmp.Scan0,
                        SysMemPitch = (uint)bmp.Stride,
                        SysMemSlicePitch = (uint)(bmp.Stride * bmp.Height),
                    };

                    SilkMarshal.ThrowHResult(this.dx.Device.CreateTexture2D(in textureDesc, in subresourceData, ref this.albedo));

                    var srvDesc = new ShaderResourceViewDesc
                    {
                        Format = textureDesc.Format,
                        ViewDimension = D3DSrvDimension.D3DSrvDimensionTexture2D,
                        Anonymous = new ShaderResourceViewDescUnion
                        {
                            Texture2D =
                            {
                                MostDetailedMip = 0,
                                MipLevels = 1,
                            },
                        },
                    };

                    SilkMarshal.ThrowHResult(this.dx.Device.CreateShaderResourceView(this.albedo, in srvDesc, ref this.albedoResourceView));

                    this.dx.DeviceContext.PSSetShaderResources(0, 1, ref this.albedoResourceView);
                }
                finally
                {
                    albedoBmp.UnlockBits(bmp);
                }

                LogMessages.LoadComplete(this.logger);
            });

            this.Camera.Position = new Vector3(BakeSize.Width / 4, 2 * BakeSize.Height / 3, BakeSize.Width / 12);
            this.Camera.Up = new Vector3(0, 0, 1);
            this.Camera.Direction = new Vector3(BakeSize.Width / 2, 0, 0) - this.Camera.Position;
            this.Camera.FarPlane = Math.Max(BakeSize.Width, BakeSize.Height);
        }

        protected sealed override void AdvanceFrame(TimeSpan elapsed)
        {
            var moveVector = Vector2.Zero;
            var right = 0.0f;
            var up = 0.0f;

            static float DeadZone(float v) => InputShapes.DeadZone(0.1f, 1.0f, v);

            var bindings = new Bindings<Action<float>>();

            bindings.BindCurrent(
                [(c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Usages.Any(u => u == (uint)GenericDesktopPage.X), InputShapes.Signed)],
                v => moveVector.X += v);
            bindings.BindCurrent(
                [(c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Usages.Any(u => u == (uint)GenericDesktopPage.Y), InputShapes.Signed)],
                v => moveVector.Y += v);
            bindings.BindCurrent(
                [(c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Usages.Any(u => u == (uint)GenericDesktopPage.Ry), InputShapes.Signed)],
                v => up -= v);
            bindings.BindCurrent(
                [(c => c.Device.Name == "Controller (Xbox One For Windows)" && c.Usages.Any(u => u == (uint)GenericDesktopPage.Rx), InputShapes.Signed)],
                v => right -= v);

            this.controlChangeTracker.ProcessChanges(bindings);

            var moveScale = DeadZone(moveVector.Length());
            moveVector *= moveScale;
            if (moveVector != Vector2.Zero)
            {
                this.Camera.Position += (this.Camera.Right * moveVector.X - this.Camera.Direction * moveVector.Y) * (float)(elapsed.TotalSeconds * BakeSize.Width / 10);
            }

            var xyScale = DeadZone(right);
            right *= xyScale;
            if (right != 0)
            {
                var (sin, cos) = Math.SinCos(right * elapsed.TotalSeconds / 3 * Math.Tau);
                var v = this.Camera.Direction;
                var k = this.Camera.Up;
                this.Camera.Direction = v * (float)cos + Vector3.Cross(k, v) * (float)sin + k * Vector3.Dot(k, v) * (float)(1 - cos);
            }

            var zScale = DeadZone(up);
            up *= zScale;
            if (up != 0)
            {
                var (sin, cos) = Math.SinCos(up * elapsed.TotalSeconds / 3 * Math.Tau);
                var v = this.Camera.Direction;
                var k = this.Camera.Right;
                this.Camera.Direction = v * (float)cos + Vector3.Cross(k, v) * (float)sin + k * Vector3.Dot(k, v) * (float)(1 - cos);
            }
        }

        protected override void DrawScene(TimeSpan elapsed)
        {
            this.dx.PaintFrame(() =>
            {
                this.cbuffer.Update(this.dx.DeviceContext, this.Camera.Matrix);

                if (this.vertexBuffer != null && this.indexBuffer != null)
                {
                    this.dx.DrawTriangles(this.vertexBuffer, this.indexBuffer, this.shader);
                }
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.textureSampler.Dispose();
                this.albedoResourceView.Dispose();
                this.albedo.Dispose();
                this.vertexBuffer?.Dispose();
                this.indexBuffer?.Dispose();
                this.shader.Dispose();
                this.dx.Dispose();
            }

            base.Dispose(disposing);
        }

        private static partial class LogMessages
        {
            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Loading '{ImagePath}'...")]
            public static partial void LoadingImage(ILogger logger, string imagePath);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Building vertices...")]
            public static partial void BuildingVertices(ILogger logger);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Building indices...")]
            public static partial void BuildingIndices(ILogger logger);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Done. ({Vertices} vertices)")]
            public static partial void VerticesDone(ILogger logger, long vertices);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Done. ({Indices} indices)")]
            public static partial void IndicesDone(ILogger logger, long indices);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Installing texture...")]
            public static partial void InstallingTexture(ILogger logger);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Load Complete.")]
            public static partial void LoadComplete(ILogger logger);
        }
    }
}
