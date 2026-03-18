namespace Texty.AI.Providers;

public sealed class OpenRouterProvider : ApiKeyHttpAiProvider
{
    /// <summary>
    /// Erstellt eine Instanz des OpenRouter-Providers, die Requests an den OpenRouter-API-Endpunkt vorbereitet.
    /// </summary>
    /// <remarks>
    /// Verwendet die Umgebungsvariable "OPENROUTER_API_KEY" für den API-Schlüssel und konfiguriert den Endpunkt "https://openrouter.ai/api/v1/chat/completions".
    /// </remarks>
    public OpenRouterProvider(HttpClient httpClient)
        : base(httpClient, "OPENROUTER_API_KEY", "https://openrouter.ai/api/v1/chat/completions")
    {
    }

    public override string Name => "OpenRouter";

    protected override string DefaultModel => "openai/gpt-4.1-mini";

    /// <summary>
    /// Erstellt die HTTP-Header für Anfragen an den OpenRouter-API-Endpunkt und fügt Referer- und X-Title-Header hinzu.
    /// </summary>
    /// <param name="apiKey">Der API-Schlüssel zur Authentifizierung der Anfrage.</param>
    /// <returns>Ein Wörterbuch mit Header-Namen und -Werten für die HTTP-Anfrage.</returns>
    protected override IDictionary<string, string> BuildHeaders(string apiKey)
    {
        var headers = base.BuildHeaders(apiKey);
        headers["HTTP-Referer"] = "https://texty.local";
        headers["X-Title"] = "Texty";
        return headers;
    }
}
