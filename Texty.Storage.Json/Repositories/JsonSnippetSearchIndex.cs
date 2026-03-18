using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Storage.Json.Repositories;

public sealed class JsonSnippetSearchIndex : ISnippetSearchIndex
{
    private readonly List<Snippet> _index = [];

    /// <summary>
    /// Aktualisiert den internen Suchindex und ersetzt ihn durch die angegebenen Snippets.
    /// </summary>
    /// <param name="snippets">Die aufzunehmenden Snippets; Snippets mit Deleted = true werden nicht in den Index übernommen.</param>
    /// <returns>Eine abgeschlossene Task, die den Abschluss des Neuaufbaus signalisiert.</returns>
    public Task RebuildAsync(IEnumerable<Snippet> snippets, CancellationToken cancellationToken = default)
    {
        _index.Clear();
        _index.AddRange(snippets.Where(s => !s.Deleted));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Führt eine Suche im internen Snippet-Index anhand der im Query angegebenen Kriterien aus und liefert Treffer mit berechneten Relevanzwerten.
    /// </summary>
    /// <param name="query">Suchkriterien und Einschränkungen (z. B. Term, IncludeHidden, FolderId, Tag, Limit).</param>
    /// <returns>Eine Liste von SnippetSearchResult-Objekten, sortiert nach absteigendem Relevanzwert und anschließend nach Titel (ordinal, ohne Berücksichtigung der Groß-/Kleinschreibung). Ergebnisse mit Relevanz 0 werden ausgeschlossen; es werden maximal `query.Limit` Einträge zurückgegeben.</returns>
    public IReadOnlyList<SnippetSearchResult> Search(SnippetSearchQuery query)
    {
        var source = _index.AsEnumerable();

        if (!query.IncludeHidden)
        {
            source = source.Where(s => s.HighlightMode != SnippetHighlightMode.Hidden);
        }

        if (query.FolderId is not null)
        {
            source = source.Where(s => s.FolderId == query.FolderId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Tag))
        {
            source = source.Where(s => s.Tags.Any(t => string.Equals(t.Value, query.Tag, StringComparison.OrdinalIgnoreCase)));
        }

        return source
            .Select(s => new SnippetSearchResult(s, Score(query.Term, s)))
            .Where(r => r.Score > 0)
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Snippet.Title, StringComparer.OrdinalIgnoreCase)
            .Take(query.Limit)
            .ToList();
    }

    /// <summary>
    /// Berechnet einen Relevanzwert für ein Snippet bezogen auf einen Suchbegriff.
    /// </summary>
    /// <param name="term">Der zu suchende Begriff; Leerzeichen werden getrimmt. Ein null- oder nur aus Leerzeichen bestehender Begriff wird speziell behandelt.</param>
    /// <param name="snippet">Das zu bewertende Snippet.</param>
    /// <returns>
    /// Eine numerische Relevanzbewertung: <c>1</c>, wenn <paramref name="term"/> null oder nur aus Leerzeichen besteht; sonst die Summe gewichteter Treffer:
    /// +5 wenn der Titel den Begriff enthält, +4 für Treffer im Shortcut, +2 für Treffer im PlainText und +2 für Treffer in Tags. <c>0</c> bedeutet keine Übereinstimmung.
    /// </returns>
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
            score += 5;
        }

        if (snippet.Shortcut.Contains(value, StringComparison.OrdinalIgnoreCase))
        {
            score += 4;
        }

        if (snippet.PlainText.Contains(value, StringComparison.OrdinalIgnoreCase))
        {
            score += 2;
        }

        if (snippet.Tags.Any(t => t.Value.Contains(value, StringComparison.OrdinalIgnoreCase)))
        {
            score += 2;
        }

        return score;
    }
}
