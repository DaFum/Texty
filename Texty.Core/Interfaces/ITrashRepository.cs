namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface ITrashRepository
{
    Task<IReadOnlyList<TrashEntry>> GetAllAsync(CancellationToken cancellationToken = default);
    Task MoveToTrashAsync(TrashEntry entry, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid trashEntryId, CancellationToken cancellationToken = default);
}
