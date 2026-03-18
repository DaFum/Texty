namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface IFolderRepository
{
    /// <summary>
/// Gibt alle vorhandenen Folder als schreibgeschützte Liste zurück.
/// </summary>
/// <returns>Eine schreibgeschützte Liste aller gespeicherten <see cref="Folder"/>-Instanzen.</returns>
Task<IReadOnlyList<Folder>> GetAllAsync(CancellationToken cancellationToken = default);
    /// <summary>
/// Speichert das angegebene Folder-Objekt in der Persistenzschicht.
/// </summary>
/// <param name="folder">Das zu speichernde Folder-Objekt.</param>
Task SaveAsync(Folder folder, CancellationToken cancellationToken = default);
    /// <summary>
/// Löscht den Ordner mit der angegebenen ID.
/// </summary>
/// <param name="id">Die eindeutige Kennung des zu löschenden Ordners.</param>
Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
