namespace Texty.Core.Models;

public sealed record Folder(
    Guid Id,
    string Name,
    Guid? ParentFolderId,
    string? ColorHex,
    int SortOrder,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);
