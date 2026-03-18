namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface IClipboardGateway
{
    /// <summary>
/// Erfasst den aktuellen Inhalt der Zwischenablage als ClipboardItem.
/// </summary>
/// <returns>`ClipboardItem` mit dem aktuellen Zwischenablageinhalt, oder `null`, wenn kein Inhalt verfügbar ist.</returns>
Task<ClipboardItem?> SnapshotAsync(CancellationToken cancellationToken = default);
    /// <summary>
/// Schreibt das angegebene ClipboardItem in die Systemzwischenablage und ersetzt deren aktuellen Inhalt.
/// </summary>
/// <param name="clipboardItem">Das Zwischenablageobjekt, das in die Zwischenablage gesetzt werden soll.</param>
Task SetAsync(ClipboardItem clipboardItem, CancellationToken cancellationToken = default);
    /// <summary>
/// Stellt die Systemzwischenablage auf den angegebenen Snapshot wieder her.
/// </summary>
/// <param name="snapshot">Der wiederherzustellende Zwischenablage-Snapshot; bei `null` wird keine Änderung vorgenommen.</param>
Task RestoreAsync(ClipboardItem? snapshot, CancellationToken cancellationToken = default);
}

public interface IKeystrokeEmitter
{
    /// <summary>
/// Löst eine Einfügeaktion (Paste) über simulierte Tastatureingaben oder das System aus.
/// </summary>
/// <param name="cancellationToken">Abbruch-Token, um das Sendevorgang vorzeitig abzubrechen.</param>
Task SendPasteAsync(CancellationToken cancellationToken = default);
    /// <summary>
/// Führt eine Folge von durch Zeichenketten beschriebenen Tastenaktionen oder Befehlen aus.
/// </summary>
/// <param name="actions">Die in Ausführungsreihenfolge zu sendenden Aktionen; jede Zeichenkette identifiziert ein Tastenereignis oder einen Befehl.</param>
/// <param name="cancellationToken">Token zur Abbruchsteuerung der asynchronen Operation.</param>
Task SendSequenceAsync(IReadOnlyList<string> actions, CancellationToken cancellationToken = default);
}

public interface IInsertionPipeline
{
    /// <summary>
        /// Führt die Einfüge-Pipeline für die angegebene Nutzlast und den angegebenen Kontext aus.
        /// </summary>
        /// <param name="payload">Die Einfüge-Nutzlast, die in die Pipeline eingespeist wird.</param>
        /// <param name="context">Ausführungsbezogene Metadaten und Einstellungen für die Pipeline.</param>
        /// <param name="cancellationToken">Token zum Abbrechen der Ausführung.</param>
        /// <returns>Eine Liste von InsertionStepResult-Objekten, die die Ergebnisse jeder Pipeline-Stufe in Ausführungsreihenfolge enthalten.</returns>
        Task<IReadOnlyList<InsertionStepResult>> ExecuteAsync(
        InsertionPayload payload,
        InsertionContext context,
        CancellationToken cancellationToken = default);
}
