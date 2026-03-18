using Texty.Core.Interfaces;

namespace Texty.Runtime.Insertion;

public sealed class NoOpKeystrokeEmitter : IKeystrokeEmitter
{
    /// <summary>
/// Führt keinen Keystroke-Vorgang aus und liefert sofort ein abgeschlossenes Ergebnis.
/// </summary>
/// <returns>Ein bereits abgeschlossener Task.</returns>
public Task SendPasteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
        /// Implementiert eine No‑Op-Version, die die übergebene Sequenz von Aktionen nicht ausführt.
        /// </summary>
        /// <param name="actions">Eine Reihenfolge von Aktions-Strings (z. B. Tasten, Befehle); die Werte werden ignoriert.</param>
        /// <returns>Ein bereits abgeschlossener Task, der das Ende der (nicht ausgeführten) Sequenz signalisiert.</returns>
        public Task SendSequenceAsync(IReadOnlyList<string> actions, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
