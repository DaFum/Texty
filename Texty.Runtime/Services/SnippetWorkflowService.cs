using System.Diagnostics;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Services;

public sealed class SnippetWorkflowService : ISnippetWorkflowService
{
    private readonly ISnippetRepository _snippetRepository;
    private readonly ITrashRepository _trashRepository;
    private readonly ISnippetSearchIndex _searchIndex;
    private readonly ISnippetVersioningService _versioningService;

    public SnippetWorkflowService(
        ISnippetRepository snippetRepository,
        ITrashRepository trashRepository,
        ISnippetSearchIndex searchIndex,
        ISnippetVersioningService versioningService)
    {
        _snippetRepository = snippetRepository;
        _trashRepository = trashRepository;
        _searchIndex = searchIndex;
        _versioningService = versioningService;
    }

    public async Task<Snippet?> SaveAsync(Snippet snippet, bool createVersion = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (createVersion)
        {
            await _versioningService.SaveAndVersionAsync(snippet, cancellationToken);
        }
        else
        {
            await _snippetRepository.SaveAsync(snippet, cancellationToken);
        }

        try
        {
            await _searchIndex.UpsertAsync(snippet, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Search index upsert failed for snippet '{snippet.Id}': {ex.Message}");
        }

        return snippet;
    }

    public async Task DeleteAsync(Guid snippetId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var snapshot = await _snippetRepository.GetByIdAsync(snippetId, cancellationToken);
        await _snippetRepository.DeleteAsync(snippetId, cancellationToken);
        try
        {
            await _searchIndex.RemoveAsync(snippetId, cancellationToken);
        }
        catch
        {
            if (snapshot is not null)
            {
                try
                {
                    await _snippetRepository.SaveAsync(snapshot, CancellationToken.None);
                }
                catch (Exception rollbackEx)
                {
                    Trace.TraceError($"Failed to rollback snippet delete for '{snippetId}': {rollbackEx}");
                }
            }

            throw;
        }
    }

    public async Task MoveToTrashAsync(TrashEntry entry, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _trashRepository.MoveToTrashAsync(entry, cancellationToken);

        try
        {
            await DeleteAsync(entry.Snapshot.Id, cancellationToken);
        }
        catch
        {
            try
            {
                await _trashRepository.RemoveAsync(entry.Id, CancellationToken.None);
            }
            catch (Exception rollbackEx)
            {
                Trace.TraceError($"Failed to rollback trash entry '{entry.Id}': {rollbackEx}");
            }

            throw;
        }
    }

    public async Task<Snippet?> RestoreAsync(Snippet snapshot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var restored = snapshot with
        {
            Deleted = false,
            UpdatedUtc = DateTimeOffset.UtcNow,
        };

        await SaveAsync(restored, createVersion: true, cancellationToken);
        return restored;
    }

    public async Task<Snippet?> RollbackToVersionAsync(Guid snippetId, VersionEntry version, string? editor, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (version.SnippetId != snippetId)
        {
            throw new ArgumentException("Version entry does not match requested snippet id.", nameof(version));
        }

        var current = await _snippetRepository.GetByIdAsync(snippetId, cancellationToken);
        if (current is null)
        {
            return null;
        }

        var rolledBack = current with
        {
            PlainText = version.PlainText,
            HtmlText = version.HtmlText,
            UpdatedUtc = DateTimeOffset.UtcNow,
            LastEditor = editor,
        };

        await SaveAsync(rolledBack, createVersion: true, cancellationToken);
        return rolledBack;
    }

    public async Task<SnippetReplaceResult> ReplaceAsync(
        SnippetReplaceRequest request,
        string? editor,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.FindText))
        {
            return new SnippetReplaceResult(0, 0, [], ["FindText must not be empty."]);
        }

        var all = await _snippetRepository.GetAllAsync(cancellationToken);
        var candidates = FilterByScope(all, request.Scope);
        var updatedSnippetIds = new List<Guid>();
        var errors = new List<string>();
        var now = DateTimeOffset.UtcNow;

        foreach (var snippet in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var newTitle = snippet.Title.Replace(request.FindText, request.ReplaceText, StringComparison.OrdinalIgnoreCase);
            var newPlain = snippet.PlainText.Replace(request.FindText, request.ReplaceText, StringComparison.OrdinalIgnoreCase);
            var newHtml = snippet.HtmlText.Replace(request.FindText, request.ReplaceText, StringComparison.OrdinalIgnoreCase);

            if (newTitle == snippet.Title && newPlain == snippet.PlainText && newHtml == snippet.HtmlText)
            {
                continue;
            }

            var updated = snippet with
            {
                Title = newTitle,
                PlainText = newPlain,
                HtmlText = newHtml,
                UpdatedUtc = now,
                LastEditor = editor,
            };

            try
            {
                await SaveAsync(updated, createVersion: true, cancellationToken);
                updatedSnippetIds.Add(updated.Id);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                errors.Add($"{snippet.Id}: {ex.Message}");
            }
        }

        return new SnippetReplaceResult(
            candidates.Count,
            updatedSnippetIds.Count,
            updatedSnippetIds,
            errors);
    }

    public Task<IReadOnlyList<SnippetSearchResult>> SearchAsync(SnippetSearchQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_searchIndex.Search(query));
    }

    private static IReadOnlyList<Snippet> FilterByScope(IReadOnlyList<Snippet> snippets, SnippetReplaceScope scope)
    {
        var selectedIds = scope.SnippetIds?.Count > 0
            ? scope.SnippetIds.ToHashSet()
            : null;

        return snippets
            .Where(s => !s.Deleted)
            .Where(s => scope.IncludeHidden || s.HighlightMode != SnippetHighlightMode.Hidden)
            .Where(s => scope.FolderId is null || s.FolderId == scope.FolderId.Value)
            .Where(s => string.IsNullOrWhiteSpace(scope.Tag) || s.Tags.Any(t => string.Equals(t.Value, scope.Tag, StringComparison.OrdinalIgnoreCase)))
            .Where(s => string.IsNullOrWhiteSpace(scope.TargetProcess) ||
                        s.Triggers.Any(t =>
                            t.Enabled &&
                            !string.IsNullOrWhiteSpace(t.TargetProcess) &&
                            string.Equals(
                                NormalizeProcessName(t.TargetProcess),
                                NormalizeProcessName(scope.TargetProcess),
                                StringComparison.OrdinalIgnoreCase)))
            .Where(s => selectedIds is null || selectedIds.Contains(s.Id))
            .ToList();
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
