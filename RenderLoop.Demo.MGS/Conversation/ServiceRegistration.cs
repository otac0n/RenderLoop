// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.Conversation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Globalization;
    using System.Net.Http;
    using System.Speech.Synthesis;
    using ConversationModel;
    using ConversationModel.Backends;
    using ConversationModel.Voices;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    internal static class ServiceRegistration
    {
        public static readonly ImmutableDictionary<string, string> AssignedAzureVoices = new Dictionary<string, string>()
        {
            { "Solid Snake", "en-US-DerekMultilingualNeural" },
            { "Roy Campbell", "en-US-LewisMultilingualNeural" },
            { "Naomi Hunter", "en-GB-BellaNeural" },
            { "Mei Ling", "en-US-AmberNeural" },
            { "Hal Emmerich", "en-US-TonyNeural" },
            { "Liquid Snake", "en-GB-RyanNeural" }, // "en-US-AndrewMultilingualNeural" - for miller
            { "Nastasha Romanenko", "uk-UA-PolinaNeural" },
            { "Meryl Silverburgh", "en-US-AvaNeural" },
            { "Sniper Wolf", "en-US-NancyNeural" },
            { "Jim Houseman", "en-US-AdamMultilingualNeural" },
        }.ToImmutableDictionary();

        public static readonly ImmutableDictionary<string, (VoiceGender Gender, VoiceAge Age, string Culture)> VoiceHints = new Dictionary<string, (VoiceGender Gender, VoiceAge Age, string Culture)>()
        {
            { "Solid Snake", (VoiceGender.Male, VoiceAge.Adult, "en-US") },
            { "Roy Campbell", (VoiceGender.Male, VoiceAge.Senior, "en-US") },
            { "Naomi Hunter", (VoiceGender.Female, VoiceAge.Adult, "en-GB") },
            { "Mei Ling", (VoiceGender.Female, VoiceAge.Teen, "ja-JP") },
            { "Hal Emmerich", (VoiceGender.Male, VoiceAge.Teen, "en-US") },
            { "Liquid Snake", (VoiceGender.Male, VoiceAge.Adult, "en-GB") },
            { "Nastasha Romanenko", (VoiceGender.Female, VoiceAge.Adult, "uk-UA") },
            { "Meryl Silverburgh", (VoiceGender.Female, VoiceAge.Teen, "en-US") },
            { "Sniper Wolf", (VoiceGender.Female, VoiceAge.Adult, "ar-IQ") },
            { "Jim Houseman", (VoiceGender.Male, VoiceAge.Senior, "en-US") },
            { "Gray Fox", (VoiceGender.Male, VoiceAge.Adult, "en-US") },
        }.ToImmutableDictionary();

        internal static void Register(IServiceCollection services)
        {
            services.AddHttpClient<IBackend, HttpBackend>();
            services.AddTransient<IBackend, HttpBackend>(provider =>
            {
                var lmOptions = provider.GetRequiredService<LanguageModelOptions>();
                return new HttpBackend(
                    provider.GetRequiredService<HttpClient>(),
                    lmOptions.LMEndpoint,
                    lmOptions.LanguageModel,
                    provider.GetService<ILogger<HttpBackend>>());
            });

            services.AddSingleton(
                new PhoeneticReplacer(
                    new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase)
                    {
                        { "Otacon", "Awtacon" },
                        { "FOXDIE", "Fox-Die" },
                        { "REX", "Rex" },
                    }));

            services.AddKeyedTransient<Voice, string>(static (provider, key) =>
            {
                var options = provider.GetRequiredService<VoiceOptions>();
                var phoeneticReplacements = provider.GetService<PhoeneticReplacer>();
                if (options.SpeechEndpoint != null && !string.IsNullOrWhiteSpace(options.SpeechKey) && AssignedAzureVoices.TryGetValue(key, out var assignedVoice))
                {
                    return new AzureCognitiveVoice(options.SpeechEndpoint, options.SpeechKey, assignedVoice, phoeneticReplacements, provider.GetRequiredService<ILogger<AzureCognitiveVoice>>());
                }
                else if (VoiceHints.TryGetValue(key, out var voiceHints))
                {
                    return new SystemSpeechVoice(voiceHints.Gender, voiceHints.Age, CultureInfo.GetCultureInfo(voiceHints.Culture), phoeneticReplacements, provider.GetRequiredService<ILogger<SystemSpeechVoice>>());
                }

                return new SystemSpeechVoice(VoiceGender.NotSet, VoiceAge.NotSet, voiceCulture: null, phoeneticReplacements, provider.GetRequiredService<ILogger<SystemSpeechVoice>>());
            });
        }
    }
}
