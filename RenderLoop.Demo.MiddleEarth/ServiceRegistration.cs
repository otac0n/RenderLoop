namespace RenderLoop.Demo.MiddleEarth
{
    using System.IO.Compression;
    using Microsoft.Extensions.DependencyInjection;
    using RenderLoop.SoftwareRenderer;

    internal class ServiceRegistration
    {
        internal static void Register(IServiceCollection services, Program.Options options)
        {
            RenderLoop.ServiceRegistration.Register(services);
            services.AddTransient<Display>();

            services.AddKeyedSingleton(options.File, (s, key) => ZipFile.OpenRead(key));

            services.AddTransient<FlyBy>();
        }
    }
}
