using System.Net.Http.Json;
using System.Text.Json;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.AI.Providers;

public sealed class AnthropicProvider : IAiProvider, IAiHealthCheckProvider
{
    private readonly HttpClient _httpClient;

    public AnthropicProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Name => "Anthropic";

    public async Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new AiResponse(Name, request.Model ?? "claude-3-5-sonnet-latest", $"[{Name} stub] {request.Prompt}");
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = JsonContent.Create(new
            {
                model = request.Model ?? "claude-3-5-sonnet-latest",
                max_tokens = 1024,
                temperature = request.Temperature ?? 0.2,
                system = request.SystemPrompt ?? "You are a helpful assistant.",
                messages = new object[]
                {
                    new { role = "user", content = request.Prompt },
                },
            }),
        };

        message.Headers.TryAddWithoutValidation("x-api-key", apiKey);
        message.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new AiResponse(Name, request.Model ?? "claude-3-5-sonnet-latest", $"[{Name} error] {response.StatusCode}: {body}");
        }

        using var doc = JsonDocument.Parse(body);
        var first = doc.RootElement.GetProperty("content")[0];
        var text = first.GetProperty("text").GetString() ?? string.Empty;
        return new AiResponse(Name, request.Model ?? "claude-3-5-sonnet-latest", text);
    }

    public async Task<AiProviderHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new AiProviderHealthResult(Name, false, "Missing API key environment variable 'ANTHROPIC_API_KEY'.");
        }

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, "https://api.anthropic.com/v1/models");
            message.Headers.TryAddWithoutValidation("x-api-key", apiKey);
            message.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
            using var response = await _httpClient.SendAsync(message, cancellationToken);
            return response.IsSuccessStatusCode
                ? new AiProviderHealthResult(Name, true, "ok")
                : new AiProviderHealthResult(Name, false, $"{(int)response.StatusCode} {response.ReasonPhrase}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new AiProviderHealthResult(Name, false, ex.Message);
        }
    }
}

