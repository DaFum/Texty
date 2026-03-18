namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface ISyncOrchestrator
{
    /// <summary>
/// Startet die Synchronisierung der Inhalte vom Quellverzeichnis in das Zielverzeichnis.
/// </summary>
/// <param name="sourceDirectory">Pfad zum Quellverzeichnis, dessen Inhalte synchronisiert werden sollen.</param>
/// <param name="targetDirectory">Pfad zum Zielverzeichnis, in das die Inhalte synchronisiert werden sollen.</param>
/// <param name="cancellationToken">Token zum Abbrechen der Operation.</param>
/// <returns>Ein <see cref="SyncResult"/> mit Informationen zum Ergebnis der Synchronisierung.</returns>
Task<SyncResult> SyncAsync(string sourceDirectory, string targetDirectory, CancellationToken cancellationToken = default);
}

public interface ISnippetSearchIndex
{
    /// <summary>
/// Baut den Suchindex vollständig aus der angegebenen Sammlung von Snippets neu auf.
/// </summary>
/// <param name="snippets">Die Snippets, aus denen der Index erstellt werden soll; die vorhandene Indexdaten werden durch das Ergebnis ersetzt.</param>
/// <param name="cancellationToken">Token zum Abbrechen des Vorgangs.</param>
Task RebuildAsync(IEnumerable<Snippet> snippets, CancellationToken cancellationToken = default);
    /// <summary>
/// Führt eine Suche im Snippet-Index mit der angegebenen Abfrage aus.
/// </summary>
/// <param name="query">Die Suchabfrage, welche Kriterien und Filter für die Suche enthält.</param>
/// <returns>Eine schreibgeschützte Liste von SnippetSearchResult-Objekten, die den Suchkriterien entsprechen; eine leere Liste, wenn keine Treffer vorhanden sind.</returns>
IReadOnlyList<SnippetSearchResult> Search(SnippetSearchQuery query);
}
