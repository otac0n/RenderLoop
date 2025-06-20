// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.Conversation
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using Microsoft.Extensions.DependencyInjection;

    internal class LanguageModelOptions
    {
        public static readonly Option<string?> LMEndpointOption = new(
            name: "--lmEndpoint",
            description: "The LM Studio API to use for conversation.",
            getDefaultValue: () => "http://localhost:5000");

        public static readonly Option<string?> LanguageModelOption = new(
            name: "--languageModel",
            description: "The language model to request for conversation.",
            getDefaultValue: () => "mradermacher/QwQ-LCoT-14B-Conversational-i1-GGUF");

        public static readonly Option<TimeSpan> LMCooldownOption = new(
            name: "--lmCooldown",
            description: "The time to allow the LM to cooldown between requests.",
            getDefaultValue: () => TimeSpan.FromSeconds(5));

        public required string? LMEndpoint { get; set; }

        public required string? LanguageModel { get; set; }

        public required TimeSpan LMCoolDown { get; set; }

        public static void Attach(Command command)
        {
            command.AddOption(LMEndpointOption);
            command.AddOption(LanguageModelOption);
            command.AddOption(LMCooldownOption);
        }

        public static void Bind(InvocationContext context, IServiceCollection services)
        {
            var options = new LanguageModelOptions
            {
                LMEndpoint = context.ParseResult.GetValueForOption(LMEndpointOption),
                LanguageModel = context.ParseResult.GetValueForOption(LanguageModelOption),
                LMCoolDown = context.ParseResult.GetValueForOption(LMCooldownOption),
            };

            services.AddSingleton(options);
        }
    }
}
