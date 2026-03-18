using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.AI.Providers;

public sealed class OpenAiTranslationProvider : ITranslationProvider
{
    private readonly IAiProvider _provider;

    /// <summary>
    /// Initialisiert eine neue Instanz von <see cref="OpenAiTranslationProvider"/> mit dem angegebenen KI-Anbieter.
    /// </summary>
    /// <param name="provider">Die Implementierung von <see cref="IAiProvider"/>, die zur Erzeugung der Übersetzungen verwendet wird.</param>
    public OpenAiTranslationProvider(IAiProvider provider)
    {
        _provider = provider;
    }

    public string Name => "OpenAI";

    /// <summary>
    /// Übersetzt den angegebenen Text von der Quell- in die Zielsprache mithilfe des konfigurierten KI-Anbieters.
    /// </summary>
    /// <param name="request">Enthält Quell- und Zielsprache sowie den zu übersetzenden Text.</param>
    /// <returns>`TranslationResult` mit dem Namen des verwendeten Providers und dem übersetzten Text.</returns>
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
