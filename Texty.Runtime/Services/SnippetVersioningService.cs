using System.Collections.Concurrent;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Services;

public sealed class SnippetVersioningService
{
    private readonly ISnippetRepository _snippetRepository;
    private readonly IVersionRepository _versionRepository;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _snippetLocks = new();

    public SnippetVersioningService(ISnippetRepository snippetRepository, IVersionRepository versionRepository)
    {
        _snippetRepository = snippetRepository;
        _versionRepository = versionRepository;
    }

    public async Task<VersionEntry> SaveAndVersionAsync(Snippet snippet, CancellationToken cancellationToken = default)
    {
        var gate = _snippetLocks.GetOrAdd(snippet.Id, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);

        Snippet? previous = null;
        var savedSnippet = false;

        try
        {
            previous = await _snippetRepository.GetByIdAsync(snippet.Id, cancellationToken);

            await _snippetRepository.SaveAsync(snippet, cancellationToken);
            savedSnippet = true;

            var versions = await _versionRepository.GetVersionsAsync(snippet.Id, cancellationToken);
            var nextVersionNumber = versions.Count == 0
                ? 1
                : versions.Max(v => v.VersionNumber) + 1;

            var version = new VersionEntry(
                Guid.NewGuid(),
                snippet.Id,
                nextVersionNumber,
                snippet.PlainText,
                snippet.HtmlText,
                DateTimeOffset.UtcNow,
                snippet.LastEditor);

            await _versionRepository.AddVersionAsync(version, cancellationToken);
            return version;
        }
        catch
        {
            if (savedSnippet)
            {
                try
                {
                    if (previous is null)
                    {
                        await _snippetRepository.DeleteAsync(snippet.Id, CancellationToken.None);
                    }
                    else
                    {
                        await _snippetRepository.SaveAsync(previous, CancellationToken.None);
                    }
                }
                catch
                {
                    // Ignore rollback failures and rethrow the original failure.
                }
            }

            throw;
        }
        finally
        {
            gate.Release();
        }
    }
}
