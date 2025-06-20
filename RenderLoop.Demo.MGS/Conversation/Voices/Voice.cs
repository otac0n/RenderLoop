// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.Conversation.Voices
{
    using System;
    using System.Globalization;
    using System.Speech.Synthesis;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using static RenderLoop.Demo.MGS.MGS1.Codec.CharacterMetadata;

    internal abstract class Voice
    {
        public event EventHandler<MouthMovedEventArgs>? MouthMoved;

        public event EventHandler<IndexReachedEventArgs>? IndexReached;

        public static Voice GetVoice(IServiceProvider serviceProvider, string name)
        {
            var options = serviceProvider.GetRequiredService<VoiceOptions>();
            if (!string.IsNullOrWhiteSpace(options.SpeechKey) && !string.IsNullOrWhiteSpace(options.SpeechEndpoint) && AssignedAzureVoices.TryGetValue(name, out var assignedVoice))
            {
                return new AzureCognitiveVoice(options, serviceProvider.GetRequiredService<ILogger<AzureCognitiveVoice>>(), assignedVoice);
            }
            else if (VoiceHints.TryGetValue(name, out var voiceHints))
            {
                return new SystemSpeechVoice(serviceProvider.GetRequiredService<ILogger<SystemSpeechVoice>>(), voiceHints.Gender, voiceHints.Age, CultureInfo.GetCultureInfo(voiceHints.Culture));
            }

            return new SystemSpeechVoice(serviceProvider.GetRequiredService<ILogger<SystemSpeechVoice>>(), VoiceGender.NotSet, VoiceAge.NotSet, voiceCulture: null);
        }

        protected static string ApplyPhoneticReplacements(string text) => text
            .Replace("Otacon", "Awtacon", StringComparison.CurrentCultureIgnoreCase)
            .Replace("FOXDIE", "Fox-Die", StringComparison.CurrentCultureIgnoreCase)
            .Replace("REX", "Rex", StringComparison.CurrentCulture);

        protected abstract Task SayImplAsync(string text, CancellationToken cancel);

        public async Task<string> SayAsync(string text, CancellationToken cancel)
        {
            var index = 0;
            void IndexReached(object? sender, IndexReachedEventArgs args) => index = args.Index;
            try
            {
                this.IndexReached += IndexReached;
                await this.SayImplAsync(text, cancel).ConfigureAwait(false);
            }
            finally
            {
                this.IndexReached -= IndexReached;
                if (index != text.Length)
                {
                    text = text[..index].TrimEnd();
                    if (text is not "" && text[^1..] is not ("." or "?" or "!"))
                    {
                        text += "--";
                    }
                }
            }

            return text;
        }

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
