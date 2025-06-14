namespace RenderLoop.Demo.MGS.Codec.Conversation
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    internal class ConversationModel(CodecOptions codecOptions, Func<CharacterResponse, Task> speechFunction) : IDisposable
    {
        private static readonly JsonDocument Configuration;
        private static readonly JsonSerializerOptions JsonOptions;
        private readonly HttpClient httpClient = new() { BaseAddress = new Uri(codecOptions.LMEndpoint) };
        private CancellationTokenSource cts = new();
        private Task activeWork;

        private static readonly Dictionary<string, string> Aliases = new()
        {
            { "Snake", "Solid Snake" },
            { "Otacon", "Hal Emmerich" },
            { "Liquid", "Liquid Snake" },
            { "Meryl", "Meryl Silverburgh" },
            { "Campbell", "Roy Campbell" },
            { "Colonel", "Roy Campbell" },
            { "Naomi", "Naomi Hunter" },
            { "Nastasha", "Nastasha Romanenko" },
        };

        private readonly List<Message> messages =
        [
            new("system", Configuration.RootElement.GetProperty("inference_params").GetProperty("pre_prompt").GetString() + Configuration.RootElement.GetProperty("inference_params").GetProperty("pre_prompt_suffix").GetString()),
        ];

        public event EventHandler<MessageReceivedArgs> MessageReceived;

        static ConversationModel()
        {
            JsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

            using var configStream = typeof(ConversationModel).Assembly.GetManifestResourceStream($"{typeof(ConversationModel).Namespace}.model_config.json");
            Configuration = JsonDocument.Parse(configStream!);
        }

        public void Dispose()
        {
            this.cts.Cancel();
            this.httpClient.Dispose();
        }

        public void AddUserMessage(string content)
        {
            this.cts.Cancel();

            lock (this.messages)
            {
                this.messages.Add(new Message("user", $"User: {content.Trim()}\n"));
            }

            this.cts = new CancellationTokenSource();
            this.activeWork = this.ProcessNextResponses(this.cts.Token);
        }

        public async Task ProcessNextResponses(CancellationToken cancel)
        {
            var response = await this.GetNextResponse(cancel);
            var parsed = new ConversationParser().Parse(response);
            foreach (var item in parsed)
            {
                switch (item)
                {
                    case CharacterResponse characterResponse:
                        lock (this.messages)
                        {
                            this.messages.Add(new Message("assistant", $"{characterResponse.Name}{(string.IsNullOrWhiteSpace(characterResponse.Mood) ? string.Empty : $" [{characterResponse.Mood}]")}: {characterResponse.Text}\n"));
                        }

                        if (Aliases.TryGetValue(characterResponse.Name, out var name))
                        {
                            characterResponse = characterResponse with { Name = name };
                        }

                        await speechFunction(characterResponse);
                        break;
                }
            }
        }

        public async Task<string> GetNextResponse(CancellationToken cancel)
        {
            string requestBody;
            lock (this.messages)
            {
                requestBody = JsonSerializer.Serialize(
                new
                {
                    model = codecOptions.LanguageModel,
                    this.messages,
                    temperature = 0.7,
                    max_tokens = -1,
                    stream = false,
                },
                JsonOptions);
            }

            var response = await this.httpClient.PostAsync("/v1/chat/completions", new StringContent(requestBody, Encoding.UTF8, "application/json"), cancel).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancel).ConfigureAwait(false);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()!;
        }

        private record class Message(string Role, string Content);

        public class MessageReceivedArgs(string character, string mood, string message) : EventArgs
        {
            public string Character { get; } = character;

            public string Mood { get; } = mood;

            public string Message { get; } = message;
        }
    }
}
