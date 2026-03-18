namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface ITriggerProvider
{
    string Name { get; }
    IAsyncEnumerable<TriggerSignal> ListenAsync(CancellationToken cancellationToken = default);
}

public interface ITriggerEvaluator
{
    Task<IReadOnlyList<TriggerMatch>> EvaluateAsync(
        IEnumerable<TriggerRule> rules,
        TriggerSignal signal,
        CancellationToken cancellationToken = default);
}
