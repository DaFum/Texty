using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Integrations;

public sealed class FileImportService : IImportService
{
    private readonly ISnippetRepository _snippetRepository;
    private readonly Guid _defaultFolderId;

    /// <summary>
    /// Initialisiert eine neue Instanz von FileImportService mit dem angegebenen Snippet-Repository und Standardordner.
    /// </summary>
    /// <param name="defaultFolderId">Die GUID des Zielordners, in den importierte Snippets standardmäßig abgelegt werden.</param>
    public FileImportService(ISnippetRepository snippetRepository, Guid defaultFolderId)
    {
        _snippetRepository = snippetRepository;
        _defaultFolderId = defaultFolderId;
    }

    /// <summary>
    /// Importiert die angegebene Datei als Snippet und speichert das Ergebnis im Snippet-Repository.
    /// </summary>
    /// <param name="sourcePath">Der Pfad zur Quelldatei, deren Inhalt als Snippet importiert wird.</param>
    /// <param name="format">Das Importformat; unterstützte Werte sind "text", "image", "outlook" und "textexpander".</param>
    /// <returns>Ein ImportResult mit der Anzahl importierter Snippets (0 oder 1) und einer Liste möglicher Warnungen.</returns>
    public async Task<ImportResult> ImportAsync(string sourcePath, string format, CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        if (!File.Exists(sourcePath))
        {
            warnings.Add($"Source not found: {sourcePath}");
            return new ImportResult(0, warnings);
        }

        var extension = Path.GetExtension(sourcePath);
        var normalizedFormat = format.Trim().ToLowerInvariant();
        if (normalizedFormat is not ("text" or "image" or "outlook" or "textexpander"))
        {
            warnings.Add($"Unsupported format '{format}'.");
            return new ImportResult(0, warnings);
        }

        var now = DateTimeOffset.UtcNow;
        var snippet = new Snippet(
            Guid.NewGuid(),
            _defaultFolderId,
            Path.GetFileNameWithoutExtension(sourcePath),
            string.Empty,
            await File.ReadAllTextAsync(sourcePath, cancellationToken),
            string.Empty,
            [],
            [],
            [],
            null,
            SnippetHighlightMode.None,
            null,
            false,
            now,
            now,
            "import");

        if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase))
        {
            snippet = snippet with { HtmlText = snippet.PlainText };
        }

        await _snippetRepository.SaveAsync(snippet, cancellationToken);
        return new ImportResult(1, warnings);
    }
}
