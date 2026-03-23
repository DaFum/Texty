using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.AI.Providers;

public sealed class OpenAiTranslationProvider : ITranslationProvider
{
    private readonly IAiProvider _provider;

    public OpenAiTranslationProvider(IAiProvider provider)
    {
        _provider = provider;
    }

    public string Name => "OpenAI";

    public async Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _provider.GenerateAsync(
            new AiRequest(
                Prompt: $"Translate the following text from {request.SourceLanguage} to {request.TargetLanguage}: {request.Text}",
                SystemPrompt: "You are a precise translator. Return only translated text.",
                Model: "gpt-5.4-mini",
                Temperature: 0.1),
            cancellationToken);

        return new TranslationResult(Name, result.Text);
    }
}
