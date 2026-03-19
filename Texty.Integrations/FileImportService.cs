using System.Text.Json;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Integrations;

public sealed class FileImportService : IImportService
{
    private readonly ISnippetRepository _snippetRepository;
    private readonly Guid _defaultFolderId;
    private readonly string? _assetsDirectory;
    private readonly IAuditLogger? _auditLogger;

    public FileImportService(
        ISnippetRepository snippetRepository,
        Guid defaultFolderId,
        string? assetsDirectory = null,
        IAuditLogger? auditLogger = null)
    {
        _snippetRepository = snippetRepository;
        _defaultFolderId = defaultFolderId;
        _assetsDirectory = assetsDirectory;
        _auditLogger = auditLogger;
        if (!string.IsNullOrWhiteSpace(_assetsDirectory))
        {
            Directory.CreateDirectory(_assetsDirectory);
        }
    }

    public async Task<ImportResult> ImportAsync(string sourcePath, string format, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var warnings = new List<string>();
        if (!File.Exists(sourcePath))
        {
            warnings.Add($"Source not found: {sourcePath}");
            await WriteAuditAsync(
                correlationId,
                false,
                AuditDecision.Error,
                $"Import source not found: {sourcePath}",
                cancellationToken);
            return new ImportResult(0, warnings);
        }

        var extension = Path.GetExtension(sourcePath);
        var normalizedFormat = format.Trim().ToLowerInvariant();
        if (normalizedFormat is not ("text" or "html" or "image" or "outlook" or "textexpander"))
        {
            warnings.Add($"Unsupported format '{format}'.");
            await WriteAuditAsync(
                correlationId,
                false,
                AuditDecision.Deny,
                $"Import format not supported: {format}",
                cancellationToken);
            return new ImportResult(0, warnings);
        }

        var created = 0;
        var now = DateTimeOffset.UtcNow;
        switch (normalizedFormat)
        {
            case "text":
            case "html":
            {
                var content = await File.ReadAllTextAsync(sourcePath, cancellationToken);
                var snippet = CreateSnippet(now, sourcePath, content, string.Empty);
                if (normalizedFormat == "html" || extension.Equals(".html", StringComparison.OrdinalIgnoreCase))
                {
                    snippet = snippet with { HtmlText = content };
                }

                await _snippetRepository.SaveAsync(snippet, cancellationToken);
                created++;
                await WriteAuditAsync(correlationId, true, AuditDecision.Allow, $"Imported {normalizedFormat} snippet.", cancellationToken);
                break;
            }
            case "image":
            {
                if (!IsSupportedImageExtension(extension))
                {
                    warnings.Add($"Unsupported image extension '{extension}'.");
                    return new ImportResult(0, warnings);
                }

                var imagePath = sourcePath;
                if (!string.IsNullOrWhiteSpace(_assetsDirectory))
                {
                    var targetFileName = $"{Guid.NewGuid():N}{extension}";
                    var targetPath = Path.Combine(_assetsDirectory, targetFileName);
                    File.Copy(sourcePath, targetPath, overwrite: true);
                    imagePath = targetPath;
                }

                var imageUri = new Uri(imagePath, UriKind.Absolute).AbsoluteUri;
                var html = $"<p><img src=\"{imageUri}\" alt=\"{Path.GetFileName(sourcePath)}\" /></p>";
                var plain = $"[Image] {Path.GetFileName(sourcePath)}";
                var imageSnippet = CreateSnippet(now, sourcePath, plain, html);
                await _snippetRepository.SaveAsync(imageSnippet, cancellationToken);
                created++;
                await WriteAuditAsync(correlationId, true, AuditDecision.Allow, "Imported image snippet.", cancellationToken);
                break;
            }
            case "outlook":
            {
                var content = await File.ReadAllTextAsync(sourcePath, cancellationToken);
                var body = ExtractOutlookBody(content);
                var title = ExtractOutlookSubject(content) ?? Path.GetFileNameWithoutExtension(sourcePath);
                var snippet = CreateSnippet(now, sourcePath, body, $"<pre>{System.Net.WebUtility.HtmlEncode(body)}</pre>") with { Title = title };
                await _snippetRepository.SaveAsync(snippet, cancellationToken);
                created++;
                await WriteAuditAsync(correlationId, true, AuditDecision.Allow, "Imported outlook snippet.", cancellationToken);
                break;
            }
            case "textexpander":
            {
                var content = await File.ReadAllTextAsync(sourcePath, cancellationToken);
                if (TryParseTextExpanderSnippet(content, sourcePath, now, out var snippet))
                {
                    await _snippetRepository.SaveAsync(snippet, cancellationToken);
                    created++;
                    await WriteAuditAsync(correlationId, true, AuditDecision.Allow, "Imported TextExpander snippet.", cancellationToken);
                }
                else
                {
                    warnings.Add("TextExpander format could not be parsed; imported as plain text.");
                    var fallback = CreateSnippet(now, sourcePath, content, string.Empty);
                    await _snippetRepository.SaveAsync(fallback, cancellationToken);
                    created++;
                    await WriteAuditAsync(correlationId, true, AuditDecision.Error, "TextExpander parse failed; imported as plain text.", cancellationToken);
                }

                break;
            }
        }

        return new ImportResult(created, warnings);
    }

    private Snippet CreateSnippet(DateTimeOffset now, string sourcePath, string plainText, string htmlText)
    {
        return new Snippet(
            Guid.NewGuid(),
            _defaultFolderId,
            Path.GetFileNameWithoutExtension(sourcePath),
            string.Empty,
            plainText,
            htmlText,
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
    }

    private static bool IsSupportedImageExtension(string extension)
    {
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".gif", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractOutlookBody(string content)
    {
        var marker = content.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        if (marker >= 0)
        {
            return content[(marker + 4)..].Trim();
        }

        marker = content.IndexOf("\n\n", StringComparison.Ordinal);
        if (marker >= 0)
        {
            return content[(marker + 2)..].Trim();
        }

        return content;
    }

    private static string? ExtractOutlookSubject(string content)
    {
        foreach (var line in content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("Subject:", StringComparison.OrdinalIgnoreCase))
            {
                return line["Subject:".Length..].Trim();
            }
        }

        return null;
    }

    private bool TryParseTextExpanderSnippet(string content, string sourcePath, DateTimeOffset now, out Snippet snippet)
    {
        snippet = default!;

        try
        {
            using var json = JsonDocument.Parse(content);
            var root = json.RootElement;

            if (root.TryGetProperty("abbreviation", out var abbreviationElement) &&
                root.TryGetProperty("content", out var contentElement))
            {
                var abbreviation = abbreviationElement.GetString() ?? string.Empty;
                var body = contentElement.GetString() ?? string.Empty;

                snippet = new Snippet(
                    Guid.NewGuid(),
                    _defaultFolderId,
                    Path.GetFileNameWithoutExtension(sourcePath),
                    abbreviation,
                    body,
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
                return true;
            }
        }
        catch
        {
            // Fallback to plain text import.
        }

        return false;
    }

    private Task WriteAuditAsync(
        string correlationId,
        bool success,
        AuditDecision decision,
        string message,
        CancellationToken cancellationToken)
    {
        if (_auditLogger is null)
        {
            return Task.CompletedTask;
        }

        return _auditLogger.WriteAsync(
            new AuditLogEntry(
                DateTimeOffset.UtcNow,
                AuditCategory.Import,
                "import.file",
                decision,
                correlationId,
                success,
                message),
            cancellationToken);
    }
}
