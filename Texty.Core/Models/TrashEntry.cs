namespace Texty.Core.Models;

public sealed record TrashEntry(
    Guid Id,
    Snippet Snapshot,
    DateTimeOffset DeletedUtc,
    string? DeletedBy);
