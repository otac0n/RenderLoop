// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.SilkRenderer.DX
{
    using System;
    using Microsoft.Extensions.Logging;
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D11;

    public static class LogExtensions
    {
#pragma warning disable CA1848
#pragma warning disable CA2254
        public static Task SetInfoQueueLogger(this ComPtr<ID3D11Device> device, ILogger logger)
        {
#if DEBUG
            unsafe void Callback(Message msg) => logger.Log(msg.Severity.ToLogLevel(), SilkMarshal.PtrToString((nint)msg.PDescription));
            return device.SetInfoQueueCallback(Callback);
#else
            return Task.CompletedTask;
#endif
        }
#pragma warning restore

        public static LogLevel ToLogLevel(this MessageSeverity msg) => msg switch
        {
            MessageSeverity.Corruption => LogLevel.Critical,
            MessageSeverity.Error => LogLevel.Error,
            MessageSeverity.Warning => LogLevel.Warning,
            MessageSeverity.Info => LogLevel.Information,
            MessageSeverity.Message => LogLevel.Trace,
            _ => throw new NotImplementedException(),
        };
    }
}
