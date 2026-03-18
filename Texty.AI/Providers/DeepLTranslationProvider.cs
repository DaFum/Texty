using System.Net.Http.Json;
using System.Text.Json;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.AI.Providers;

public sealed class DeepLTranslationProvider : ITranslationProvider
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initialisiert eine neue Instanz von DeepLTranslationProvider und speichert den bereitgestellten HttpClient zur Verwendung bei HTTP-Anfragen.
    /// </summary>
    /// <param name="httpClient">HttpClient, der für Anfragen an den DeepL-API-Endpunkt verwendet wird.</param>
    public DeepLTranslationProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Name => "DeepL";

    /// <summary>
    /// Übersetzt den im Request angegebenen Text mithilfe des DeepL-API oder liefert bei fehlendem API‑Schlüssel bzw. Fehlern einen erklärenden Fallback.
    /// </summary>
    /// <param name="request">Enthält den zu übersetzenden Text sowie Quell- und Zielsprache.</param>
    /// <param name="cancellationToken">Token zum Abbrechen der laufenden Anfrage.</param>
    /// <returns>Ein <see cref="TranslationResult"/> mit dem Namen des Providers und dem übersetzten Text. Wenn die Umgebungsvariable DEEPL_API_KEY fehlt, enthält das Ergebnis den Fallback "[{TargetLanguage}] {OriginalText}". Bei einer fehlerhaften HTTP-Antwort enthält das Ergebnis eine Fehlermeldung mit Statuscode und Antwortkörper.</returns>
    public async Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken = default)
    {
        var apiKey = Environment.GetEnvironmentVariable("DEEPL_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new TranslationResult(Name, $"[{request.TargetLanguage}] {request.Text}");
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api-free.deepl.com/v2/translate")
        {
            Content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("auth_key", apiKey),
                new KeyValuePair<string, string>("text", request.Text),
                new KeyValuePair<string, string>("source_lang", request.SourceLanguage.ToUpperInvariant()),
                new KeyValuePair<string, string>("target_lang", request.TargetLanguage.ToUpperInvariant()),
            ]),
        };

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new TranslationResult(Name, $"[DeepL error] {response.StatusCode}: {body}");
        }

        using var json = JsonDocument.Parse(body);
        var text = json.RootElement.GetProperty("translations")[0].GetProperty("text").GetString() ?? request.Text;
        return new TranslationResult(Name, text);
    }
}
