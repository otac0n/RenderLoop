// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.Codec.Voices
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Speech.Synthesis;
    using System.Threading.Tasks;

    internal abstract class Voice
    {
        private static readonly Dictionary<string, string> AssignedAzureVoices = new()
        {
            { "Solid Snake", "en-US-DerekMultilingualNeural" },
            { "Roy Campbell", "en-US-LewisMultilingualNeural" },
            ////{ "Naomi Hunter", "en-US-LunaNeural" },
            { "Mei Ling", "en-US-AmberNeural" },
            { "Hal Emmerich", "en-US-TonyNeural" },
            { "Liquid Snake", "en-US-AndrewMultilingualNeural" },
            ////{ "Nastasha Romanenko", "en-US-CoraNeural" },
            { "Meryl Silverburgh", "en-US-AvaNeural" },
            { "Sniper Wolf", "en-US-NancyNeural" },
            ////{ "Jim Houseman", "en-US-DavisNeural" },
        };

        private static readonly Dictionary<string, (VoiceGender Gender, VoiceAge Age, string Culture)> VoiceHints = new()
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
        };

        public event EventHandler<MouthMovedEventArgs>? MouthMoved;

        public event EventHandler<IndexReachedEventArgs>? IndexReached;

        public static Voice GetVoice(CodecOptions options, string name)
        {
            if (!string.IsNullOrWhiteSpace(options.SpeechKey) && !string.IsNullOrWhiteSpace(options.SpeechEndpoint) && AssignedAzureVoices.TryGetValue(name, out var assignedVoice))
            {
                return new AzureCognitiveVoice(options, assignedVoice);
            }
            else if (VoiceHints.TryGetValue(name, out var voiceHints))
            {
                return new SystemSpeechVoice(voiceHints.Gender, voiceHints.Age, CultureInfo.GetCultureInfo(voiceHints.Culture));
            }

            return new SystemSpeechVoice(VoiceGender.NotSet, VoiceAge.NotSet, voiceCulture: null);
        }

        protected static string ApplyPhoneticReplacements(string text) => text
            .Replace("Otacon", "Awtacon", StringComparison.CurrentCultureIgnoreCase)
            .Replace("FOXDIE", "Fox-Die", StringComparison.CurrentCultureIgnoreCase)
            .Replace("REX", "Rex", StringComparison.CurrentCulture);

        public abstract Task SayAsync(string text);

        protected void InvokeMouthMoved(uint visemeId)
        {
            this.MouthMoved?.Invoke(this, new MouthMovedEventArgs(visemeId));
        }

        protected void InvokeIndexReached(string text, int index, int length)
        {
            this.IndexReached?.Invoke(this, new IndexReachedEventArgs(text, index, length));
        }

        public class MouthMovedEventArgs(uint visemeId) : EventArgs
        {
            public uint VisemeId { get; } = visemeId;
        }

        public class IndexReachedEventArgs(string text, int index, int length) : EventArgs
        {
            public string Text { get; } = text;

            public int Index { get; } = index;

            public int Length { get; } = length;
        }
    }
}
