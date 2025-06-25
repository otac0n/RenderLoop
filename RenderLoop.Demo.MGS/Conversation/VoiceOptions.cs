// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.Conversation
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using Microsoft.Extensions.DependencyInjection;

    internal class VoiceOptions
    {
        public static readonly Option<Uri> SpeechEndpointOption = new(
            name: "--speechEndpoint",
            description: "The Azure Speech API to use for avatars.",
            getDefaultValue: () => Uri.TryCreate(Environment.GetEnvironmentVariable("SPEECH_ENDPOINT"), UriKind.Absolute, out var uri) ? uri : null!)
        {
            IsRequired = true,
        };

        public static readonly Option<string> SpeechKeyOption = new(
            name: "--speechKey",
            description: "The Azure Speech API key.",
            getDefaultValue: () => Environment.GetEnvironmentVariable("SPEECH_KEY")!)
        {
            IsRequired = true,
        };

        public required Uri SpeechEndpoint { get; set; }

        public required string SpeechKey { get; set; }

        public static void Attach(Command command)
        {
            command.AddOption(SpeechEndpointOption);
            command.AddOption(SpeechKeyOption);
        }

        public static void Bind(InvocationContext context, IServiceCollection services)
        {
            var options = new VoiceOptions
            {
                SpeechEndpoint = context.ParseResult.GetValueForOption(SpeechEndpointOption)!,
                SpeechKey = context.ParseResult.GetValueForOption(SpeechKeyOption)!,
            };

            services.AddSingleton(options);
        }
    }
}
