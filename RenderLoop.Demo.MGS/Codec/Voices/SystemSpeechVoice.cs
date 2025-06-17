// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.Codec.Voices
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Speech.Synthesis;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    internal partial class SystemSpeechVoice : Voice, IDisposable
    {
        private readonly SpeechSynthesizer synth = new();
        private readonly string voiceName;
        private readonly ILogger<SystemSpeechVoice> logger;

        [SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "This enumerates all cultures.")]
        public SystemSpeechVoice(ILogger<SystemSpeechVoice> logger, VoiceGender voiceGender, VoiceAge voiceAge, CultureInfo? voiceCulture)
        {
            this.logger = logger;

            if (voiceCulture?.Name == "uk-UA")
            {
                LogMessages.ReplacingCulture(this.logger, voiceCulture?.Name!, "ru-RU");
                voiceCulture = CultureInfo.GetCultureInfo("ru-RU"); // Closest accent available in Microsoft TTS.
            }

            var voice = (from v in this.synth.GetInstalledVoices()
                         orderby v.VoiceInfo.Gender == voiceGender descending,
                                 voiceCulture == null || v.VoiceInfo.Culture.LCID == voiceCulture.LCID descending,
                                 v.VoiceInfo.Age == voiceAge descending
                         select v).First();
            this.voiceName = voice.VoiceInfo.Name;
            LogMessages.SelectedVoice(this.logger, this.voiceName, voiceGender, voiceAge, voiceCulture?.Name);
            this.synth.SelectVoice(this.voiceName);
            this.synth.SpeakProgress += this.Synth_SpeakProgress;
            this.synth.VisemeReached += this.Synth_VisemeReached;
        }

        public void Dispose() => this.synth.Dispose();

        public override Task SayAsync(string text, CancellationToken cancel)
        {
            var tcs = new TaskCompletionSource();
            Prompt? prompt = null;
            CancellationTokenRegistration ctr = default;

            void FinishHandler(object? e, SpeakCompletedEventArgs a)
            {
                if (a.Prompt == prompt)
                {
                    LogMessages.SpeakingCompleted(this.logger, this.voiceName);
                    tcs.TrySetResult();
                    Dispose();
                }
            }

            void Dispose()
            {
                ctr.Dispose();
                this.synth.SpeakCompleted -= FinishHandler;
            }

            try
            {
                this.synth.SpeakCompleted += FinishHandler;

                LogMessages.SpeakingText(this.logger, this.voiceName, text);
                prompt = this.synth.SpeakAsync(text);

                ctr = cancel.Register(() =>
                {
                    if (prompt != null && !prompt.IsCompleted)
                    {
                        LogMessages.CancelingSpeaking(this.logger, this.voiceName);
                        this.synth.SpeakAsyncCancel(prompt);
                    }

                    tcs.TrySetCanceled();
                    Dispose();
                });
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                Dispose();
            }

            return tcs.Task;
        }

        private void Synth_SpeakProgress(object? sender, SpeakProgressEventArgs e)
        {
            this.InvokeIndexReached(e.Text, e.CharacterPosition, e.CharacterCount);
        }

        private void Synth_VisemeReached(object? sender, VisemeReachedEventArgs e)
        {
            this.InvokeMouthMoved((uint)e.Viseme);
        }

        private static partial class LogMessages
        {
            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Replacing culture '{replaced}' with phoenetic approximation '{replacement}'.")]
            public static partial void ReplacingCulture(ILogger logger, string replaced, string replacement);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "{voiceName}: Selected voice from Gender = '{voiceGender}', Age = '{voiceAge}', Culture = '{voiceCulture}'")]
            public static partial void SelectedVoice(ILogger logger, string voiceName, VoiceGender voiceGender, VoiceAge voiceAge, string? voiceCulture);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "{voiceName}: Canceling speaking...")]
            public static partial void CancelingSpeaking(ILogger logger, string voiceName);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "{voiceName}: Speaking text \"{text}\"")]
            public static partial void SpeakingText(ILogger logger, string voiceName, string text);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "{voiceName}: Speaking text completed.")]
            public static partial void SpeakingCompleted(ILogger logger, string voiceName);
        }
    }
}
