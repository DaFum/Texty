using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Services;

public sealed class SnippetMaintenanceService
{
    private readonly ISnippetRepository _snippetRepository;

    public SnippetMaintenanceService(ISnippetRepository snippetRepository)
    {
        _snippetRepository = snippetRepository;
    }

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

    public async Task<int> RemoveDuplicatesAsync(CancellationToken cancellationToken = default)
    {
        var snippets = await _snippetRepository.GetAllAsync(cancellationToken);
        var duplicateGroups = snippets
            .GroupBy(s => (s.Title, s.Shortcut, s.PlainText, s.HtmlText))
            .Where(g => g.Count() > 1);

        var deleted = 0;
        foreach (var group in duplicateGroups)
        {
            foreach (var snippet in group
                         .OrderByDescending(s => s.UpdatedUtc)
                         .ThenByDescending(s => s.CreatedUtc)
                         .ThenBy(s => s.Id)
                         .Skip(1))
            {
                await _snippetRepository.DeleteAsync(snippet.Id, cancellationToken);
                deleted++;
            }
        }

        return deleted;
    }
}
