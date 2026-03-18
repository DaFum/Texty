namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface IFolderRepository
{
    Task<IReadOnlyList<Folder>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(Folder folder, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
