// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
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

            var fileOption = new Option<string>(
                name: "--file",
                description: "The path of the alldata.bin file.");
            fileOption.IsRequired = true;

            var keyOption = new Option<string>(
                name: "--key",
                description: "The key to the alldata.bin file.");
            keyOption.IsRequired = true;

            var lmEndpointOption = new Option<string?>(
                name: "--lmEndpoint",
                description: "The LM Studio API to use for conversation.",
                getDefaultValue: () => "http://localhost:5000");

            var languageModelOption = new Option<string?>(
                name: "--languageModel",
                description: "The language model to request for conversation.",
                getDefaultValue: () => "mradermacher/QwQ-LCoT-14B-Conversational-i1-GGUF");

            var lmCooldownOption = new Option<TimeSpan>(
                name: "--lmCooldown",
                description: "The time to allow the LM to cooldown between requests.",
                getDefaultValue: () => TimeSpan.FromSeconds(5));

            var speechEndpointOption = new Option<string?>(
                name: "--speechEndpoint",
                description: "The Azure Speech API to use for avatars.",
                getDefaultValue: () => Environment.GetEnvironmentVariable("SPEECH_ENDPOINT"));

            var speechKeyOption = new Option<string?>(
                name: "--speechKey",
                description: "The Azure Speech API key.",
                getDefaultValue: () => Environment.GetEnvironmentVariable("SPEECH_KEY"));

            rootCommand.AddGlobalOption(fileOption);
            rootCommand.AddGlobalOption(keyOption);
            rootCommand.AddOption(lmEndpointOption);
            rootCommand.AddOption(languageModelOption);
            rootCommand.AddOption(lmCooldownOption);
            rootCommand.AddOption(speechEndpointOption);
            rootCommand.AddOption(speechKeyOption);

            var codecCommand = new Command("codec", "Display Codec");
            rootCommand.Add(codecCommand);

            var textureCommand = new Command("texture", "Display Textures (MGS2)");
            rootCommand.Add(textureCommand);

            var otaconCommand = new Command("otacon", "Display Otacon Assistant");
            rootCommand.Add(otaconCommand);

            void InstallSharedConfiguration(InvocationContext context, IServiceCollection services)
            {
                var options = new Options
                {
                    File = context.ParseResult.GetValueForOption(fileOption)!,
                    Key = context.ParseResult.GetValueForOption(keyOption)!,
                    LMEndpoint = context.ParseResult.GetValueForOption(lmEndpointOption),
                    LanguageModel = context.ParseResult.GetValueForOption(languageModelOption),
                    LMCoolDown = context.ParseResult.GetValueForOption(lmCooldownOption),
                };

                ApplicationConfiguration.Initialize();
                services.AddSingleton(options);
                ServiceRegistration.Register(services, options);
            }

            rootCommand.SetHandler(
                async context =>
                {
                    var builder = Host.CreateDefaultBuilder(args);
                    builder.ConfigureServices(services =>
                    {
                        InstallSharedConfiguration(context, services);
                        services.AddHostedService<GameLoopApplication<VehicleDisplay>>();
                    });

                    using var host = builder.Build();
                    await host.RunAsync().ConfigureAwait(true);
                });

            codecCommand.SetHandler(
                async context =>
                {
                    var voiceOptions = new Conversation.VoiceOptions
                    {
                        SpeechEndpoint = context.ParseResult.GetValueForOption(speechEndpointOption),
                        SpeechKey = context.ParseResult.GetValueForOption(speechKeyOption),
                    };

                    var builder = Host.CreateDefaultBuilder(args);
                    builder.ConfigureServices(services =>
                    {
                        InstallSharedConfiguration(context, services);
                        services.AddSingleton(voiceOptions);
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
                    var voiceOptions = new Conversation.VoiceOptions
                    {
                        SpeechEndpoint = context.ParseResult.GetValueForOption(speechEndpointOption),
                        SpeechKey = context.ParseResult.GetValueForOption(speechKeyOption),
                    };

                    var builder = Host.CreateDefaultBuilder(args);
                    builder.ConfigureServices(services =>
                    {
                        InstallSharedConfiguration(context, services);
                        services.AddSingleton(voiceOptions);
                    });

                    using var host = builder.Build();
                    await Task.Yield();
                    Application.Run(host.Services.GetService<MGS2.Otacon.OtaconDisplay>()!);
                });

            return await rootCommand.InvokeAsync(args).ConfigureAwait(true);
        }

        internal class Options
        {
            public required string File { get; set; }

            public required string Key { get; set; }

            public required string? LMEndpoint { get; set; }

            public required string? LanguageModel { get; set; }

            public required TimeSpan LMCoolDown { get; set; }
        }
    }
}
