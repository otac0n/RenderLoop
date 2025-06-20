// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS
{
    using Microsoft.Extensions.DependencyInjection;

    internal class ServiceRegistration
    {
        internal static void Register(IServiceCollection services)
        {
            RenderLoop.ServiceRegistration.Register(services);

            services.AddTransient<Codec.CodecDisplay>();
            services.AddTransient<MGS2.TextureDisplay>();
            services.AddTransient<MGS2.Otacon.OtaconDisplay>();
            services.AddTransient<ModelDisplay>();
            services.AddTransient<VehicleDisplay>();
        }
    }
}
