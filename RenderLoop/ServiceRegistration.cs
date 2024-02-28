namespace RenderLoop
{
    using Microsoft.Extensions.DependencyInjection;
    using RenderLoop.SoftwareRenderer;
    using Silk.NET.Windowing;

    public class ServiceRegistration
    {
        public static void Register(IServiceCollection services)
        {
            services.AddTransient(_ => Window.Create(WindowOptions.Default));
            services.AddTransient<Display>();
            Input.ServiceRegistration.Register(services);
        }
    }
}
