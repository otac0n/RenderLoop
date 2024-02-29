namespace RenderLoop.SilkRenderer.DX
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D.Compilers;
    using Silk.NET.Direct3D11;
    using Silk.NET.DXGI;

    public sealed class ShaderHandle<TVertex> : IDisposable
    {
        private readonly ComPtr<ID3D11VertexShader> vertexShader;
        private readonly ComPtr<ID3D11PixelShader> pixelShader;
        private readonly ComPtr<ID3D11InputLayout> inputLayout;

        public unsafe ShaderHandle(ComPtr<ID3D11Device> device, (string Name, uint Index, Format Format)[] inputFields, string vertexFunctionName, string fragmentFunctionName, Func<string> getShaderSource)
        {
            using var compiler = D3DCompiler.GetApi();

            var shaderSource = getShaderSource();
            var shaderBytes = Encoding.ASCII.GetBytes(shaderSource);

            ComPtr<ID3D10Blob> vertexCode = default;
            ComPtr<ID3D10Blob> vertexErrors = default;
            HResult hr = compiler.Compile(
                in shaderBytes[0],
                (nuint)shaderBytes.Length,
                nameof(shaderSource),
                null,
                ref Unsafe.NullRef<ID3DInclude>(),
                vertexFunctionName,
                "vs_5_0",
                0,
                0,
                ref vertexCode,
                ref vertexErrors);

            if (hr.IsFailure)
            {
                if (vertexErrors.Handle is not null)
                {
                    throw new InvalidProgramException(SilkMarshal.PtrToString((nint)vertexErrors.GetBufferPointer()));
                }

                hr.Throw();
            }

            ComPtr<ID3D10Blob> pixelCode = default;
            ComPtr<ID3D10Blob> pixelErrors = default;
            hr = compiler.Compile(
                in shaderBytes[0],
                (nuint)shaderBytes.Length,
                nameof(shaderSource),
                null,
                ref Unsafe.NullRef<ID3DInclude>(),
                fragmentFunctionName,
                "ps_5_0",
                0,
                0,
                ref pixelCode,
                ref pixelErrors);

            if (hr.IsFailure)
            {
                if (pixelErrors.Handle is not null)
                {
                    throw new InvalidProgramException(SilkMarshal.PtrToString((nint)pixelErrors.GetBufferPointer()));
                }

                hr.Throw();
            }

            SilkMarshal.ThrowHResult(
                device.CreateVertexShader(
                    vertexCode.GetBufferPointer(),
                    vertexCode.GetBufferSize(),
                    ref Unsafe.NullRef<ID3D11ClassLinkage>(),
                    ref this.vertexShader));

            SilkMarshal.ThrowHResult(
                device.CreatePixelShader(
                    pixelCode.GetBufferPointer(),
                    pixelCode.GetBufferSize(),
                    ref Unsafe.NullRef<ID3D11ClassLinkage>(),
                    ref this.pixelShader));

            this.inputLayout = CreateInputLayout(device, vertexCode, inputFields);

            vertexCode.Dispose();
            vertexErrors.Dispose();
            pixelCode.Dispose();
            pixelErrors.Dispose();
        }

        public void Bind(ComPtr<ID3D11DeviceContext> deviceContext)
        {
            deviceContext.IASetInputLayout(this.inputLayout);
            deviceContext.VSSetShader(this.vertexShader, ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);
            deviceContext.PSSetShader(this.pixelShader, ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);
        }

        public void Dispose()
        {
            this.vertexShader.Dispose();
            this.pixelShader.Dispose();
            this.inputLayout.Dispose();
            GC.SuppressFinalize(this);
        }

        private static unsafe ComPtr<ID3D11InputLayout> CreateInputLayout(ComPtr<ID3D11Device> device, ComPtr<ID3D10Blob> program, (string Name, uint Index, Format Format)[] inputFields)
        {
            const uint D3D11_APPEND_ALIGNED_ELEMENT = uint.MaxValue;

            ComPtr<ID3D11InputLayout> inputLayout = default;

            var inputElements = new InputElementDesc[inputFields.Length];

            void CreateElementsAndLayout(int element)
            {
                if (element == inputElements.Length)
                {
                    SilkMarshal.ThrowHResult(
                        device.CreateInputLayout(
                            in inputElements[0],
                            (uint)inputElements.Length,
                            program.GetBufferPointer(),
                            program.GetBufferSize(),
                            ref inputLayout));
                }
                else
                {
                    fixed (byte* name = SilkMarshal.StringToMemory(inputFields[element].Name))
                    {
                        inputElements[element] = new InputElementDesc
                        {
                            SemanticName = name,
                            SemanticIndex = inputFields[element].Index,
                            Format = inputFields[element].Format,
                            InputSlot = 0,
                            AlignedByteOffset = D3D11_APPEND_ALIGNED_ELEMENT,
                            InputSlotClass = InputClassification.PerVertexData,
                            InstanceDataStepRate = 0
                        };

                        CreateElementsAndLayout(element + 1);
                    }
                }
            }

            CreateElementsAndLayout(0);

            return inputLayout;
        }
    }
}
