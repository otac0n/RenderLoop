// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.Codec
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Speech.Synthesis;

    internal static class CharacterMetadata
    {
        public static readonly ImmutableDictionary<string, (string Id, string Tags)[]> CharacterImages = new Dictionary<string, (string Id, string Tags)[]>()
        {
            ["Solid Snake"] = [
                ("f73b", "Neutral"),
                ("ae23", "Frown"),
                ("a2ca", "Looking Down, Eyes Closed"),
                ("3e2d", "Baring Teeth"),
                ("7228", "Smile"),
                ("3108", "Laugh?"),
                ("3078", "Laugh?"),
                ("2272", "Yell"),
                ("0b7e", "Nude, Neutral"),
                ("c265", "Nude, Frown"),
                ("e6eb", "Nude, Looking Down, Eyes Closed"),
                ("36b4", "Nude, Angry / Yell"),
                ("2089", "Nude, Neutral (duplicate)"),
                ("59f8", "Suited, Neutral"),
                ("da69", "Looking Down, Eyes Closed"),
                ("1c7e", "Nude, Looking Down, Eyes Closed"),
                ("0d84", "Nude, Looking Down, Eyes Closed"),
                ("bc7b", "Looking Down, Eyes Closed"),
            ],
            ["Roy Campbell"] = [
                ("3320", "Neutral"),
                ("ae0c", "Smile"),
                ("7a11", "Surprised"),
                ("5e56", "Frown"),
                ("1a37", "Yell"),
                ("a927", "Sad"), // Eyes Closed
                ("a472", "Reserved"),
                ("bb69", "Dumbfounded"),
            ],
            ["Naomi Hunter"] = [
                ("21f3", "Neutral"),
                ("9cdf", "Smile"),
                ("68e4", "Surprised"),
                ("b96e", "Reserved"),
                ("fd17", "Concerned"),
                ("b176", "Sad"),
                ("de08", "Frown"), // Eyes Closed
                ("f1aa", "Frown"), // Eyes Closed
                ("2118", "Frown"), // Eyes Closed, Shake 'no'
                ("7c87", "Resigned"), // Eyes Closed
                ("25a1", "Pain"), // Eyes Closed
                ("f0ef", "Hide Pain"),
                ("6f74", "Defiant / Hold Back Tears"),
            ],
            ["Mei Ling"] = [
                ("5347", "Neutral"),
                ("6244", "Mischievous"),
                ("ce33", "Smile"),
                ("7e7d", "Enthusiastic"),
                ("2c6a", "Mid Blink"),
                ("1091", "Mid Blink"),
                ("dcf4", "Concerned"),
                ("c60f", "Left-eye Wink"),
                ("fe9f", "Tongue Out"),
                ("40b0", "Wiggle"),
            ],
            ["Hal Emmerich"] = [
                ("ad5d", "Neutral"),
                ("ec59", "Neutral"), // Lens Shine
                ("284a", "Smile"),
                ("9c70", "Frown"),
                ("3069", "Yell"), // Close-up
                ("74a7", "Concerned"),
            ],
            ["Liquid Snake"] = [
                ("9cc0", "Neutral"), // Miller
                ("17ad", "Smile"), // Miller
                ("d6ef", "Wince"), // Miller
                ("6a21", "Smirk"), // Miller
                ("99c1", "Neutral"), // Liquid
                ("80d8", "Frown"), // Liquid
                ("2f79", "Miller -> Liquid Reveal"),
            ],
            ["Nastasha Romanenko"] = [
                ("158d", "Neutral"),
                ("1e41", "Looking Down, Eyes Closed"),
                ("9079", "Smile"),
                ("40c3", "Concerned"),
            ],
            ["Meryl Silverburgh"] = [
                ("7702", "Masked"),
                ("7d66", "Doff Mask"),
                ("39c3", "Neutral"),
                ("6d84", "Don Mask"),
                ("b4af", "Smile"),
                ("1162", "Grin"),
                ("0cc2", "Looking Down, Eyes Closed"),
                ("64f9", "Frown"),
                ("3d59", "Looking Down, Eyes Closed"),
                ("8d32", "Turn To Screen Left"),
                ("dce9", "Facing Screen Left"),
            ],
            ["Sniper Wolf"] = [
                ("3d63", "Neutral"),
                ("b84f", "Smile"),
                ("124a", "Grin"),
                ("6899", "Frown"),
                ("a83c", "Neutral"),
            ],
            ["Jim Houseman"] = [
                ("93f9", "Neutral"),
                ("bf2f", "Frown"),
            ],
        }.ToImmutableDictionary();

        public static readonly ImmutableDictionary<string, string> DisplayedFrequency = new Dictionary<string, string>()
        {
            ["Solid Snake"] = "141.80",
            ["Roy Campbell"] = "140.85",
            ["Naomi Hunter"] = "140.85",
            ["Mei Ling"] = "140.96",
            ["Hal Emmerich"] = "141.12",
            ["Liquid Snake"] = "141.80",
            ["Nastasha Romanenko"] = "141.52",
            ["Meryl Silverburgh"] = "140.15",
            ["Sniper Wolf"] = "141.12",
            ["Jim Houseman"] = "140.85",
            ["Gray Fox"] = "140.48",
        }.ToImmutableDictionary();

        public static readonly ImmutableDictionary<string, string> AssignedAzureVoices = new Dictionary<string, string>()
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
    }
}
