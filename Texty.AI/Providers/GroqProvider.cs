namespace Texty.AI.Providers;

public sealed class GroqProvider : ApiKeyHttpAiProvider
{
    public GroqProvider(HttpClient httpClient)
        : base(httpClient, "GROQ_API_KEY", "https://api.groq.com/openai/v1/chat/completions")
    {
    }

    public override string Name => "Groq";

    protected override string DefaultModel => "llama-3.3-70b-versatile";
}
