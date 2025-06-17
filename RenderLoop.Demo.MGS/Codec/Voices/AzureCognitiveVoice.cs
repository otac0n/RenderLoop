// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.Codec.Voices
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.CognitiveServices.Speech;

    internal class AzureCognitiveVoice : Voice, IDisposable
    {
        private readonly SpeechSynthesizer synth;

        public AzureCognitiveVoice(CodecOptions options, string voiceName)
        {
            var speechConfig = SpeechConfig.FromEndpoint(new Uri(options.SpeechEndpoint!), options.SpeechKey);
            speechConfig.SpeechSynthesisVoiceName = voiceName;
            this.synth = new(speechConfig);
            this.synth.WordBoundary += this.Synth_WordBoundary;
            this.synth.VisemeReceived += this.Synth_VisemeReceived;
        }

        private void Synth_WordBoundary(object? sender, SpeechSynthesisWordBoundaryEventArgs e)
        {
            this.InvokeIndexReached(e.Text, (int)e.TextOffset, (int)e.WordLength);
        }

        public void Dispose() => this.synth.Dispose();

        public override Task SayAsync(string text) => this.synth.SpeakTextAsync(ApplyPhoneticReplacements(text));

        private void Synth_VisemeReceived(object? sender, SpeechSynthesisVisemeEventArgs e)
        {
            this.InvokeMouthMoved(e.VisemeId);
        }
    }
}
