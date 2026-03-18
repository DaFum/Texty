using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Storage.SqlServer.Repositories;

public sealed class SqlServerTrashRepository : ITrashRepository
{
    private readonly SqlServerStorageState _state;

    /// <summary>
    /// Initialisiert eine neue Instanz von <see cref="SqlServerTrashRepository"/> mit dem angegebenen Speicherzustand.
    /// </summary>
    /// <param name="state">Die zugrunde liegende <see cref="SqlServerStorageState"/>, die die Trash-Einträge verwaltet.</param>
    public SqlServerTrashRepository(SqlServerStorageState state)
    {
        _state = state;
    }

    /// <summary>
    /// Gibt alle TrashEntry-Einträge zurück, geordnet nach DeletedUtc absteigend.
    /// </summary>
    /// <returns>Eine IReadOnlyList von TrashEntry-Objekten, geordnet nach DeletedUtc (neueste zuerst).</returns>
    public Task<IReadOnlyList<TrashEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TrashEntry> items = _state.Trash.Values
            .OrderByDescending(t => t.DeletedUtc)
            .ToList();

        return Task.FromResult(items);
    }

    /// <summary>
    /// Fügt einen Trash-Eintrag zum Papierkorb hinzu oder aktualisiert einen bestehenden Eintrag.
    /// </summary>
    /// <param name="entry">Der Eintrag, der in den Papierkorb verschoben oder aktualisiert werden soll.</param>
    public Task MoveToTrashAsync(TrashEntry entry, CancellationToken cancellationToken = default)
    {
        _state.Trash[entry.Id] = entry;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Entfernt einen Eintrag aus dem Papierkorb anhand seiner ID.
    /// </summary>
    /// <param name="trashEntryId">Die ID des zu entfernenden TrashEntry.</param>
    /// <param name="cancellationToken">Abbruchtoken (derzeit nicht verwendet).</param>
    public Task RemoveAsync(Guid trashEntryId, CancellationToken cancellationToken = default)
    {
        _state.Trash.TryRemove(trashEntryId, out _);
        return Task.CompletedTask;
    }
}
