using System.Net.Http.Json;
using System.Text.Json;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.AI.Providers;

public sealed class OllamaProvider : IAiProvider, IAiHealthCheckProvider
{
    private readonly HttpClient _httpClient;

    public OllamaProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Name => "Ollama";

    public async Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        var endpoint = ResolveBaseUrl().TrimEnd('/') + "/api/chat";
        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(new
            {
                model = request.Model ?? ResolveDefaultModel(),
                stream = false,
                messages = new object[]
                {
                    new { role = "system", content = request.SystemPrompt ?? "You are a helpful assistant." },
                    new { role = "user", content = request.Prompt },
                },
                options = new
                {
                    temperature = request.Temperature ?? 0.2,
                },
            }),
        };

        try
        {
            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new AiResponse(Name, request.Model ?? ResolveDefaultModel(), $"[{Name} error] {response.StatusCode}: {body}");
            }

            using var doc = JsonDocument.Parse(body);
            var text = doc.RootElement
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;
            return new AiResponse(Name, request.Model ?? ResolveDefaultModel(), text);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new AiResponse(Name, request.Model ?? ResolveDefaultModel(), $"[{Name} error] {ex.Message}");
        }
    }

    public async Task<AiProviderHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var endpoint = ResolveBaseUrl().TrimEnd('/') + "/api/tags";
        try
        {
            using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            return response.IsSuccessStatusCode
                ? new AiProviderHealthResult(Name, true, "ok")
                : new AiProviderHealthResult(Name, false, $"{(int)response.StatusCode} {response.ReasonPhrase}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new AiProviderHealthResult(Name, false, ex.Message);
        }
    }

    private static string ResolveBaseUrl() =>
        Environment.GetEnvironmentVariable("OLLAMA_BASE_URL")?.Trim() ?? "http://localhost:11434";

    private static string ResolveDefaultModel() =>
        Environment.GetEnvironmentVariable("OLLAMA_MODEL")?.Trim() ?? "llama3.1";
}

