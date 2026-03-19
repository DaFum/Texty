using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Insertion;

public sealed class ClipboardInsertionPipeline : IInsertionPipeline
{
    private readonly IClipboardGateway _clipboardGateway;
    private readonly IKeystrokeEmitter _keystrokeEmitter;

    public ClipboardInsertionPipeline(IClipboardGateway clipboardGateway, IKeystrokeEmitter keystrokeEmitter)
    {
        _clipboardGateway = clipboardGateway;
        _keystrokeEmitter = keystrokeEmitter;
    }

    public async Task<IReadOnlyList<InsertionStepResult>> ExecuteAsync(
        InsertionPayload payload,
        InsertionContext context,
        CancellationToken cancellationToken = default)
    {
        _ = context;
        var results = new List<InsertionStepResult>();
        ClipboardItem? snapshot = null;

        try
        {
            snapshot = await _clipboardGateway.SnapshotAsync(cancellationToken);
            results.Add(new InsertionStepResult(InsertionStep.Prepare, true, "Clipboard snapshot captured."));

            await _clipboardGateway.SetAsync(new ClipboardItem(payload.PlainText, payload.HtmlText, null), cancellationToken);
            await _keystrokeEmitter.SendPasteAsync(cancellationToken);
            results.Add(new InsertionStepResult(InsertionStep.Insert, true, "Paste command emitted."));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            results.Add(new InsertionStepResult(InsertionStep.Insert, false, ex.Message));
        }
        finally
        {
            try
            {
                await _clipboardGateway.RestoreAsync(snapshot, cancellationToken);
                results.Add(new InsertionStepResult(InsertionStep.Restore, true, "Clipboard restored."));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                results.Add(new InsertionStepResult(InsertionStep.Restore, false, ex.Message));
            }
        }

        var insertSucceeded = results.Any(r => r.Step == InsertionStep.Insert && r.Success);
        if (insertSucceeded && payload.PostActions.Count > 0)
        {
            try
            {
                await _keystrokeEmitter.SendSequenceAsync(payload.PostActions, cancellationToken);
                results.Add(new InsertionStepResult(InsertionStep.PostProcess, true, "Post-actions executed."));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                results.Add(new InsertionStepResult(InsertionStep.PostProcess, false, ex.Message));
            }
        }

        return results;
    }
}
