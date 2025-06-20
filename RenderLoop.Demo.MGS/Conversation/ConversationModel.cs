namespace RenderLoop.Demo.MGS.Conversation
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
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Pegasus.Common;

    internal partial class ConversationModel : IDisposable
    {
        private static readonly JsonDocument Configuration;
        private static readonly JsonSerializerOptions JsonOptions;
        private readonly ILogger<ConversationModel> logger;
        private readonly LanguageModelOptions options;
        private readonly Func<CharacterResponse, CancellationToken, Task<CharacterResponse>> speechFunction;
        private readonly Func<CodeResponse, Task<string>> codeFunction;
        private readonly HttpClient httpClient;
        private CancellationTokenSource cts = new();
        private Task activeWork;

        private readonly List<Message> messages;

        public event EventHandler<TokenReceivedArgs> TokenReceived;

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

        public ConversationModel(IServiceProvider serviceProvider, string systemPrompt, Func<CharacterResponse, CancellationToken, Task<CharacterResponse>> speechFunction, Func<CodeResponse, Task<string>> codeFunction)
        {
            this.logger = serviceProvider.GetRequiredService<ILogger<ConversationModel>>();
            this.options = serviceProvider.GetRequiredService<LanguageModelOptions>();
            this.httpClient = new(new SerialRequestsWithTimeBufferHandler(this.options.LMCoolDown)) { BaseAddress = new Uri(this.options.LMEndpoint) };
            this.speechFunction = speechFunction;
            this.codeFunction = codeFunction;
            this.messages =
            [
                new("system", systemPrompt),
            ];
        }

        public void Dispose()
        {
            this.cts.Cancel();
            this.httpClient.Dispose();
        }

        public async Task AddUserMessageAsync(string content, bool userPrefix = true)
        {
            LogMessages.ReceivedUserMessage(this.logger, content);

            LogMessages.CancelingActiveGeneration(this.logger);
            await this.cts.CancelAsync().ConfigureAwait(false);
            if (this.activeWork is Task activeWork)
            {
                LogMessages.AwaitingActiveGeneration(this.logger);

                try
                {
                    await activeWork.ConfigureAwait(false);
                }
                catch
                {
                }

                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }

            LogMessages.CanceledActiveGeneration(this.logger);

            lock (this.messages)
            {
                this.messages.Add(new Message("user", userPrefix ? $"User: {content.Trim()}\n" : $"{content.Trim()}\n"));
            }

            this.cts = new CancellationTokenSource();
            var work = this.ProcessNextResponsesAsync(this.cts.Token);
            this.activeWork = work;
            await work.ConfigureAwait(false);
        }

        public async Task ProcessNextResponsesAsync(CancellationToken cancel)
        {
            var getNextResponse = true;
            try
            {
                while (getNextResponse)
                {
                    getNextResponse = false;
                    cancel.ThrowIfCancellationRequested();

                    var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
                    {
                        SingleReader = true,
                        SingleWriter = true,
                    });

                    var producer = this.GetNextResponseTokensAsync(channel.Writer, cancel);

                    try
                    {
                        await foreach (var response in ParseResponsesAsync(channel.Reader, cancel).ConfigureAwait(false))
                        {
                            switch (response)
                            {
                                case CharacterResponse characterResponse:
                                    var content = $"{characterResponse.Name}{(string.IsNullOrWhiteSpace(characterResponse.Mood) ? string.Empty : $" [{characterResponse.Mood}]")}: {characterResponse.Text}";

                                    LogMessages.ReceivedAgentMessage(this.logger, content);

                                    characterResponse = await this.speechFunction(characterResponse, cancel).ConfigureAwait(false);
                                    lock (this.messages)
                                    {
                                        this.messages.Add(new Message("assistant", $"{characterResponse.Name}{(string.IsNullOrWhiteSpace(characterResponse.Mood) ? string.Empty : $" [{characterResponse.Mood}]")}: {characterResponse.Text}\n"));
                                    }

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
                                        output = await this.codeFunction(codeResponse).ConfigureAwait(false);
                                        output = $"{output.Trim()}\nSystem: Task Status Completed";
                                    }
                                    catch (Exception ex)
                                    {
                                        output = $"{ex.ToString().Trim()}\nSystem: Task Status Faulted";
                                    }

                                    this.messages.Add(new Message("assistant", $"{output}\n"));
                                    break;
                            }

                            cancel.ThrowIfCancellationRequested();
                        }
                    }
                    catch (FormatException ex)
                    {
                        LogMessages.RetryingDueToParseFailure(this.logger, ex);
                        getNextResponse = true;
                        continue;
                    }
                    finally
                    {
                        await producer.ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessages.ProcessingFailed(this.logger, ex);
                throw;
            }
        }

        private static async IAsyncEnumerable<Response> ParseResponsesAsync(ChannelReader<string> reader, [EnumeratorCancellation] CancellationToken cancel)
        {
            var parser = new ConversationParser();
            var remaining = "";
            await foreach (var token in reader.ReadAllAsync(cancel).ConfigureAwait(false))
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

        private async Task GetNextResponseTokensAsync(ChannelWriter<string> writer, CancellationToken cancel)
        {
            string requestBody;
            lock (this.messages)
            {
                requestBody = JsonSerializer.Serialize(
                new
                {
                    model = this.options.LanguageModel,
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

            LogMessages.RequestingCompletion(this.logger, request.RequestUri!);
            using var response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel).ConfigureAwait(false);
            LogMessages.ResponseReceived(this.logger, request.RequestUri!, (int)response.StatusCode, response.ReasonPhrase);
            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false);
            using var reader = new StreamReader(responseStream);

            try
            {
                while (!reader.EndOfStream)
                {
                    if (cancel.IsCancellationRequested)
                    {
                        LogMessages.TokenStreamCanceled(this.logger);
                        cancel.ThrowIfCancellationRequested();
                    }

                    var line = await reader.ReadLineAsync(cancel).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var jsonPart = line[5..].Trim();
                    if (jsonPart == "[DONE]")
                    {
                        LogMessages.FinishedTokens(this.logger);
                        writer.Complete();
                        return;
                    }

                    using var doc = JsonDocument.Parse(jsonPart);
                    if (doc.RootElement.TryGetProperty("choices", out var choices) && choices[0].GetProperty("delta").TryGetProperty("content", out var content))
                    {
                        var tokens = content.GetString();
                        if (!string.IsNullOrEmpty(tokens))
                        {
                            LogMessages.ReceivedTokens(this.logger, tokens);
                            this.TokenReceived?.Invoke(this, new TokenReceivedArgs(tokens));
                            await writer.WriteAsync(tokens, cancel).ConfigureAwait(false);
                        }
                    }
                }

                LogMessages.TokenStreamEnded(this.logger);
                throw new FormatException("Unexpected end of token stream.");
            }
            catch (Exception ex)
            {
                writer.Complete(ex);
            }
            finally
            {
                LogMessages.RequestComplete(this.logger, request.RequestUri!);
            }
        }

        private record class Message(string Role, string Content);

        public class TokenReceivedArgs(string token) : EventArgs
        {
            public string Token { get; } = token;
        }

        private class SerialRequestsWithTimeBufferHandler(TimeSpan interval) : DelegatingHandler(new HttpClientHandler())
        {
            private readonly SemaphoreSlim semaphore = new(1, 1);
            private readonly TimeSpan interval = interval;
            private DateTimeOffset lastCompleted = DateTimeOffset.MinValue;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel)
            {
                await this.semaphore.WaitAsync(cancel).ConfigureAwait(false);
                try
                {
                    var elapsed = DateTimeOffset.UtcNow - this.lastCompleted;
                    if (elapsed < this.interval)
                    {
                        var remainder = this.interval - elapsed;
                        await Task.Delay(remainder, cancel).ConfigureAwait(false);
                    }

                    return await base.SendAsync(request, cancel).ConfigureAwait(false);
                }
                finally
                {
                    this.lastCompleted = DateTimeOffset.UtcNow;
                    this.semaphore.Release();
                }
            }
        }

        private static partial class LogMessages
        {
            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Received user message \"{message}\".")]
            public static partial void ReceivedUserMessage(ILogger logger, string message);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Received agent message \"{message}\".")]
            public static partial void ReceivedAgentMessage(ILogger logger, string message);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Canceling active generation...")]
            public static partial void CancelingActiveGeneration(ILogger logger);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Awaiting active generation...")]
            public static partial void AwaitingActiveGeneration(ILogger logger);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Canceled active generation.")]
            public static partial void CanceledActiveGeneration(ILogger logger);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Requesting completion from {url}...")]
            public static partial void RequestingCompletion(ILogger logger, Uri url);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Response received from {url}: {statusCode} {statusMessage}")]
            public static partial void ResponseReceived(ILogger logger, Uri url, int statusCode, string? statusMessage);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Processing responses failed: {error}")]
            public static partial void ProcessingFailed(ILogger logger, Exception error);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Request complete.")]
            public static partial void RequestComplete(ILogger logger, Uri url);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Received tokens \"{tokens}\".")]
            public static partial void ReceivedTokens(ILogger logger, string tokens);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Finished receiving tokens.")]
            public static partial void FinishedTokens(ILogger logger);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Token stream ended.")]
            public static partial void TokenStreamEnded(ILogger logger);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Token stream canceled.")]
            public static partial void TokenStreamCanceled(ILogger logger);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Retrying due to parse failure: {error}")]
            public static partial void RetryingDueToParseFailure(ILogger logger, Exception error);
        }
    }
}
