// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.Conversation.Voices
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.Extensions.Logging;

    internal partial class AzureCognitiveVoice : Voice, IDisposable
    {
        private readonly ILogger<AzureCognitiveVoice> logger;
        private readonly string voiceName;
        private readonly SpeechSynthesizer synth;
        private Task lastCancelTask;

        public AzureCognitiveVoice(VoiceOptions options, ILogger<AzureCognitiveVoice> logger, string voiceName)
        {
            this.logger = logger;
            this.voiceName = voiceName;
            var speechConfig = SpeechConfig.FromEndpoint(new Uri(options.SpeechEndpoint!), options.SpeechKey);
            speechConfig.SpeechSynthesisVoiceName = voiceName;
            this.synth = new(speechConfig);
            this.synth.WordBoundary += this.Synth_WordBoundary;
            this.synth.VisemeReceived += this.Synth_VisemeReceived;
        }

        public void Dispose() => this.synth.Dispose();

        protected override async Task SayImplAsync(string text, CancellationToken cancel)
        {
            if (this.lastCancelTask is Task cancelTask && !cancelTask.IsCompleted)
            {
                LogMessages.WaitingOnCancelTask(this.logger, this.voiceName);
                await cancelTask.ConfigureAwait(false);
            }

            using var ctr = cancel.Register(() =>
            {
                LogMessages.CancelingSpeaking(this.logger, this.voiceName);
                this.lastCancelTask = this.synth.StopSpeakingAsync();
            });

            LogMessages.SpeakingText(this.logger, this.voiceName, text);

            try
            {
                await this.synth.SpeakTextAsync(ApplyPhoneticReplacements(text)).ConfigureAwait(false);
            }
            finally
            {
                if (!cancel.IsCancellationRequested)
                {
                    this.InvokeIndexReached(text, text.Length, 0);
                }

                this.InvokeMouthMoved(0);
            }

            LogMessages.SpeakingCompleted(this.logger, this.voiceName);
        }

        private void Synth_VisemeReceived(object? sender, SpeechSynthesisVisemeEventArgs e)
        {
            this.InvokeMouthMoved(e.VisemeId);
        }

        private void Synth_WordBoundary(object? sender, SpeechSynthesisWordBoundaryEventArgs e)
        {
            this.InvokeIndexReached(e.Text, (int)e.TextOffset, (int)e.WordLength);
        }

        private static partial class LogMessages
        {
            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "{voiceName}: Waiting on cancel task...")]
            public static partial void WaitingOnCancelTask(ILogger logger, string voiceName);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "{voiceName}: Canceling speaking...")]
            public static partial void CancelingSpeaking(ILogger logger, string voiceName);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "{voiceName}: Speaking text \"{text}\"")]
            public static partial void SpeakingText(ILogger logger, string voiceName, string text);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "{voiceName}: Speaking text completed.")]
            public static partial void SpeakingCompleted(ILogger logger, string voiceName);
        }
    }
}
