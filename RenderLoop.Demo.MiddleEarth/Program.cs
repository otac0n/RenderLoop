namespace RenderLoop.Demo.MiddleEarth
{
    using System;
    using System.Threading.Tasks;
    using CommandLine;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

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

            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                ApplicationConfiguration.Initialize();

                services.AddSingleton(options);
                ServiceRegistration.Register(services, options);

                services.AddHostedService<GameLoopApplication<FlyBy>>();
            });

            using var host = builder.Build();
            await host.RunAsync().ConfigureAwait(true);
            return 0;
        }

        internal class Options
        {
            [Option("path", Required = false, HelpText = "The path of the Middle-earth_TW_Map.zip file.")]
            public required string File { get; set; }

            public static void PopulateDefaults(Options options)
            {
                options.File ??= Environment.ExpandEnvironmentVariables("%userprofile%/Downloads/Middle-earth_TW_Map.zip");
            }
        }
    }
}
