using System.Net.Http.Json;
using System.Text.Json;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.AI.Providers;

public abstract class ApiKeyHttpAiProvider : IAiProvider, IAiHealthCheckProvider
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKeyEnvironmentVariable;
    private readonly string _defaultEndpoint;
    private readonly string? _endpointEnvironmentVariable;
    private readonly bool _requireApiKey;

    protected ApiKeyHttpAiProvider(
        HttpClient httpClient,
        string? apiKeyEnvironmentVariable,
        string endpoint,
        string? endpointEnvironmentVariable = null,
        bool requireApiKey = true)
    {
        _httpClient = httpClient;
        _apiKeyEnvironmentVariable = apiKeyEnvironmentVariable;
        _defaultEndpoint = endpoint;
        _endpointEnvironmentVariable = endpointEnvironmentVariable;
        _requireApiKey = requireApiKey;
    }

    public abstract string Name { get; }

    protected virtual string DefaultModel => "default";
    protected virtual string HealthPath => "/models";

    protected string Endpoint => ResolveEndpoint();

    protected virtual string ResolveEndpoint()
    {
        if (!string.IsNullOrWhiteSpace(_endpointEnvironmentVariable))
        {
            var configuredEndpoint = Environment.GetEnvironmentVariable(_endpointEnvironmentVariable!)?.Trim();
            if (!string.IsNullOrWhiteSpace(configuredEndpoint))
            {
                return configuredEndpoint;
            }
        }

        return _defaultEndpoint;
    }

    protected virtual string? ResolveApiKey()
    {
        return string.IsNullOrWhiteSpace(_apiKeyEnvironmentVariable)
            ? null
            : Environment.GetEnvironmentVariable(_apiKeyEnvironmentVariable!);
    }

    protected virtual IDictionary<string, string> BuildHeaders(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Authorization"] = $"Bearer {apiKey}",
        };
    }

    public virtual async Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        var apiKey = ResolveApiKey();
        if (_requireApiKey && string.IsNullOrWhiteSpace(apiKey))
        {
            return new AiResponse(Name, request.Model ?? DefaultModel, $"[{Name} stub] {request.Prompt}");
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, Endpoint)
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

    public virtual async Task<AiProviderHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var apiKey = ResolveApiKey();
        if (_requireApiKey && string.IsNullOrWhiteSpace(apiKey))
        {
            return new AiProviderHealthResult(Name, false, $"Missing API key environment variable '{_apiKeyEnvironmentVariable}'.");
        }

        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpointUri))
        {
            return new AiProviderHealthResult(Name, false, $"Invalid endpoint '{Endpoint}'.");
        }

        var healthUri = new Uri(endpointUri, HealthPath);
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, healthUri);
            foreach (var header in BuildHeaders(apiKey))
            {
                message.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var ok = response.IsSuccessStatusCode;
            return new AiProviderHealthResult(Name, ok, ok ? "ok" : $"{(int)response.StatusCode} {response.ReasonPhrase}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new AiProviderHealthResult(Name, false, ex.Message);
        }
    }
}
