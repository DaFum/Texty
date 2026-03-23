namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface IVersionRepository
{
    Task<IReadOnlyList<VersionEntry>> GetVersionsAsync(Guid snippetId, CancellationToken cancellationToken = default);
    Task AddVersionAsync(VersionEntry version, CancellationToken cancellationToken = default);
}
