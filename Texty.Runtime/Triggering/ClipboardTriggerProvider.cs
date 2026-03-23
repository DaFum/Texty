using Texty.Core.Interfaces;
using Texty.Core.Models;
using Texty.Runtime.Insertion;

namespace Texty.Runtime.Triggering;

public sealed class ClipboardTriggerProvider : ITriggerProvider
{
    private readonly IClipboardGateway _clipboardGateway;
    private readonly IForegroundProcessProvider _foregroundProcessProvider;
    private readonly TimeSpan _pollInterval;

    public ClipboardTriggerProvider(
        IClipboardGateway clipboardGateway,
        IForegroundProcessProvider foregroundProcessProvider,
        TimeSpan? pollInterval = null)
    {
        _clipboardGateway = clipboardGateway;
        _foregroundProcessProvider = foregroundProcessProvider;
        _pollInterval = pollInterval ?? TimeSpan.FromMilliseconds(350);
    }

    public string Name => "Clipboard";

    public async IAsyncEnumerable<TriggerSignal> ListenAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? lastSnapshot = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            ClipboardItem? snapshot;
            try
            {
                snapshot = await _clipboardGateway.SnapshotAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }
            catch
            {
                await Task.Delay(_pollInterval, cancellationToken);
                continue;
            }

            var currentRaw = snapshot?.PlainText ?? snapshot?.HtmlText;
            var current = string.IsNullOrWhiteSpace(currentRaw) ? null : currentRaw;
            var previous = lastSnapshot;
            lastSnapshot = current;

            if (!string.IsNullOrWhiteSpace(current) &&
                !string.Equals(current, previous, StringComparison.Ordinal))
            {
                var processName = _foregroundProcessProvider.GetForegroundProcessName();
                yield return new TriggerSignal(
                    TriggerType.Clipboard,
                    current,
                    processName,
                    TriggerScopeClassifier.Classify(processName));
            }

            await Task.Delay(_pollInterval, cancellationToken);
        }
    }
}
