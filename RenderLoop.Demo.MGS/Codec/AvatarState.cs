// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.Codec
{
    using System;
    using System.Threading.Tasks;
    using RenderLoop.Demo.MGS.Codec.Voices;

    internal sealed class AvatarState : IDisposable
    {
        private Voice voice;
        private int eyeState;
        private DateTime? lastBlinkTime;
        private uint lastViseme;

        public AvatarState(CodecOptions options, string voiceName)
        {
            this.voice = Voice.GetVoice(options, voiceName);
            this.voice.MouthMoved += this.Voice_MouthMoved;
        }

        public event EventHandler<EventArgs>? Updated;

        public double Attention { get; set; } = 0.5;

        public string? Eyes => this.eyeState == 0 ? null : this.eyeState % 2 == 0 ? "eyes-blink" : "eyes-droop";

        public string? Mouth => this.lastViseme switch
        {
            1 or 2 or 3 or 4 or 5 or 6 or 15 or 16 => "mouth-a",
            8 or 9 or 10 or 11 or 12 or 13 or 14 or 17 or 20 => "mouth-e",
            _ => null,
        };

        public double Volume => this.lastViseme switch
        {
            12 => 0.3,
            7 or 8 => 0.4,
            1 or 2 or 3 or 4 or 5 or 9 or 13 or 14 => 0.5,
            6 or 10 or 11 or 15 => 0.6,
            16 or 17 or 18 or 19 => 0.7,
            20 => 0.9,
            21 => 1.0,
            _ => 0,
        };

        public async Task SayAsync(string text)
        {
            await this.voice.SayAsync(text).ConfigureAwait(true);
        }

        public void Update()
        {
            var now = DateTime.Now;
            var updated = false;

            if (this.eyeState == 0)
            {
                if (BlinkBehavior.StartAutomaticBlink(now, this.lastBlinkTime, this.Attention))
                {
                    this.eyeState = 1;
                    updated = true;
                }
            }
            else
            {
                this.eyeState = (this.eyeState + 1) % 4;
                this.lastBlinkTime = now;
                updated = true;
            }

            if (updated)
            {
                this.Updated?.Invoke(this, new());
            }
        }

        public void Dispose()
        {
            (this.voice as IDisposable)?.Dispose();
        }

        private void Voice_MouthMoved(object? sender, Voice.MouthMovedEventArgs e)
        {
            var shape = this.Mouth;
            this.lastViseme = e.VisemeId;
            if (this.Mouth != shape)
            {
                this.Updated?.Invoke(this, new());
            }
        }

        private static class BlinkBehavior
        {
            private static readonly double MinSeconds = 2.0;
            private static readonly double MaxSeconds = 8.0;
            private static readonly double AttentionExponent = 2.0;

            public static bool StartAutomaticBlink(DateTime now, DateTime? lastBlinkTime, double attention)
            {
                if (lastBlinkTime == null)
                {
                    return true;
                }

                attention = Math.Clamp(attention, 0.0, 1.0);

                var blinkInterval = TimeSpan.FromSeconds(double.Lerp(MinSeconds, MaxSeconds, Math.Pow(1.0 - attention, AttentionExponent)));
                blinkInterval *= double.Lerp(0.95, 1.05, Random.Shared.NextDouble());

                return now - lastBlinkTime >= blinkInterval;
            }
        }
    }
}
