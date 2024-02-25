﻿namespace RenderLoop.Demo.MGS
{
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
                services.AddSingleton(options);
                ServiceRegistration.Register(services, options);

                services.AddHostedService<GameLoopApplication<VehicleDisplay>>();
            });

            using var host = builder.Build();
            await host.RunAsync().ConfigureAwait(true);
            return 0;
        }

        internal class Options
        {
            [Option("file", Required = true, HelpText = "The path of the alldata.bin file.")]
            public required string File { get; set; }

            [Option("key", Required = true, HelpText = "The key to the alldata.bin file.")]
            public required string Key { get; set; }

            public static void PopulateDefaults(Options options)
            {
                if (options.File == null)
                {
                    // TODO: Get default path.
                }
            }
        }
    }
}