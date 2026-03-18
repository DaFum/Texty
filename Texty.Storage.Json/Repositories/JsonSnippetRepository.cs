using System.Text.Json;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Storage.Json.Repositories;

public sealed class JsonSnippetRepository : ISnippetRepository
{
    private readonly JsonStorageOptions _options;

    public JsonSnippetRepository(JsonStorageOptions options)
    {
        _options = options;
        Directory.CreateDirectory(_options.SnippetsDirectory);
    }

    public async Task<IReadOnlyList<Snippet>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var files = Directory.GetFiles(_options.SnippetsDirectory, "*.json", SearchOption.TopDirectoryOnly);
        var list = new List<Snippet>(files.Length);

        foreach (var file in files)
        {
            await using var stream = File.OpenRead(file);
            var snippet = await JsonSerializer.DeserializeAsync<Snippet>(stream, JsonSerializerDefaults.Options, cancellationToken);
            if (snippet is not null)
            {
                list.Add(snippet);
            }
        }

        return list
            .OrderBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<Snippet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var path = GetPath(id);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<Snippet>(stream, JsonSerializerDefaults.Options, cancellationToken);
    }

    public async Task SaveAsync(Snippet snippet, CancellationToken cancellationToken = default)
    {
        var path = GetPath(snippet.Id);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, snippet, JsonSerializerDefaults.Options, cancellationToken);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var path = GetPath(id);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<SnippetSearchResult>> SearchAsync(SnippetSearchQuery query, CancellationToken cancellationToken = default)
    {
        var snippets = await GetAllAsync(cancellationToken);
        var results = snippets
            .Where(s => !s.Deleted)
            .Where(s => query.IncludeHidden || s.HighlightMode != SnippetHighlightMode.Hidden)
            .Where(s => query.FolderId is null || s.FolderId == query.FolderId.Value)
            .Where(s => string.IsNullOrWhiteSpace(query.Tag) || s.Tags.Any(t => string.Equals(t.Value, query.Tag, StringComparison.OrdinalIgnoreCase)))
            .Select(s => new SnippetSearchResult(s, Score(query, s)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Snippet.Title, StringComparer.OrdinalIgnoreCase)
            .Take(query.Limit)
            .ToList();

        return results;
    }

    private static double Score(SnippetSearchQuery query, Snippet snippet)
    {
        if (string.IsNullOrWhiteSpace(query.Term))
        {
            return 1;
        }

        var term = query.Term.Trim();
        double score = 0;

        if (snippet.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
        {
            score += 5;
        }

        if (snippet.Shortcut.Contains(term, StringComparison.OrdinalIgnoreCase))
        {
            score += 4;
        }

        if (snippet.PlainText.Contains(term, StringComparison.OrdinalIgnoreCase))
        {
            score += 2;
        }

        if (snippet.HtmlText.Contains(term, StringComparison.OrdinalIgnoreCase))
        {
            score += 1;
        }

        if (snippet.Tags.Any(t => t.Value.Contains(term, StringComparison.OrdinalIgnoreCase)))
        {
            score += 2;
        }

        if (!string.IsNullOrWhiteSpace(query.TargetProcess) &&
            snippet.Triggers.Any(t => string.Equals(t.TargetProcess, query.TargetProcess, StringComparison.OrdinalIgnoreCase)))
        {
            score += 1;
        }

        return score;
    }

    private string GetPath(Guid id) => Path.Combine(_options.SnippetsDirectory, $"{id:N}.json");
}
