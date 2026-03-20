namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface IAiProvider
{
    string Name { get; }
    Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default);
}

public interface IAiHealthCheckProvider
{
    Task<AiProviderHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}

public interface ITranslationProvider
{
    string Name { get; }
    Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken = default);
}
