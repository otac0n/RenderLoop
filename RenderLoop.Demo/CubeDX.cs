﻿namespace RenderLoop.Demo
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

    internal class CubeDX : GameLoop<CubeDX.AppState>
    {
        public record class AppState(double T);
        private readonly ILogger<CubeDX> logger;

        protected readonly IWindow display;
        protected DxWindow dx;
        protected readonly Camera Camera = new();

        private ConstantBuffer<Matrix4x4> cbuffer;
        protected ShaderHandle<(Vector3 position, Vector2 uv)> shader;

        /// <remarks>
        /// 0 -- 1<br/>
        /// |  / |<br/>
        /// | /  |<br/>
        /// 2 -- 3
        /// </remarks>
        protected static readonly Vector2[] UV = [
            new(0, 0),
            new(0, 1),
            new(1, 0),
            new(1, 1),
        ];

        protected static readonly Vector3[] Vertices = [
            new Vector3(-1, -1, +1) / 2, // L, F, T
            new Vector3(-1, +1, +1) / 2, // L, B, T
            new Vector3(+1, +1, +1) / 2, // R, B, T
            new Vector3(+1, -1, +1) / 2, // R, F, T
            new Vector3(-1, -1, -1) / 2, // L, F, B
            new Vector3(-1, +1, -1) / 2, // L, B, B
            new Vector3(+1, +1, -1) / 2, // R, B, B
            new Vector3(+1, -1, -1) / 2, // R, F, B
        ];

        /// <remarks>
        /// Same order as <see cref="UV"/>
        /// </remarks>
        protected static readonly uint[][] Shapes = [
            [0, 1, 3, 2], // TOP
            [5, 4, 6, 7], // BOTTOM
            [0, 3, 4, 7], // FRONT
            [2, 1, 6, 5], // BACK
            [1, 0, 5, 4], // LEFT
            [3, 2, 7, 6], // RIGHT
        ];

        protected readonly (Vector3 position, Vector2 uv)[][] shapes = Array.ConvertAll(Shapes, shape => shape.Select((i, j) => (Vertices[i], UV[j])).ToArray());

        public CubeDX([FromKeyedServices("Direct3D")] IWindow display, ILogger<CubeDX> logger)
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
                            : float4(0.6, 0.6, 0.6, 1.0);
                    }
                """);
        }

        protected sealed override void AdvanceFrame(ref AppState state, TimeSpan elapsed)
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