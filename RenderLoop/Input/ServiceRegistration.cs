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
