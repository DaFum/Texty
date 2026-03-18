using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Insertion;

public sealed class ClipboardInsertionPipeline : IInsertionPipeline
{
    private readonly IClipboardGateway _clipboardGateway;
    private readonly IKeystrokeEmitter _keystrokeEmitter;

    /// <summary>
    /// Initialisiert eine neue Instanz von <see cref="ClipboardInsertionPipeline"/> mit den benötigten Abhängigkeiten.
    /// </summary>
    /// <param name="clipboardGateway">Schnittstelle zum Erfassen, Setzen und Wiederherstellen des System-Clipboards.</param>
    /// <param name="keystrokeEmitter">Schnittstelle zum Emittieren von Tastaturaktionen (z. B. Einfügen und Post-Aktionen).</param>
    public ClipboardInsertionPipeline(IClipboardGateway clipboardGateway, IKeystrokeEmitter keystrokeEmitter)
    {
        _clipboardGateway = clipboardGateway;
        _keystrokeEmitter = keystrokeEmitter;
    }

    /// <summary>
    /// Führt eine clipboard-basierte Einfügung durch: es wird ein Snapshot der Zwischenablage erstellt, die Zwischenablage mit den Payload-Inhalten ersetzt, ein Einfügevorgang (Paste) ausgelöst und anschließend die ursprüngliche Zwischenablage wiederhergestellt; danach werden ggf. Post-Aktionen ausgeführt.
    /// </summary>
    /// <param name="payload">Enthält die einzufügenden Inhalte (PlainText, HtmlText) und eine optionale Sequenz von Post-Aktionen, die nach dem Einfügen ausgeführt werden.</param>
    /// <returns>Eine Liste von InsertionStepResult-Einträgen, die den Verlauf und das Ergebnis der Schritte Prepare, Insert, Restore und ggf. PostProcess jeweils mit Erfolgsstatus und Nachricht beschreiben.</returns>
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
            catch (Exception ex)
            {
                results.Add(new InsertionStepResult(InsertionStep.Restore, false, ex.Message));
            }
        }

        if (payload.PostActions.Count > 0)
        {
            await _keystrokeEmitter.SendSequenceAsync(payload.PostActions, cancellationToken);
            results.Add(new InsertionStepResult(InsertionStep.PostProcess, true, "Post-actions executed."));
        }

        return results;
    }
}
