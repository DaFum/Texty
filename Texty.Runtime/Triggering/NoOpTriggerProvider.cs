using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Triggering;

public sealed class NoOpTriggerProvider : ITriggerProvider
{
    public NoOpTriggerProvider(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public async IAsyncEnumerable<TriggerSignal> ListenAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected cancellation path.
        }

        yield break;
    }
}
