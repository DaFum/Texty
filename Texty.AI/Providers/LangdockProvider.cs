namespace Texty.AI.Providers;

public sealed class LangdockProvider : ApiKeyHttpAiProvider
{
    /// <summary>
    /// Initialisiert eine neue Instanz von LangdockProvider mit der für Langdock erwarteten API-Konfiguration.
    /// </summary>
    /// <remarks>
    /// Registriert die Umgebungsvariable "LANGDOCK_API_KEY" als Schlüssel und verwendet die Endpunkt-URL "https://api.langdock.com/openai/v1/chat/completions".
    /// </remarks>
    public LangdockProvider(HttpClient httpClient)
        : base(httpClient, "LANGDOCK_API_KEY", "https://api.langdock.com/openai/v1/chat/completions")
    {
    }

    public override string Name => "Langdock";

    protected override string DefaultModel => "gpt-4.1-mini";
}
