using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Services;

public sealed class SnippetMaintenanceService
{
    private readonly ISnippetRepository _snippetRepository;

    /// <summary>
    /// Initialisiert eine neue Instanz von <see cref="SnippetMaintenanceService"/> mit dem angegebenen Snippet-Repository.
    /// </summary>
    /// <param name="snippetRepository">Repository zum Lesen, Speichern und Löschen von Snippets.</param>
    public SnippetMaintenanceService(ISnippetRepository snippetRepository)
    {
        _snippetRepository = snippetRepository;
    }

    /// <summary>
    /// Setzt die Schriftfamilie und das UpdatedUtc-Feld für die angegebenen Snippets.
    /// </summary>
    /// <param name="snippetIds">Auflistung von Snippet-IDs, die aktualisiert werden sollen.</param>
    /// <param name="fontFamily">Die neue Schriftfamilie, die auf die Snippets angewendet wird.</param>
    /// <returns>Die Anzahl der tatsächlich aktualisierten Snippets.</returns>
    public async Task<int> BulkSetFontAsync(IEnumerable<Guid> snippetIds, string fontFamily, CancellationToken cancellationToken = default)
    {
        var idSet = snippetIds.ToHashSet();
        var snippets = await _snippetRepository.GetAllAsync(cancellationToken);
        var updatedCount = 0;

        foreach (var snippet in snippets.Where(s => idSet.Contains(s.Id)))
        {
            await _snippetRepository.SaveAsync(
                snippet with
                {
                    FontFamily = fontFamily,
                    UpdatedUtc = DateTimeOffset.UtcNow,
                },
                cancellationToken);
            updatedCount++;
        }

        return updatedCount;
    }

    /// <summary>
    /// Entfernt doppelte Snippets, die in Title, Shortcut, PlainText und HtmlText identisch sind.
    /// </summary>
    /// <param name="cancellationToken">Token zum Abbrechen des Vorgangs.</param>
    /// <returns>Die Anzahl der gelöschten (zusätzlichen) Duplikate. In jeder Duplikatgruppe bleibt das erste Element erhalten; alle weiteren werden gelöscht.</returns>
    public async Task<int> RemoveDuplicatesAsync(CancellationToken cancellationToken = default)
    {
        var snippets = await _snippetRepository.GetAllAsync(cancellationToken);
        var duplicateGroups = snippets
            .GroupBy(s => $"{s.Title}|{s.Shortcut}|{s.PlainText}|{s.HtmlText}", StringComparer.Ordinal)
            .Where(g => g.Count() > 1);

        var deleted = 0;
        foreach (var group in duplicateGroups)
        {
            foreach (var snippet in group.Skip(1))
            {
                await _snippetRepository.DeleteAsync(snippet.Id, cancellationToken);
                deleted++;
            }
        }

        return deleted;
    }
}
