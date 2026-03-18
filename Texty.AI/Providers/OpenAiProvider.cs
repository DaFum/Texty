using Texty.Core.Models;

namespace Texty.AI.Providers;

public sealed class OpenAiProvider : ApiKeyHttpAiProvider
{
    public OpenAiProvider(HttpClient httpClient)
        : base(httpClient, "OPENAI_API_KEY", "https://api.openai.com/v1/chat/completions")
    {
    }

    public override string Name => "OpenAI";

    protected override string DefaultModel => "gpt-5.4-mini";
}
