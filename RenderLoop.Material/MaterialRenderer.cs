// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.Material
{
    using System;
    using System.Linq;
    using System.Numerics;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using RenderLoop.SilkRenderer.DX;
    using Silk.NET.Direct3D11;
    using Silk.NET.DXGI;
    using Silk.NET.Windowing;
    using static Geometry;

    internal class MaterialRenderer : GameLoop<MaterialRenderer.AppState>
    {
        public record class AppState(double T);

        protected Camera Camera { get; } = new();

        private readonly ILogger<MaterialRenderer> logger;

        protected readonly IWindow display;
        protected DxWindow dx;

        private ConstantBuffer<Matrix4x4> cbuffer;
        protected ShaderHandle<(Vector3 position, Vector2 uv)> shader;

        protected readonly (Vector3 position, Vector2 uv)[][] shapes = Array.ConvertAll(Shapes, shape => shape.Select((i, j) => (Vertices[i], UV[j])).ToArray());

        public MaterialRenderer([FromKeyedServices("Direct3D")] IWindow display, ILogger<MaterialRenderer> logger)
            : base(display, new AppState(0))
        {
            this.display = display;
            this.logger = logger;
        }

        protected override void Initialize()
        {
            this.dx = new DxWindow(this.display, CreateDeviceFlag.Debug, this.logger);

            this.cbuffer = new ConstantBuffer<Matrix4x4>(this.dx.Device, Matrix4x4.Identity);
            this.cbuffer.Bind(this.dx.DeviceContext);

            this.shader = new ShaderHandle<(Vector3, Vector2)>(
                this.dx.Device,
                [
                    ("POS", 0, Format.FormatR32G32B32Float),
                    ("TEXCOORD", 0, Format.FormatR32G32Float),
                ],
                "vs_main",
                "ps_main",
                () => """
                    #pragma pack_matrix(row_major)
                    cbuffer camera : register(b0)
                    {
                        float4x4 camera_matrix;
                    };

                    struct vs_in {
                        float3 position : POS;
                        float2 textureCoords : TEXCOORD0;
                    };

                    struct vs_out {
                        float4 position_clip : SV_POSITION;
                        float2 textureCoords : TEXCOORD0;
                    };

                    vs_out vs_main(vs_in input) {
                        vs_out output;
                        output.position_clip = mul(float4(input.position, 1.0), camera_matrix);
                        output.textureCoords = input.textureCoords;
                        return output;
                    }

                    float4 ps_main(vs_out input) : SV_TARGET {
                        return ((int)(input.textureCoords.x * 4) + (int)(input.textureCoords.y * 4)) % 2 == 0
                            ? float4(1.0, 1.0, 1.0, 1.0)
                            : float4(0.5, 0.5, 0.5, 1.0);
                    }
                """);
        }

        protected override void AdvanceFrame(ref AppState state, TimeSpan elapsed)
        {
            var dist = 2;

            var a = Math.Tau * state.T / 3;
            var (x, y) = Math.SinCos(a);
            var z = Math.Sin(a / 3);
            var p = new Vector3((float)(dist * x), (float)(dist * y), (float)(dist / 2 * z));

            this.Camera.Position = p;
            this.Camera.Direction = -p;

            state = state with
            {
                T = state.T + elapsed.TotalSeconds,
            };
        }

        protected override void DrawScene(AppState state, TimeSpan elapsed)
        {
            this.dx.PaintFrame(() =>
            {
                this.Camera.Width = this.display.FramebufferSize.X;
                this.Camera.Height = this.display.FramebufferSize.Y;

                this.cbuffer.Update(this.dx.DeviceContext, this.Camera.Matrix);

                foreach (var shape in this.shapes)
                {
                    this.dx.DrawStrip(shape, this.shader);
                }
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.cbuffer.Dispose();
                this.shader.Dispose();
                this.dx.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
