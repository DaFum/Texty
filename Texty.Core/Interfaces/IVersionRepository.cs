namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface IVersionRepository
{
    /// <summary>
/// Liefert die Versionseinträge für das angegebene Snippet.
/// </summary>
/// <param name="snippetId">Die eindeutige Kennung des Snippets.</param>
/// <param name="cancellationToken">Token zum Abbrechen der Operation.</param>
/// <returns>Eine schreibgeschützte Liste der VersionEntry-Objekte, die dem Snippet zugeordnet sind.</returns>
Task<IReadOnlyList<VersionEntry>> GetVersionsAsync(Guid snippetId, CancellationToken cancellationToken = default);
    /// <summary>
/// Speichert einen Versions-Eintrag für ein Snippet im Repository.
/// </summary>
/// <param name="version">Der zu speichernde Versions-Eintrag.</param>
/// <param name="cancellationToken">Token zum Abbrechen des asynchronen Vorgangs.</param>
Task AddVersionAsync(VersionEntry version, CancellationToken cancellationToken = default);
}
