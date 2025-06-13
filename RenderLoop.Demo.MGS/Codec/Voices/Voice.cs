// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.Codec.Voices
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    internal abstract class Voice
    {
        private static Dictionary<string, string> AssignedAzureVoices = new()
        {
            { "Solid Snake", "en-US-DerekMultilingualNeural" },
            { "Roy Campbell", "en-US-LewisMultilingualNeural" },
            { "Naomi Hunter", "en-US-LunaNeural" },
            { "Mei Ling", "en-US-AmberNeural" },
            { "Hal Emmerich", "en-US-TonyNeural" },
            { "Liquid Snake", "en-US-AndrewMultilingualNeural" },
            { "Nastasha Romanenko", "en-US-CoraNeural" },
            { "Meryl Silverburgh", "en-US-AvaNeural" },
            { "Sniper Wolf", "en-US-NancyNeural" },
            { "Jim Houseman", "en-US-DavisNeural" },
        };

        public event EventHandler<MouthMovedEventArgs>? MouthMoved;

        public static Voice? GetVoice(CodecOptions options, string name)
        {
            if (!string.IsNullOrWhiteSpace(options.SpeechKey) && !string.IsNullOrWhiteSpace(options.SpeechEndpoint) && AssignedAzureVoices.TryGetValue(name, out var assignedVoice))
            {
                return new AzureCognitiveVoice(options, assignedVoice);
            }

            return null;
        }

        public abstract Task SayAsync(string text);

        protected void InvokeMouthMoved(uint visemeId)
        {
            this.MouthMoved?.Invoke(this, new MouthMovedEventArgs(visemeId));
        }

        public class MouthMovedEventArgs(uint visemeId) : EventArgs
        {
            public uint VisemeId { get; } = visemeId;
        }
    }
}
