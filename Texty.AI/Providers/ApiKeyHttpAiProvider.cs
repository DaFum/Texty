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
        var model = request.Model ?? DefaultModel;
        var apiKey = ResolveApiKey();
        if (_requireApiKey && string.IsNullOrWhiteSpace(apiKey))
        {
            return new AiResponse(Name, model, $"[{Name} stub] {request.Prompt}");
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(new
            {
                model,
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

        try
        {
            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new AiResponse(Name, model, $"[{Name} error] {response.StatusCode}: {body}");
            }

            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("choices", out var choices) ||
                choices.ValueKind != JsonValueKind.Array ||
                choices.GetArrayLength() == 0)
            {
                return CreateErrorResponse(model, "Unexpected response schema.");
            }

            var firstChoice = choices[0];
            if (!firstChoice.TryGetProperty("message", out var messageElement) ||
                messageElement.ValueKind != JsonValueKind.Object ||
                !messageElement.TryGetProperty("content", out var contentElement) ||
                contentElement.ValueKind != JsonValueKind.String)
            {
                return CreateErrorResponse(model, "Unexpected response schema.");
            }

            var text = contentElement.GetString() ?? string.Empty;
            return new AiResponse(Name, model, text);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(model, ex.Message);
        }
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

        var healthUri = BuildHealthUri(endpointUri);
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

    private AiResponse CreateErrorResponse(string model, string message)
    {
        return new AiResponse(Name, model, $"[{Name} error] {message}");
    }

    private Uri BuildHealthUri(Uri endpointUri)
    {
        if (Uri.TryCreate(HealthPath, UriKind.Absolute, out var absoluteHealthUri))
        {
            return absoluteHealthUri;
        }

        var endpointSegments = endpointUri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        if (endpointSegments.Count >= 2 &&
            string.Equals(endpointSegments[^2], "chat", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(endpointSegments[^1], "completions", StringComparison.OrdinalIgnoreCase))
        {
            endpointSegments.RemoveRange(endpointSegments.Count - 2, 2);
        }
        else if (endpointSegments.Count > 0 &&
                 string.Equals(endpointSegments[^1], "completions", StringComparison.OrdinalIgnoreCase))
        {
            endpointSegments.RemoveAt(endpointSegments.Count - 1);
        }

        var healthSegments = (string.IsNullOrWhiteSpace(HealthPath) ? "models" : HealthPath)
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        var finalSegments = endpointSegments.Concat(healthSegments).ToArray();
        var builder = new UriBuilder(endpointUri.Scheme, endpointUri.Host, endpointUri.IsDefaultPort ? -1 : endpointUri.Port)
        {
            Path = "/" + string.Join('/', finalSegments),
        };

        return builder.Uri;
    }
}
