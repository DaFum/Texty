namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface ISnippetRepository
{
    Task<IReadOnlyList<Snippet>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Snippet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveAsync(Snippet snippet, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SnippetSearchResult>> SearchAsync(SnippetSearchQuery query, CancellationToken cancellationToken = default);
}
