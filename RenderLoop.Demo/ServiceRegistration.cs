namespace RenderLoop.Demo
{
    using Microsoft.Extensions.DependencyInjection;
    using RenderLoop.SoftwareRenderer;

    internal class ServiceRegistration
    {
        internal static void Register(IServiceCollection services, Program.Options options)
        {
            RenderLoop.ServiceRegistration.Register(services);
            services.AddTransient<Display>();

            services.AddTransient<Cube>();
            services.AddTransient<CenterScreen>();
        }
    }
}
