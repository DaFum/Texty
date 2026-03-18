namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface ITrashRepository
{
    /// <summary>
/// Gibt alle Einträge des Papierkorbs zurück.
/// </summary>
/// <returns>Eine schreibgeschützte Liste aller vorhandenen <see cref="TrashEntry"/>-Einträge; kann leer sein.</returns>
Task<IReadOnlyList<TrashEntry>> GetAllAsync(CancellationToken cancellationToken = default);
    /// <summary>
/// Verschiebt den angegebenen Eintrag in den Papierkorb.
/// </summary>
/// <param name="entry">Der zu verschiebende TrashEntry.</param>
Task MoveToTrashAsync(TrashEntry entry, CancellationToken cancellationToken = default);
    /// <summary>
/// Entfernt den Trash-Eintrag mit der angegebenen ID aus dem Papierkorb.
/// </summary>
/// <param name="trashEntryId">Die eindeutige ID (GUID) des zu löschenden Trash-Eintrags.</param>
Task RemoveAsync(Guid trashEntryId, CancellationToken cancellationToken = default);
}
