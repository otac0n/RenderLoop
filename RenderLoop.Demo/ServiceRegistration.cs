﻿namespace RenderLoop.Demo
{
    using Microsoft.Extensions.DependencyInjection;

    internal class ServiceRegistration
    {
        internal static void Register(IServiceCollection services, Program.Options options)
        {
            RenderLoop.ServiceRegistration.Register(services);

            services.AddTransient<CubeDX>();
            services.AddTransient<CubeGL>();
            services.AddTransient<Cube>();
            services.AddTransient<CenterScreen>();
        }
    }
}
