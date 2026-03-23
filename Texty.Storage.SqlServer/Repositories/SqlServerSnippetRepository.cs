using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Storage.SqlServer.Repositories;

public sealed class SqlServerSnippetRepository : ISnippetRepository
{
    private readonly SqlServerStorageOptions _options;
    private readonly SqlServerStorageState _state;

    public SqlServerSnippetRepository(SqlServerStorageOptions options, SqlServerStorageState state)
    {
        _options = options;
        _state = state;
    }

    public Task<IReadOnlyList<Snippet>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Snippet> items = _state.Snippets.Values
            .OrderBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(items);
    }

    public Task<Snippet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _state.Snippets.TryGetValue(id, out var snippet);
        return Task.FromResult(snippet);
    }

    public Task SaveAsync(Snippet snippet, CancellationToken cancellationToken = default)
    {
        _state.Snippets[snippet.Id] = snippet;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _state.Snippets.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SnippetSearchResult>> SearchAsync(SnippetSearchQuery query, CancellationToken cancellationToken = default)
    {
        var results = _state.Snippets.Values
            .Where(s => !s.Deleted)
            .Where(s => query.IncludeHidden || s.HighlightMode != SnippetHighlightMode.Hidden)
            .Where(s => query.FolderId is null || s.FolderId == query.FolderId.Value)
            .Where(s => string.IsNullOrWhiteSpace(query.Tag) || s.Tags.Any(t => string.Equals(t.Value, query.Tag, StringComparison.OrdinalIgnoreCase)))
            .Where(s => string.IsNullOrWhiteSpace(query.TargetProcess) ||
                        s.Triggers.Any(t =>
                            t.Enabled &&
                            !string.IsNullOrWhiteSpace(t.TargetProcess) &&
                            string.Equals(NormalizeProcessName(t.TargetProcess), NormalizeProcessName(query.TargetProcess), StringComparison.OrdinalIgnoreCase)))
            .Select(s => new SnippetSearchResult(s, Score(query.Term, s)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Snippet.Title, StringComparer.OrdinalIgnoreCase)
            .Take(query.Limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<SnippetSearchResult>>(results);
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

    private static string NormalizeProcessName(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return string.Empty;
        }

        var value = processName.Trim();
        return value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? value[..^4]
            : value;
    }
}
