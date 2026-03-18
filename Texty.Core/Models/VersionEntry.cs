namespace Texty.Core.Models;

public sealed record VersionEntry(
    Guid Id,
    Guid SnippetId,
    int VersionNumber,
    string PlainText,
    string HtmlText,
    DateTimeOffset CreatedUtc,
    string? CreatedBy);
