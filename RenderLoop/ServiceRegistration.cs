namespace RenderLoop
{
    using Microsoft.Extensions.DependencyInjection;

    public class ServiceRegistration
    {
        public static void Register(IServiceCollection services)
        {
            Input.ServiceRegistration.Register(services);
        }
    }
}
