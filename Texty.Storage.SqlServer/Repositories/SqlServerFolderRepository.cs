using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Storage.SqlServer.Repositories;

public sealed class SqlServerFolderRepository : IFolderRepository
{
    private readonly SqlServerStorageState _state;

    public SqlServerFolderRepository(SqlServerStorageState state)
    {
        _state = state;
    }

    public Task<IReadOnlyList<Folder>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Folder> folders = _state.Folders.Values
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(folders);
    }

    public Task SaveAsync(Folder folder, CancellationToken cancellationToken = default)
    {
        _state.Folders[folder.Id] = folder;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _state.Folders.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
