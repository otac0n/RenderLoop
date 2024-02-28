namespace RenderLoop
{
    using Microsoft.Extensions.DependencyInjection;
    using RenderLoop.SoftwareRenderer;

    public class ServiceRegistration
    {
        public static void Register(IServiceCollection services)
        {
            services.AddTransient<Display>();
            Input.ServiceRegistration.Register(services);
        }
    }
}
