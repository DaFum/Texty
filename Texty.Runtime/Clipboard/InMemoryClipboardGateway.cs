using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Clipboard;

public sealed class InMemoryClipboardGateway : IClipboardGateway
{
    private ClipboardItem? _item;

    public Task<ClipboardItem?> SnapshotAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_item);

    public Task SetAsync(ClipboardItem clipboardItem, CancellationToken cancellationToken = default)
    {
        _item = clipboardItem;
        return Task.CompletedTask;
    }

    public Task RestoreAsync(ClipboardItem? snapshot, CancellationToken cancellationToken = default)
    {
        _item = snapshot;
        return Task.CompletedTask;
    }
}
