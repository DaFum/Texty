namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface ISyncOrchestrator
{
    Task<SyncResult> SyncAsync(string sourceDirectory, string targetDirectory, CancellationToken cancellationToken = default);
}

public interface ISnippetSearchIndex
{
    Task RebuildAsync(IEnumerable<Snippet> snippets, CancellationToken cancellationToken = default);
    Task UpsertAsync(Snippet snippet, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid snippetId, CancellationToken cancellationToken = default);
    IReadOnlyList<SnippetSearchResult> Search(SnippetSearchQuery query);
}
