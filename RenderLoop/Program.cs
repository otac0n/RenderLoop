namespace RenderLoop
{
    using System.Threading.Tasks;
    using CommandLine;
    using DiscUtils.Complete;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using RenderLoop.MGS;

    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static async Task<int> Main(string[] args)
        {
            Options? options = null;
            Parser.Default
                .ParseArguments<Options>(args)
                .WithParsed(o => Options.PopulateDefaults(options = o));

            if (options == null)
            {
                return 1;
            }

            SetupHelper.SetupComplete();

            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(options);

                services.AddHostedService<RenderLoopApplication>();
            });

            using var host = builder.Build();
            await host.RunAsync();
            return 0;
        }

        internal class Options
        {
        }
    }
}
