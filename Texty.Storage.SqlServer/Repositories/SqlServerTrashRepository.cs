using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Storage.SqlServer.Repositories;

public sealed class SqlServerTrashRepository : ITrashRepository
{
    private readonly SqlServerStorageState _state;

    public SqlServerTrashRepository(SqlServerStorageState state)
    {
        _state = state;
    }

    public Task<IReadOnlyList<TrashEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TrashEntry> items = _state.Trash.Values
            .OrderByDescending(t => t.DeletedUtc)
            .ToList();

        return Task.FromResult(items);
    }

    public Task MoveToTrashAsync(TrashEntry entry, CancellationToken cancellationToken = default)
    {
        _state.Trash[entry.Id] = entry;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid trashEntryId, CancellationToken cancellationToken = default)
    {
        _state.Trash.TryRemove(trashEntryId, out _);
        return Task.CompletedTask;
    }
}
