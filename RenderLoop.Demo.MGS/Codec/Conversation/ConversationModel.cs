namespace RenderLoop.Demo.MGS.Codec.Conversation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using Pegasus.Common;

    internal class ConversationModel(CodecOptions codecOptions, Func<CharacterResponse, Task> speechFunction, Func<CodeResponse, Task<string>> codeFunction) : IDisposable
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
            { "Master", "Liquid Snake" },
            { "Master Miller", "Liquid Snake" },
            { "Meryl", "Meryl Silverburgh" },
            { "Campbell", "Roy Campbell" },
            { "Colonel", "Roy Campbell" },
            { "Naomi", "Naomi Hunter" },
            { "Nastasha", "Nastasha Romanenko" },
        };

        private readonly List<Message> messages = [];

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
            var getNextResponse = true;
            while (getNextResponse)
            {
                getNextResponse = false;
                try
                {
                    await foreach (var response in this.ParseResponsesAsync(this.GetNextResponseTokensAsync(cancel)).ConfigureAwait(false))
                    {
                        switch (response)
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

                                await speechFunction(characterResponse).ConfigureAwait(false);
                                break;

                            case CodeResponse codeResponse:
                                getNextResponse = true;
                                lock (this.messages)
                                {
                                    this.messages.Add(new Message("assistant", $"```\n{codeResponse.Code.Trim()}\n```\n"));
                                }

                                string output;
                                try
                                {
                                    output = await codeFunction(codeResponse).ConfigureAwait(false);
                                    output = $"{output.Trim()}\nSystem: Task Status Completed";
                                }
                                catch (Exception ex)
                                {
                                    output = $"{ex.ToString().Trim()}\nSystem: Task Status Faulted";
                                }

                                this.messages.Add(new Message("assistant", $"{output}\n"));
                                break;
                        }
                    }
                }
                catch (FormatException)
                {
                    getNextResponse = true;
                    continue;
                }
            }
        }

        private async IAsyncEnumerable<Response> ParseResponsesAsync(IAsyncEnumerable<string> tokens)
        {
            var parser = new ConversationParser();
            var remaining = "";
            await foreach (var token in tokens.ConfigureAwait(false))
            {
                remaining += token;

                var cursor = new Cursor(remaining);
                while (true)
                {
                    var startCursor = cursor;
                    var parsed = parser.Exported.Response(ref cursor);
                    if (parsed != null && cursor.Location < remaining.Length)
                    {
                        yield return parsed.Value;
                    }
                    else
                    {
                        cursor = startCursor;
                        break;
                    }
                }

                remaining = remaining[cursor.Location..];
            }

            foreach (var parsed in parser.Parse(remaining))
            {
                yield return parsed;
            }
        }

        private async IAsyncEnumerable<string> GetNextResponseTokensAsync([EnumeratorCancellation] CancellationToken cancel)
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
                    stream = true,
                },
                JsonOptions);
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json"),
            };

            using var response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false);
            using var reader = new StreamReader(responseStream);

            while (!reader.EndOfStream && !cancel.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancel).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.Ordinal))
                {
                    continue;
                }

                var jsonPart = line[5..].Trim();
                if (jsonPart == "[DONE]")
                {
                    yield break;
                }

                using var doc = JsonDocument.Parse(jsonPart);
                if (doc.RootElement.TryGetProperty("choices", out var choices) && choices[0].GetProperty("delta").TryGetProperty("content", out var content))
                {
                    var tokens = content.GetString();
                    if (!string.IsNullOrEmpty(tokens))
                    {
                        yield return tokens;
                    }
                }
            }
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
