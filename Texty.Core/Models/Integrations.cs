namespace Texty.Core.Models;

public sealed record ExternalValueRequest(string Expression);

public sealed record AiRequest(
    string Prompt,
    string? SystemPrompt,
    string? Model,
    double? Temperature);

public sealed record AiResponse(
    string Provider,
    string Model,
    string Text,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record AiProviderHealthResult(
    string Provider,
    bool Available,
    string Message);

public sealed record TranslationRequest(
    string Text,
    string SourceLanguage,
    string TargetLanguage);

public sealed record TranslationResult(string Provider, string Text);
