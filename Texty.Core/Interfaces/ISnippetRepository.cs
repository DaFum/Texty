namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface ISnippetRepository
{
    /// <summary>
/// Ruft alle Snippet-Entitäten als schreibgeschützte Liste ab.
/// </summary>
/// <returns>Eine schreibgeschützte Liste aller Snippets.</returns>
Task<IReadOnlyList<Snippet>> GetAllAsync(CancellationToken cancellationToken = default);
    /// <summary>
/// Ruft ein Snippet anhand seiner eindeutigen Kennung ab.
/// </summary>
/// <param name="id">Die eindeutige GUID des Snippets.</param>
/// <returns>`Snippet` mit der angegebenen ID, oder `null`, wenn kein Snippet gefunden wurde.</returns>
Task<Snippet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>
/// Persistiert das angegebene Snippet; fügt es ein oder aktualisiert es je nach Zustand.
/// </summary>
/// <param name="snippet">Das zu speichernde Snippet.</param>
Task SaveAsync(Snippet snippet, CancellationToken cancellationToken = default);
    /// <summary>
/// Löscht das Snippet mit der angegebenen eindeutigen Kennung.
/// </summary>
/// <param name="id">Die eindeutige Kennung des zu löschenden Snippets.</param>
/// <param name="cancellationToken">Token zum Abbrechen der Löschoperation.</param>
Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>
/// Führt eine Suche nach Snippets anhand der angegebenen Suchkriterien aus.
/// </summary>
/// <param name="query">Die Kriterien und Optionen, nach denen gesucht wird.</param>
/// <param name="cancellationToken">Token zum Abbrechen der Operation.</param>
/// <returns>Eine schreibgeschützte Liste der gefundenen Suchergebnisse; eine leere Liste, wenn keine Treffer vorliegen.</returns>
Task<IReadOnlyList<SnippetSearchResult>> SearchAsync(SnippetSearchQuery query, CancellationToken cancellationToken = default);
}
