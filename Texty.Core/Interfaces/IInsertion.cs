namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface IClipboardGateway
{
    Task<ClipboardItem?> SnapshotAsync(CancellationToken cancellationToken = default);
    Task SetAsync(ClipboardItem clipboardItem, CancellationToken cancellationToken = default);
    Task RestoreAsync(ClipboardItem? snapshot, CancellationToken cancellationToken = default);
}

public interface IKeystrokeEmitter
{
    Task SendPasteAsync(CancellationToken cancellationToken = default);
    Task SendSequenceAsync(IReadOnlyList<string> actions, CancellationToken cancellationToken = default);
}

public interface IInsertionPipeline
{
    Task<IReadOnlyList<InsertionStepResult>> ExecuteAsync(
        InsertionPayload payload,
        InsertionContext context,
        CancellationToken cancellationToken = default);
}
