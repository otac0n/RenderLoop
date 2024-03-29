﻿// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.Demo
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
                ApplicationConfiguration.Initialize();

                services.AddSingleton(options);
                ServiceRegistration.Register(services, options);

                services.AddHostedService<GameLoopApplication<CenterScreen>>();
                services.AddHostedService<GameLoopApplication<CubeGL>>();
            });

            using var host = builder.Build();
            await host.RunAsync().ConfigureAwait(true);
            return 0;
        }

        internal class Options
        {
            public static void PopulateDefaults(Options options)
            {
            }
        }
    }
}
