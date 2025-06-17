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

            rootCommand.AddGlobalOption(fileOption);
            rootCommand.AddGlobalOption(keyOption);

            var codecCommand = new Command("codec", "Display Codec");

            var speechEndpointOption = new Option<string?>(
                name: "--speechEndpoint",
                description: "The Azure Speech API to use for avatars.",
                getDefaultValue: () => Environment.GetEnvironmentVariable("SPEECH_ENDPOINT"));

            var speechKeyOption = new Option<string?>(
                name: "--speechKey",
                description: "The Azure Speech API key.",
                getDefaultValue: () => Environment.GetEnvironmentVariable("SPEECH_KEY"));

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

            codecCommand.AddOption(speechEndpointOption);
            codecCommand.AddOption(speechKeyOption);
            codecCommand.AddOption(lmEndpointOption);
            codecCommand.AddOption(languageModelOption);
            codecCommand.AddOption(lmCooldownOption);

            rootCommand.Add(codecCommand);

            void InstallSharedConfiguration(InvocationContext context, IServiceCollection services)
            {
                var options = new Options
                {
                    File = context.ParseResult.GetValueForOption(fileOption)!,
                    Key = context.ParseResult.GetValueForOption(keyOption)!,
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
                    var codecOptions = new Codec.CodecOptions
                    {
                        SpeechEndpoint = context.ParseResult.GetValueForOption(speechEndpointOption),
                        SpeechKey = context.ParseResult.GetValueForOption(speechKeyOption),
                        LMEndpoint = context.ParseResult.GetValueForOption(lmEndpointOption),
                        LanguageModel = context.ParseResult.GetValueForOption(languageModelOption),
                        LMCoolDown = context.ParseResult.GetValueForOption(lmCooldownOption),
                    };

                    var builder = Host.CreateDefaultBuilder(args);
                    builder.ConfigureServices(services =>
                    {
                        InstallSharedConfiguration(context, services);
                        services.AddSingleton(codecOptions);
                    });

                    using var host = builder.Build();
                    await Task.Yield();
                    Application.Run(host.Services.GetService<Codec.CodecDisplay>()!);
                });

            return await rootCommand.InvokeAsync(args).ConfigureAwait(true);
        }

        internal class Options
        {
            public required string File { get; set; }

            public required string Key { get; set; }
        }
    }
}
