// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.SoftwareRenderer
{
    using Microsoft.Extensions.DependencyInjection;

    public class ServiceRegistration
    {
        public static void Register(IServiceCollection services)
        {
            services.AddTransient<CooperativeIdleApplicationContext>();
            services.AddTransient<Display>();
        }
    }
}
