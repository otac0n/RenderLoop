// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop
{
    using Microsoft.Extensions.DependencyInjection;
    using Silk.NET.Windowing;

    public class ServiceRegistration
    {
        public static void Register(IServiceCollection services)
        {
            services.AddTransient(_ => Window.Create(WindowOptions.Default));
            services.AddKeyedTransient("Direct3D", (_, _) => Window.Create(WindowOptions.Default with { API = GraphicsAPI.None }));
            SoftwareRenderer.ServiceRegistration.Register(services);
            Input.ServiceRegistration.Register(services);
        }
    }
}
