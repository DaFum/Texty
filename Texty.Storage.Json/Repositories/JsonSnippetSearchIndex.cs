using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Storage.Json.Repositories;

public sealed class JsonSnippetSearchIndex : ISnippetSearchIndex
{
    private Snippet[] _index = [];

    public Task RebuildAsync(IEnumerable<Snippet> snippets, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        _index = snippets.Where(s => !s.Deleted).ToArray();
        return Task.CompletedTask;
    }

    public IReadOnlyList<SnippetSearchResult> Search(SnippetSearchQuery query)
    {
        var snapshot = _index;
        var source = snapshot.AsEnumerable();

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
