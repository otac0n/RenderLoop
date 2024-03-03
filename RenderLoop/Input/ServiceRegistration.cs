// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.Input
{
    using DevDecoder.HIDDevices;
    using Microsoft.Extensions.DependencyInjection;

    internal class ServiceRegistration
    {
        internal static void Register(IServiceCollection services)
        {
            services.AddSingleton<Devices>();
            services.AddTransient<ControlChangeTracker>();
        }
    }
}
