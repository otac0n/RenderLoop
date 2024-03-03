// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.Demo
{
    using Microsoft.Extensions.DependencyInjection;

    internal class ServiceRegistration
    {
        internal static void Register(IServiceCollection services, Program.Options options)
        {
            RenderLoop.ServiceRegistration.Register(services);

            services.AddTransient<CubeDX>();
            services.AddTransient<CubeGL>();
            services.AddTransient<CubeSW>();
            services.AddTransient<CenterScreen>();
        }
    }
}
