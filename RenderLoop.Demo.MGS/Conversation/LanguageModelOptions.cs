// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.Conversation
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.IO;
    using Microsoft.Extensions.DependencyInjection;

    internal class LanguageModelOptions
    {
        public static readonly Option<Uri> LMEndpointOption = new(
            name: "--lmEndpoint",
            description: "The LM Studio API to use for conversation.",
            getDefaultValue: () => new Uri("http://localhost:5000"));

        public static readonly Option<string> LanguageModelOption = new(
            name: "--languageModel",
            description: "The language model to request for conversation.",
            getDefaultValue: () => "mradermacher/QwQ-LCoT-14B-Conversational-i1-GGUF")
        {
            IsRequired = true,
        };

        public static readonly Option<string> LMRepositoryPathOption = new(
            name: "--lmRepository",
            description: "The path that contains language models.",
            getDefaultValue: static () => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".cache\lm-studio\models\"));

        public required Uri? LMEndpoint { get; set; }

        public required string LanguageModel { get; set; }

        public required string? LMRepositoryPath { get; set; }

        public static void Attach(Command command)
        {
            command.AddOption(LMEndpointOption);
            command.AddOption(LanguageModelOption);
            command.AddOption(LMRepositoryPathOption);
        }

        public static void Bind(InvocationContext context, IServiceCollection services)
        {
            var options = new LanguageModelOptions
            {
                LMEndpoint = context.ParseResult.GetValueForOption(LMEndpointOption),
                LanguageModel = context.ParseResult.GetValueForOption(LanguageModelOption)!,
                LMRepositoryPath = context.ParseResult.GetValueForOption(LMRepositoryPathOption),
            };

            services.AddSingleton(options);
        }
    }
}
