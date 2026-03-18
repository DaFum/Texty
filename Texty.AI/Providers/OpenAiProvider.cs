using Texty.Core.Models;

namespace Texty.AI.Providers;

public sealed class OpenAiProvider : ApiKeyHttpAiProvider
{
    /// <summary>
    /// Initialisiert eine neue Instanz von <see cref="OpenAiProvider"/> und konfiguriert den Provider für die OpenAI-API.
    /// </summary>
    /// <param name="httpClient">HttpClient zum Senden von HTTP-Anfragen an die OpenAI-API.</param>
    public OpenAiProvider(HttpClient httpClient)
        : base(httpClient, "OPENAI_API_KEY", "https://api.openai.com/v1/chat/completions")
    {
    }

    public override string Name => "OpenAI";

    protected override string DefaultModel => "gpt-5.4-mini";
}
