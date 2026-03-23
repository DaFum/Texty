namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface ISnippetVersioningService
{
    Task<VersionEntry> SaveAndVersionAsync(Snippet snippet, CancellationToken cancellationToken = default);
}
