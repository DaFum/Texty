using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Storage.SqlServer.Repositories;

public sealed class SqlServerSnippetRepository : ISnippetRepository
{
    private readonly SqlServerStorageOptions _options;
    private readonly SqlServerStorageState _state;

    /// <summary>
    /// Initialisiert eine neue Instanz von <see cref="SqlServerSnippetRepository"/> mit den angegebenen Speicheroptionen und dem Zustandsobjekt.
    /// </summary>
    /// <param name="options">Konfigurationsoptionen für die SQL Server‑basierte Speicherung.</param>
    /// <param name="state">In-memory Zustand/Cache, der Schnipsel (Snippets) und dazugehörige Indizes verwaltet.</param>
    public SqlServerSnippetRepository(SqlServerStorageOptions options, SqlServerStorageState state)
    {
        _options = options;
        _state = state;
    }

    /// <summary>
    /// Gibt alle Snippets zurück, sortiert nach Titel (Groß-/Kleinschreibung ignoriert).
    /// </summary>
    /// <returns>Eine IReadOnlyList mit allen Snippets, aufsteigend nach Titel sortiert.</returns>
    public Task<IReadOnlyList<Snippet>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Snippet> items = _state.Snippets.Values
            .OrderBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(items);
    }

    /// <summary>
    /// Gibt das Snippet mit der angegebenen ID zurück, falls vorhanden.
    /// </summary>
    /// <param name="id">Die eindeutige Kennung des Snippets.</param>
    /// <returns>Das gefundene Snippet, oder <c>null</c>, wenn kein Snippet mit dieser ID existiert.</returns>
    public Task<Snippet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _state.Snippets.TryGetValue(id, out var snippet);
        return Task.FromResult(snippet);
    }

    /// <summary>
    /// Speichert oder aktualisiert einen Snippet-Eintrag im Speicher.
    /// </summary>
    /// <param name="snippet">Der zu speichernde oder zu aktualisierende Snippet.</param>
    public Task SaveAsync(Snippet snippet, CancellationToken cancellationToken = default)
    {
        _state.Snippets[snippet.Id] = snippet;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Entfernt das Snippet mit der angegebenen Id aus dem Speicher, falls vorhanden.
    /// </summary>
    /// <param name="id">Die Id des zu löschenden Snippets.</param>
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _state.Snippets.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Durchsucht gespeicherte Snippets anhand der im Query spezifizierten Kriterien und liefert bewertete Treffer.
    /// </summary>
    /// <param name="query">Such- und Filterkriterien (z. B. Term, IncludeHidden, FolderId, Tag, Limit) die bestimmen, welche Snippets berücksichtigt, wie sie bewertet und wie viele Ergebnisse zurückgegeben werden.</param>
    /// <returns>Eine Liste von SnippetSearchResult-Objekten, geordnet nach Relevanz (absteigend) und dann nach Titel (aufsteigend); enthält nur Treffer mit Score &gt; 0 und maximal die Anzahl aus query.Limit.</returns>
    public Task<IReadOnlyList<SnippetSearchResult>> SearchAsync(SnippetSearchQuery query, CancellationToken cancellationToken = default)
    {
        var results = _state.Snippets.Values
            .Where(s => !s.Deleted)
            .Where(s => query.IncludeHidden || s.HighlightMode != SnippetHighlightMode.Hidden)
            .Where(s => query.FolderId is null || s.FolderId == query.FolderId.Value)
            .Where(s => string.IsNullOrWhiteSpace(query.Tag) || s.Tags.Any(t => string.Equals(t.Value, query.Tag, StringComparison.OrdinalIgnoreCase)))
            .Select(s => new SnippetSearchResult(s, Score(query.Term, s)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Snippet.Title, StringComparer.OrdinalIgnoreCase)
            .Take(query.Limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<SnippetSearchResult>>(results);
    }

    /// <summary>
    /// Berechnet einen Relevanzwert eines Snippets für eine Suchanfrage-Term.
    /// </summary>
    /// <param name="term">Der Suchbegriff; wird getrimmt. Bei null oder nur Whitespace wird ein Standardwert zurückgegeben.</param>
    /// <param name="snippet">Das zu bewertende Snippet.</param>
    /// <returns>Ein numerischer Score: höhere Werte bedeuten höhere Relevanz. Wenn `term` null oder nur Whitespace ist, wird `1` zurückgegeben. Andernfalls setzt sich der Score aus Gewichten zusammen: Titel +4, Shortcut +3, Klartext +2, beliebiges Tag +1 (summiert).</returns>
    private static double Score(string? term, Snippet snippet)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return 1;
        }

        var value = term.Trim();
        double score = 0;
        if (snippet.Title.Contains(value, StringComparison.OrdinalIgnoreCase))
        {
            score += 4;
        }

        if (snippet.Shortcut.Contains(value, StringComparison.OrdinalIgnoreCase))
        {
            score += 3;
        }

        if (snippet.PlainText.Contains(value, StringComparison.OrdinalIgnoreCase))
        {
            score += 2;
        }

        if (snippet.Tags.Any(x => x.Value.Contains(value, StringComparison.OrdinalIgnoreCase)))
        {
            score += 1;
        }

        return score;
    }
}
