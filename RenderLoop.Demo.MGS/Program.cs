// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Forms;
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

                services.AddTransient<CodecDisplay>();
                services.AddHostedService<GameLoopApplication<VehicleDisplay>>();
            });

            using var host = builder.Build();

            if (options.Display == "codec")
            {
                await Task.Yield();
                Application.Run(host.Services.GetService<CodecDisplay>()!);
            }
            else
            {
                await host.RunAsync().ConfigureAwait(true);
            }

            return 0;
        }

        internal class Options
        {
            [Option("file", Required = true, HelpText = "The path of the alldata.bin file.")]
            public required string File { get; set; }

            [Option("key", Required = true, HelpText = "The key to the alldata.bin file.")]
            public required string Key { get; set; }

            [Option("display", Required = false, HelpText = "The display to show.")]
            public string? Display { get; set; }

            [Option("speechEndpoint", Required = false, HelpText = "The Azure Speech API to use for avatars.")]
            public string? SpeechEndpoint { get; set; }

            [Option("speechKey", Required = false, HelpText = "The Azure Speech API key.")]
            public string? SpeechKey { get; set; }

            public static void PopulateDefaults(Options options)
            {
                if (options.File == null)
                {
                    // TODO: Get default path.
                }

                options.Display ??= "model";

                options.SpeechEndpoint ??= Environment.GetEnvironmentVariable("SPEECH_ENDPOINT");

                options.SpeechKey ??= Environment.GetEnvironmentVariable("SPEECH_KEY");
            }
        }
    }
}
