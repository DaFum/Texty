using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Storage.SqlServer.Repositories;

public sealed class SqlServerVersionRepository : IVersionRepository
{
    private readonly SqlServerStorageState _state;

    public SqlServerVersionRepository(SqlServerStorageState state)
    {
        _state = state;
    }

    public Task<IReadOnlyList<VersionEntry>> GetVersionsAsync(Guid snippetId, CancellationToken cancellationToken = default)
    {
        if (!_state.Versions.TryGetValue(snippetId, out var values))
        {
            return Task.FromResult<IReadOnlyList<VersionEntry>>(Array.Empty<VersionEntry>());
        }

        IReadOnlyList<VersionEntry> ordered = values
            .OrderByDescending(v => v.VersionNumber)
            .ThenByDescending(v => v.CreatedUtc)
            .ToList();

        return Task.FromResult(ordered);
    }

    public Task AddVersionAsync(VersionEntry version, CancellationToken cancellationToken = default)
    {
        var list = _state.Versions.GetOrAdd(version.SnippetId, _ => []);
        lock (list)
        {
            list.Add(version);
        }

        return Task.CompletedTask;
    }
}
