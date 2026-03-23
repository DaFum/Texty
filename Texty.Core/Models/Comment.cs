namespace Texty.Core.Models;

public sealed record Comment(
    Guid Id,
    Guid SnippetId,
    string Author,
    string Body,
    DateTimeOffset CreatedUtc);
