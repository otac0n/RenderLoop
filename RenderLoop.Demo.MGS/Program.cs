// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    internal static partial class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand();

            Options.Attach(rootCommand);

            var mgs1 = new Command("mgs1", "MGS1 Related Featuers");
            MGS1.ArchiveOptions.Attach(mgs1);
            rootCommand.AddCommand(mgs1);

            var mgs2 = new Command("mgs2", "MGS1 Related Featuers");
            rootCommand.AddCommand(mgs2);

            var textureCommand1 = new Command("texture", "Display Textures (MGS1)");
            textureCommand1.AddAlias("textures");
            mgs1.Add(textureCommand1);

            var modelCommand = new Command("model", "Display Models (MGS1)");
            modelCommand.AddAlias("models");
            mgs1.Add(modelCommand);

            var vehicleCommand = new Command("vehicle", "Display Vehicles (MGS1)");
            vehicleCommand.AddAlias("vehicles");
            mgs1.Add(vehicleCommand);

            var codecCommand = new Command("codec", "Display Codec (MGS1)");
            Conversation.LanguageModelOptions.Attach(codecCommand);
            Conversation.VoiceOptions.Attach(codecCommand);
            mgs1.Add(codecCommand);

            var textureCommand2 = new Command("texture", "Display Textures (MGS2)");
            textureCommand2.AddAlias("textures");
            mgs2.Add(textureCommand2);

            var otaconCommand = new Command("otacon", "Display Otacon Assistant (MGS2)");
            Conversation.LanguageModelOptions.Attach(otaconCommand);
            Conversation.VoiceOptions.Attach(otaconCommand);
            mgs2.Add(otaconCommand);

            static void InstallSharedConfiguration(InvocationContext context, IServiceCollection services)
            {
                ApplicationConfiguration.Initialize();
                Options.Bind(context, services);
                ServiceRegistration.Register(services);
            }

            textureCommand1.SetHandler(
                async context =>
                {
                    var builder = Host.CreateDefaultBuilder(args);
                    builder.ConfigureServices(services =>
                    {
                        InstallSharedConfiguration(context, services);
                        MGS1.ArchiveOptions.Bind(context, services);
                    });

                    using var host = builder.Build();
                    await Task.Yield();
                    Application.Run(host.Services.GetService<MGS1.TextureDisplay>()!);
                });

            modelCommand.SetHandler(
                async context =>
                {
                    var builder = Host.CreateDefaultBuilder(args);
                    builder.ConfigureServices(services =>
                    {
                        InstallSharedConfiguration(context, services);
                        MGS1.ArchiveOptions.Bind(context, services);
                        services.AddHostedService<GameLoopApplication<MGS1.ModelDisplay>>();
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
                        MGS1.ArchiveOptions.Bind(context, services);
                        services.AddHostedService<GameLoopApplication<MGS1.VehicleDisplay>>();
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
                        MGS1.ArchiveOptions.Bind(context, services);
                        Conversation.LanguageModelOptions.Bind(context, services);
                        Conversation.VoiceOptions.Bind(context, services);
                        Conversation.ServiceRegistration.Register(services);
                    });

                    using var host = builder.Build();
                    await Task.Yield();
                    Application.Run(host.Services.GetService<MGS1.Codec.CodecDisplay>()!);
                });

            textureCommand2.SetHandler(
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
                        Conversation.VoiceOptions.Bind(context, services);
                        Conversation.ServiceRegistration.Register(services);
                    });

                    using var host = builder.Build();
                    await Task.Yield();
                    Application.Run(host.Services.GetService<MGS2.Otacon.OtaconDisplay>()!);
                });

            return await rootCommand.InvokeAsync(args).ConfigureAwait(true);
        }

        internal partial class Options
        {
            public static readonly Option<string> SteamAppsOption = new(
                name: "--steamApps",
                description: "The path to the steamapps folder that contains the games.",
                getDefaultValue: GetDefaultSteamAppsPath)
            {
                IsRequired = true,
            };

            public required string SteamApps { get; set; }

            public static void Attach(Command command)
            {
                command.AddGlobalOption(SteamAppsOption);
            }

            public static void Bind(InvocationContext context, IServiceCollection services)
            {
                var options = new Options()
                {
                    SteamApps = context.ParseResult.GetValueForOption(SteamAppsOption)!,
                };

                services.AddSingleton(options);
            }

            [GeneratedRegex(@"""path""\s+""(?<escaped_path>([^\""]|\[\""])+)""[^{}]+""apps""[\r\n\s]+{[^}]+""(?<found_app_id>21316[345]0)""\s+""\d+""")]
            private static partial Regex GetPathFinderRegex();

            [GeneratedRegex(@"\\(.)")]
            private static partial Regex GetEscapeRegex();

            private static string GetDefaultSteamAppsPath()
            {
                var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Steam\steamapps\");
                try
                {
                    var library = File.ReadAllText(Path.Combine(defaultPath, "libraryfolders.vdf"));
                    var match = GetPathFinderRegex().Match(library);
                    if (match.Success)
                    {
                        return Path.Combine(GetEscapeRegex().Replace(match.Groups["escaped_path"].Value, "$1"), "steamapps");
                    }
                }
                catch
                {
                }

                return defaultPath;
            }
        }
    }
}
