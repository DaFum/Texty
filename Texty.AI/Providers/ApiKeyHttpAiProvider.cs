using System.Net.Http.Json;
using System.Text.Json;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.AI.Providers;

public abstract class ApiKeyHttpAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKeyEnvironmentVariable;
    private readonly string _endpoint;

    protected ApiKeyHttpAiProvider(HttpClient httpClient, string apiKeyEnvironmentVariable, string endpoint)
    {
        _httpClient = httpClient;
        _apiKeyEnvironmentVariable = apiKeyEnvironmentVariable;
        _endpoint = endpoint;
    }

    public abstract string Name { get; }

    protected virtual string DefaultModel => "default";

    protected virtual IDictionary<string, string> BuildHeaders(string apiKey) =>
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Authorization"] = $"Bearer {apiKey}",
        };

    public virtual async Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        var apiKey = Environment.GetEnvironmentVariable(_apiKeyEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new AiResponse(Name, request.Model ?? DefaultModel, $"[{Name} stub] {request.Prompt}");
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = JsonContent.Create(new
            {
                model = request.Model ?? DefaultModel,
                messages = new object[]
                {
                    new { role = "system", content = request.SystemPrompt ?? "You are a helpful assistant." },
                    new { role = "user", content = request.Prompt },
                },
                temperature = request.Temperature ?? 0.2,
            }),
        };

        foreach (var header in BuildHeaders(apiKey))
        {
            message.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new AiResponse(Name, request.Model ?? DefaultModel, $"[{Name} error] {response.StatusCode}: {body}");
        }

        using var document = JsonDocument.Parse(body);
        var text = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        return new AiResponse(Name, request.Model ?? DefaultModel, text);
    }
}
