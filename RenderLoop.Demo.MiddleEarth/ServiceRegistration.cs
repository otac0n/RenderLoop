namespace RenderLoop.Demo.MiddleEarth
{
    using System.IO.Compression;
    using Microsoft.Extensions.DependencyInjection;

    internal class ServiceRegistration
    {
        internal static void Register(IServiceCollection services, Program.Options options)
        {
            RenderLoop.ServiceRegistration.Register(services);

            services.AddKeyedSingleton(options.File, (s, key) => ZipFile.OpenRead(key));

            services.AddTransient<FlyBy>();
        }
    }
}
