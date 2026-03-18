namespace Texty.AI.Providers;

public sealed class GroqProvider : ApiKeyHttpAiProvider
{
    /// <summary>
    /// Initialisiert eine neue Instanz von GroqProvider, konfiguriert für die Groq-API mit dem Umgebungsvariablen-Namen "GROQ_API_KEY" und dem Chat-Completions-Endpunkt.
    /// </summary>
    /// <param name="httpClient">Der HttpClient, mit dem HTTP-Anfragen an die Groq-API gesendet werden.</param>
    public GroqProvider(HttpClient httpClient)
        : base(httpClient, "GROQ_API_KEY", "https://api.groq.com/openai/v1/chat/completions")
    {
    }

    public override string Name => "Groq";

    protected override string DefaultModel => "llama-3.3-70b-versatile";
}
