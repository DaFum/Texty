using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Clipboard;

public sealed class InMemoryClipboardGateway : IClipboardGateway
{
    private ClipboardItem? _item;

    /// <summary>
        /// Liefert einen Schnappschuss des aktuell im Speicher gehaltenen Clipboard-Elements.
        /// </summary>
        /// <returns>Das aktuell gespeicherte <see cref="ClipboardItem"/> oder <c>null</c>, wenn kein Eintrag vorhanden ist.</returns>
        public Task<ClipboardItem?> SnapshotAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_item);

    /// <summary>
    /// Speichert das übergebene ClipboardItem im internen, flüchtigen Zwischenspeicher.
    /// </summary>
    /// <param name="clipboardItem">Das ClipboardItem, das im In-Memory-Clipboard abgelegt werden soll.</param>
    public Task SetAsync(ClipboardItem clipboardItem, CancellationToken cancellationToken = default)
    {
        _item = clipboardItem;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stellt das im Speicher gehaltene Clipboard auf den übergebenen Snapshot wieder her.
    /// </summary>
    /// <param name="snapshot">Der wiederherzustellende Snapshot; `null` leert das in-memory Clipboard.</param>
    public Task RestoreAsync(ClipboardItem? snapshot, CancellationToken cancellationToken = default)
    {
        _item = snapshot;
        return Task.CompletedTask;
    }
}
