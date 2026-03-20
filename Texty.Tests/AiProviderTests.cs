using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Texty.AI;
using Texty.AI.Providers;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Tests;

public sealed class AiProviderTests
{
    private static readonly SemaphoreSlim EnvSync = new(1, 1);

    [Fact]
    public async Task AnthropicProvider_ShouldReturnStub_WhenApiKeyMissing()
    {
        await WithEnvironmentVariablesAsync(
            new Dictionary<string, string?>
            {
                ["ANTHROPIC_API_KEY"] = null,
            },
            async () =>
            {
                var provider = new AnthropicProvider(new HttpClient(new StubHttpMessageHandler(_ => throw new InvalidOperationException())));
                var response = await provider.GenerateAsync(new AiRequest("Hallo", null, null, null));

                Assert.Equal("Anthropic", response.Provider);
                Assert.Contains("[Anthropic stub]", response.Text, StringComparison.Ordinal);
            });
    }

    [Fact]
    public async Task AnthropicProvider_CheckHealth_ShouldUseModelsEndpointAndHeaders()
    {
        await WithEnvironmentVariablesAsync(
            new Dictionary<string, string?>
            {
                ["ANTHROPIC_API_KEY"] = "test-anthropic-key",
            },
            async () =>
            {
                var handler = new StubHttpMessageHandler(request =>
                {
                    Assert.Equal(HttpMethod.Get, request.Method);
                    Assert.Equal("https://api.anthropic.com/v1/models", request.RequestUri!.AbsoluteUri);
                    Assert.True(request.Headers.TryGetValues("x-api-key", out var apiKeys));
                    Assert.Contains("test-anthropic-key", apiKeys);
                    Assert.True(request.Headers.TryGetValues("anthropic-version", out var versions));
                    Assert.Contains("2023-06-01", versions);
                    return JsonResponse("""{"data":[]}""");
                });

                var provider = new AnthropicProvider(new HttpClient(handler));
                var health = await provider.CheckHealthAsync();

                Assert.True(health.Available);
                Assert.Equal("Anthropic", health.Provider);
            });
    }

    [Fact]
    public async Task OllamaProvider_ShouldUseConfiguredBaseUrl()
    {
        await WithEnvironmentVariablesAsync(
            new Dictionary<string, string?>
            {
                ["OLLAMA_BASE_URL"] = "http://127.0.0.1:11434",
                ["OLLAMA_MODEL"] = "llama3.2",
            },
            async () =>
            {
                var handler = new StubHttpMessageHandler(request =>
                {
                    Assert.Equal("http://127.0.0.1:11434/api/chat", request.RequestUri!.AbsoluteUri);
                    return JsonResponse("""{"message":{"content":"antwort"}}""");
                });
                var provider = new OllamaProvider(new HttpClient(handler));

                var response = await provider.GenerateAsync(new AiRequest("test", null, null, null));

                Assert.Equal("Ollama", response.Provider);
                Assert.Equal("antwort", response.Text);
                Assert.Equal("llama3.2", response.Model);
            });
    }

    [Fact]
    public async Task Gpt4AllProvider_ShouldUseBaseUrlFallbackWhenEndpointNotSet()
    {
        await WithEnvironmentVariablesAsync(
            new Dictionary<string, string?>
            {
                ["GPT4ALL_ENDPOINT"] = null,
                ["GPT4ALL_BASE_URL"] = "http://localhost:7777",
            },
            async () =>
            {
                var handler = new StubHttpMessageHandler(request =>
                {
                    Assert.Equal("http://localhost:7777/v1/chat/completions", request.RequestUri!.AbsoluteUri);
                    return JsonResponse("""{"choices":[{"message":{"content":"ok gpt4all"}}]}""");
                });
                var provider = new Gpt4AllProvider(new HttpClient(handler));

                var response = await provider.GenerateAsync(new AiRequest("test", null, "gpt4all", null));

                Assert.Equal("GPT4All", response.Provider);
                Assert.Equal("ok gpt4all", response.Text);
            });
    }

    [Fact]
    public async Task Gpt4AllProvider_ShouldPreferExplicitEndpoint()
    {
        await WithEnvironmentVariablesAsync(
            new Dictionary<string, string?>
            {
                ["GPT4ALL_ENDPOINT"] = "http://localhost:9900/v1/chat/completions",
                ["GPT4ALL_BASE_URL"] = "http://localhost:7777",
            },
            async () =>
            {
                var handler = new StubHttpMessageHandler(request =>
                {
                    Assert.Equal("http://localhost:9900/v1/chat/completions", request.RequestUri!.AbsoluteUri);
                    return JsonResponse("""{"choices":[{"message":{"content":"ok explicit"}}]}""");
                });
                var provider = new Gpt4AllProvider(new HttpClient(handler));

                var response = await provider.GenerateAsync(new AiRequest("test", null, "gpt4all", null));

                Assert.Equal("GPT4All", response.Provider);
                Assert.Equal("ok explicit", response.Text);
            });
    }

    [Fact]
    public async Task Gpt4AllProvider_Health_ShouldUseModelsEndpoint()
    {
        await WithEnvironmentVariablesAsync(
            new Dictionary<string, string?>
            {
                ["GPT4ALL_ENDPOINT"] = "http://localhost:9900/v1/chat/completions",
                ["GPT4ALL_BASE_URL"] = null,
            },
            async () =>
            {
                var handler = new StubHttpMessageHandler(request =>
                {
                    Assert.Equal(HttpMethod.Get, request.Method);
                    Assert.Equal("http://localhost:9900/v1/models", request.RequestUri!.AbsoluteUri);
                    return JsonResponse("""{"data":[]}""");
                });
                var provider = new Gpt4AllProvider(new HttpClient(handler));

                var health = await provider.CheckHealthAsync();

                Assert.True(health.Available);
                Assert.Equal("GPT4All", health.Provider);
            });
    }

    [Fact]
    public async Task OllamaProvider_Health_ShouldCallTagsEndpoint()
    {
        await WithEnvironmentVariablesAsync(
            new Dictionary<string, string?>
            {
                ["OLLAMA_BASE_URL"] = "http://localhost:11434",
            },
            async () =>
            {
                var handler = new StubHttpMessageHandler(request =>
                {
                    Assert.Equal(HttpMethod.Get, request.Method);
                    Assert.Equal("http://localhost:11434/api/tags", request.RequestUri!.AbsoluteUri);
                    return JsonResponse("""{"models":[]}""");
                });
                var provider = new OllamaProvider(new HttpClient(handler));

                var health = await provider.CheckHealthAsync();

                Assert.True(health.Available);
                Assert.Equal("Ollama", health.Provider);
            });
    }

    [Fact]
    public async Task OllamaProvider_ShouldParseChatResponse()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Contains("/api/chat", request.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
            return JsonResponse("""{"message":{"content":"antwort"}}""");
        });
        var provider = new OllamaProvider(new HttpClient(handler));

        var response = await provider.GenerateAsync(new AiRequest("test", null, "llama3.1", null));

        Assert.Equal("Ollama", response.Provider);
        Assert.Equal("antwort", response.Text);
    }

    [Fact]
    public async Task Gpt4AllProvider_ShouldParseOpenAiCompatibleResponse()
    {
        var handler = new StubHttpMessageHandler(_ =>
            JsonResponse("""{"choices":[{"message":{"content":"ok gpt4all"}}]}"""));
        var provider = new Gpt4AllProvider(new HttpClient(handler));

        var response = await provider.GenerateAsync(new AiRequest("test", null, "gpt4all", null));

        Assert.Equal("GPT4All", response.Provider);
        Assert.Equal("ok gpt4all", response.Text);
    }

    [Fact]
    public async Task AiProviderHealthService_ShouldReportHealthForAllProviders()
    {
        var healthyProvider = new TestHealthProvider("Healthy", true);
        var failingProvider = new TestHealthProvider("Failing", false);
        var plainProvider = new TestPlainProvider("Plain");
        var registry = new AiProviderRegistry([healthyProvider, failingProvider, plainProvider]);
        var service = new AiProviderHealthService(registry);

        var results = await service.CheckAllAsync();

        Assert.Equal(3, results.Count);
        Assert.Contains(results, r => r.Provider == "Healthy" && r.Available);
        Assert.Contains(results, r => r.Provider == "Failing" && !r.Available);
        Assert.Contains(results, r => r.Provider == "Plain" && !r.Available && r.Message.Contains("No health check", StringComparison.Ordinal));
    }

    private static async Task WithEnvironmentVariablesAsync(
        IReadOnlyDictionary<string, string?> values,
        Func<Task> action)
    {
        await EnvSync.WaitAsync();
        var originals = values.Keys.ToDictionary(key => key, key => Environment.GetEnvironmentVariable(key));
        try
        {
            foreach (var pair in values)
            {
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }

            await action();
        }
        finally
        {
            foreach (var pair in originals)
            {
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }

            EnvSync.Release();
        }
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.FromResult(_handler(request));
        }
    }

    private sealed class TestPlainProvider : IAiProvider
    {
        public TestPlainProvider(string name) => Name = name;

        public string Name { get; }

        public Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default)
        {
            _ = request;
            _ = cancellationToken;
            return Task.FromResult(new AiResponse(Name, "m", "x"));
        }
    }

    private sealed class TestHealthProvider : IAiProvider, IAiHealthCheckProvider
    {
        private readonly bool _available;

        public TestHealthProvider(string name, bool available)
        {
            Name = name;
            _available = available;
        }

        public string Name { get; }

        public Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default)
        {
            _ = request;
            _ = cancellationToken;
            return Task.FromResult(new AiResponse(Name, "m", "x"));
        }

        public Task<AiProviderHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return Task.FromResult(new AiProviderHealthResult(Name, _available, _available ? "ok" : "fail"));
        }
    }
}
