namespace RenderLoop.Demo
{
    using Microsoft.Extensions.DependencyInjection;
    using RenderLoop.SoftwareRenderer;

    internal class ServiceRegistration
    {
        internal static void Register(IServiceCollection services)
        {
            services.AddInheritedTypes<Display>(typeof(ServiceRegistration).Namespace, services.AddTransient);
        }
    }
}
