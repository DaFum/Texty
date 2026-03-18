namespace Texty.AI.Providers;

public sealed class LangdockProvider : ApiKeyHttpAiProvider
{
    public LangdockProvider(HttpClient httpClient)
        : base(httpClient, "LANGDOCK_API_KEY", "https://api.langdock.com/openai/v1/chat/completions")
    {
    }

    public override string Name => "Langdock";

    protected override string DefaultModel => "gpt-4.1-mini";
}
