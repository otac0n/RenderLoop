// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.Codec.Voices
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Speech.Synthesis;
    using System.Threading.Tasks;

    internal class SystemSpeechVoice : Voice, IDisposable
    {
        private readonly SpeechSynthesizer synth = new();

        [SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "This enumerates all cultures.")]
        public SystemSpeechVoice(VoiceGender voiceGender, VoiceAge voiceAge, CultureInfo? voiceCulture)
        {
            if (voiceCulture?.Name == "uk-UA")
            {
                voiceCulture = CultureInfo.GetCultureInfo("ru-RU"); // Closest accent available in Microsoft TTS.
            }

            var voice = (from v in this.synth.GetInstalledVoices()
                         orderby v.VoiceInfo.Gender == voiceGender descending,
                                 voiceCulture == null || v.VoiceInfo.Culture.LCID == voiceCulture.LCID descending,
                                 v.VoiceInfo.Age == voiceAge descending
                         select v).First();
            this.synth.SelectVoice(voice.VoiceInfo.Name);
            this.synth.VisemeReached += this.Synth_VisemeReached;
        }

        public void Dispose() => this.synth.Dispose();

        public override Task SayAsync(string text) => Task.Factory.StartNew(() => this.synth.Speak(ApplyPhoneticReplacements(text)));

        private void Synth_VisemeReached(object? sender, VisemeReachedEventArgs e)
        {
            this.InvokeMouthMoved((uint)e.Viseme);
        }
    }
}
