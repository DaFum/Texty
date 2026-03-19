namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface ISnippetWorkflowService
{
    Task<Snippet?> SaveAsync(Snippet snippet, bool createVersion = true, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid snippetId, CancellationToken cancellationToken = default);
    Task<Snippet?> RestoreAsync(Snippet snapshot, CancellationToken cancellationToken = default);
    Task<Snippet?> RollbackToVersionAsync(Guid snippetId, VersionEntry version, string? editor, CancellationToken cancellationToken = default);
    Task<SnippetReplaceResult> ReplaceAsync(SnippetReplaceRequest request, string? editor, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SnippetSearchResult>> SearchAsync(SnippetSearchQuery query, CancellationToken cancellationToken = default);
}
