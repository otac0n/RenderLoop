// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.Codec
{
    internal class CodecOptions
    {
        public required string? SpeechEndpoint { get; set; }

        public required string? SpeechKey { get; set; }

        public required string? LMEndpoint { get; set; }

        public required string? LanguageModel { get; set; }
    }
}
