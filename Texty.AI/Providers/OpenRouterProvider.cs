namespace Texty.AI.Providers;

public sealed class OpenRouterProvider : ApiKeyHttpAiProvider
{
    public OpenRouterProvider(HttpClient httpClient)
        : base(httpClient, "OPENROUTER_API_KEY", "https://openrouter.ai/api/v1/chat/completions")
    {
    }

    public override string Name => "OpenRouter";

    protected override string DefaultModel => "openai/gpt-4.1-mini";

    protected override IDictionary<string, string> BuildHeaders(string apiKey)
    {
        var headers = base.BuildHeaders(apiKey);
        headers["HTTP-Referer"] = "https://texty.local";
        headers["X-Title"] = "Texty";
        return headers;
    }
}
