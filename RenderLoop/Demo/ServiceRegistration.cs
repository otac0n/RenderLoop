namespace RenderLoop.Demo
{
    using Microsoft.Extensions.DependencyInjection;

    internal class ServiceRegistration
    {
        internal static void Register(IServiceCollection services)
        {
            services.AddInheritedTypes<GameLoop>(typeof(ServiceRegistration).Namespace, services.AddTransient);
        }
    }
}
