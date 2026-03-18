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

    /// <summary>
    /// Initialisiert die AI-Provider-Basis mit HTTP-Client, dem Namen der Umgebungsvariablen für den API-Schlüssel und der Ziel-Endpoint-URL.
    /// </summary>
    /// <param name="httpClient">Der HttpClient, der für HTTP-Anfragen verwendet wird.</param>
    /// <param name="apiKeyEnvironmentVariable">Der Name der Umgebungsvariable, aus der der API-Schlüssel gelesen wird.</param>
    /// <param name="endpoint">Die vollständige URL des API-Endpoints, an den Anfragen gesendet werden.</param>
    protected ApiKeyHttpAiProvider(HttpClient httpClient, string apiKeyEnvironmentVariable, string endpoint)
    {
        _httpClient = httpClient;
        _apiKeyEnvironmentVariable = apiKeyEnvironmentVariable;
        _endpoint = endpoint;
    }

    public abstract string Name { get; }

    protected virtual string DefaultModel => "default";

    /// <summary>
        /// Erstellt HTTP-Header, die den angegebenen API-Schlüssel als Bearer-Token enthalten.
        /// </summary>
        /// <param name="apiKey">Der API-Schlüssel, der in den `Authorization: Bearer {apiKey}`-Header eingefügt wird.</param>
        /// <returns>Ein Dictionary mit case-insensitiven Schlüsseln, das mindestens den `Authorization`-Header enthält.</returns>
        protected virtual IDictionary<string, string> BuildHeaders(string apiKey) =>
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Authorization"] = $"Bearer {apiKey}",
        };

    /// <summary>
    /// Generiert eine AI-Antwort für die gegebene Anfrage, entweder durch einen HTTP-Aufruf zum konfigurierten Endpunkt oder als lokaler Stub, wenn kein API-Schlüssel konfiguriert ist.
    /// </summary>
    /// <param name="request">Die Eingabeanforderung mit Prompt, optionalem System-Prompt, Modellnamen und Temperatur.</param>
    /// <param name="cancellationToken">Token zum Abbrechen der asynchronen Operation.</param>
    /// <returns>Ein AiResponse mit dem Anbieternamen, dem verwendeten Modell und dem erzeugten Text; bei fehlendem API-Schlüssel ein Stub-Text, bei HTTP-Fehlern ein Fehlertext mit Statuscode und Serverantwort.</returns>
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
