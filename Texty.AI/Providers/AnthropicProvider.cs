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
        var model = request.Model ?? "claude-3-5-sonnet-latest";
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new AiResponse(Name, model, $"[{Name} stub] {request.Prompt}");
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = JsonContent.Create(new
            {
                model,
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

        try
        {
            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new AiResponse(Name, model, $"[{Name} error] {response.StatusCode}: {body}");
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("content", out var content) ||
                content.ValueKind != JsonValueKind.Array ||
                content.GetArrayLength() == 0)
            {
                return new AiResponse(Name, model, $"[{Name} error] Unexpected response schema.");
            }

            string text = string.Empty;
            foreach (var block in content.EnumerateArray())
            {
                if (!block.TryGetProperty("type", out var typeElement) ||
                    !string.Equals(typeElement.GetString(), "text", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (block.TryGetProperty("text", out var textElement) &&
                    textElement.ValueKind == JsonValueKind.String)
                {
                    text = textElement.GetString() ?? string.Empty;
                    break;
                }
            }

            return new AiResponse(Name, model, text);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new AiResponse(Name, model, $"[{Name} error] {ex.Message}");
        }
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
