using Texty.Core.Interfaces;

namespace Texty.Runtime.Insertion;

public sealed class NoOpKeystrokeEmitter : IKeystrokeEmitter
{
    public Task SendPasteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SendSequenceAsync(IReadOnlyList<string> actions, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
