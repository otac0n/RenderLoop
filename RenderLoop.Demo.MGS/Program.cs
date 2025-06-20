// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand();

            Options.Attach(rootCommand);

            var modelCommand = new Command("model", "Display Models (MGS1)");
            ArchiveOptions.Attach(modelCommand);
            rootCommand.Add(modelCommand);

            var vehicleCommand = new Command("vehicle", "Display Vehicles (MGS1)");
            ArchiveOptions.Attach(vehicleCommand);
            rootCommand.Add(vehicleCommand);

            var codecCommand = new Command("codec", "Display Codec (MGS1)");
            ArchiveOptions.Attach(codecCommand);
            Conversation.LanguageModelOptions.Attach(codecCommand);
            Conversation.Voices.VoiceOptions.Attach(codecCommand);
            rootCommand.Add(codecCommand);

            var textureCommand = new Command("texture", "Display Textures (MGS2)");
            rootCommand.Add(textureCommand);

            var otaconCommand = new Command("otacon", "Display Otacon Assistant (MGS2)");
            Conversation.LanguageModelOptions.Attach(otaconCommand);
            Conversation.Voices.VoiceOptions.Attach(otaconCommand);
            rootCommand.Add(otaconCommand);

            static void InstallSharedConfiguration(InvocationContext context, IServiceCollection services)
            {
                ApplicationConfiguration.Initialize();
                Options.Bind(context, services);
                ServiceRegistration.Register(services);
            }

            modelCommand.SetHandler(
                async context =>
                {
                    var builder = Host.CreateDefaultBuilder(args);
                    builder.ConfigureServices(services =>
                    {
                        InstallSharedConfiguration(context, services);
                        ArchiveOptions.Bind(context, services);
                        services.AddHostedService<GameLoopApplication<ModelDisplay>>();
                    });

                    using var host = builder.Build();
                    await host.RunAsync().ConfigureAwait(true);
                });

            vehicleCommand.SetHandler(
                async context =>
                {
                    var builder = Host.CreateDefaultBuilder(args);
                    builder.ConfigureServices(services =>
                    {
                        InstallSharedConfiguration(context, services);
                        ArchiveOptions.Bind(context, services);
                        services.AddHostedService<GameLoopApplication<VehicleDisplay>>();
                    });

                    using var host = builder.Build();
                    await host.RunAsync().ConfigureAwait(true);
                });

            codecCommand.SetHandler(
                async context =>
                {
                    var builder = Host.CreateDefaultBuilder(args);
                    builder.ConfigureServices(services =>
                    {
                        InstallSharedConfiguration(context, services);
                        ArchiveOptions.Bind(context, services);
                        Conversation.LanguageModelOptions.Bind(context, services);
                        Conversation.Voices.VoiceOptions.Bind(context, services);
                    });

                    using var host = builder.Build();
                    await Task.Yield();
                    Application.Run(host.Services.GetService<Codec.CodecDisplay>()!);
                });

            textureCommand.SetHandler(
                async context =>
                {
                    var builder = Host.CreateDefaultBuilder(args);
                    builder.ConfigureServices(services =>
                    {
                        InstallSharedConfiguration(context, services);
                    });

                    using var host = builder.Build();
                    await Task.Yield();
                    Application.Run(host.Services.GetService<MGS2.TextureDisplay>()!);
                });

            otaconCommand.SetHandler(
                async context =>
                {
                    var builder = Host.CreateDefaultBuilder(args);
                    builder.ConfigureServices(services =>
                    {
                        InstallSharedConfiguration(context, services);
                        Conversation.LanguageModelOptions.Bind(context, services);
                        Conversation.Voices.VoiceOptions.Bind(context, services);
                    });

                    using var host = builder.Build();
                    await Task.Yield();
                    Application.Run(host.Services.GetService<MGS2.Otacon.OtaconDisplay>()!);
                });

            return await rootCommand.InvokeAsync(args).ConfigureAwait(true);
        }

        internal class Options
        {
            public static readonly Option<string> SteamAppsOption = new(
                name: "--steamApps",
                description: "The path to the steamapps folder that contains the games.",
                getDefaultValue: () => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Steam\steamapps\"))
            {
                IsRequired = true,
            };

            public required string SteamApps { get; set; }

            public static void Attach(Command command)
            {
                command.AddOption(SteamAppsOption);
            }

            public static void Bind(InvocationContext context, IServiceCollection services)
            {
                var options = new Options()
                {
                    SteamApps = context.ParseResult.GetValueForOption(SteamAppsOption)!,
                };

                services.AddSingleton(options);
            }
        }
    }
}
