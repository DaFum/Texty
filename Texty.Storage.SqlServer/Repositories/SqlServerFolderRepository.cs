using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Storage.SqlServer.Repositories;

public sealed class SqlServerFolderRepository : IFolderRepository
{
    private readonly SqlServerStorageState _state;

    /// <summary>
    /// Erstellt eine neue Instanz von <see cref="SqlServerFolderRepository"/> und verwendet den übergebenen Zustand als zugrundeliegenden Ordner-Speicher.
    /// </summary>
    /// <param name="state">In-Memory-Zustand (<see cref="SqlServerStorageState"/>), der die gespeicherten Ordner verwaltet.</param>
    public SqlServerFolderRepository(SqlServerStorageState state)
    {
        _state = state;
    }

    /// <summary>
    /// Liefert alle Ordner aus dem internen Zustand in sortierter Reihenfolge.
    /// </summary>
    /// <returns>Eine schreibgeschützte Liste aller Ordner, sortiert zuerst nach SortOrder und anschließend nach Name (ordinal, ohne Beachtung der Groß-/Kleinschreibung).</returns>
    public Task<IReadOnlyList<Folder>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Folder> folders = _state.Folders.Values
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(folders);
    }

    /// <summary>
    /// Speichert einen Ordner im internen SQL-Server‑Zustand oder aktualisiert ihn, falls bereits vorhanden.
    /// </summary>
    /// <param name="folder">Der zu speichernde oder zu aktualisierende Ordner; seine Id wird als Schlüssel verwendet.</param>
    public Task SaveAsync(Folder folder, CancellationToken cancellationToken = default)
    {
        _state.Folders[folder.Id] = folder;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Entfernt den Ordner mit der angegebenen ID aus dem Speicher.
    /// </summary>
    /// <param name="id">Die eindeutige Kennung des zu löschenden Ordners.</param>
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _state.Folders.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
