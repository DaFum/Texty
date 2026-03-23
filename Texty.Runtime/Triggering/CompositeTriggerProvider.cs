using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Triggering;

public sealed class CompositeTriggerProvider : ITriggerProvider, IDisposable
{
    private readonly IReadOnlyList<ITriggerProvider> _providers;
    private bool _disposed;

    public CompositeTriggerProvider(string name, IEnumerable<ITriggerProvider> providers)
    {
        Name = name;
        _providers = providers.ToList();
    }

    public string Name { get; }

    public async IAsyncEnumerable<TriggerSignal> ListenAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_providers.Count == 0)
        {
            yield break;
        }

        var channel = Channel.CreateUnbounded<TriggerSignal>();
        var relayTasks = _providers.Select(provider => RelayProviderAsync(provider, channel.Writer, cancellationToken)).ToArray();

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.WhenAll(relayTasks);
                channel.Writer.TryComplete();
            }
            catch (Exception ex)
            {
                channel.Writer.TryComplete(ex);
            }
        }, cancellationToken);

        await foreach (var signal in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return signal;
        }
    }

    private static async Task RelayProviderAsync(
        ITriggerProvider provider,
        ChannelWriter<TriggerSignal> writer,
        CancellationToken cancellationToken)
    {
        await foreach (var signal in provider.ListenAsync(cancellationToken))
        {
            await writer.WriteAsync(signal, cancellationToken);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var provider in _providers)
        {
            if (provider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
