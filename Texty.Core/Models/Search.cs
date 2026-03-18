namespace Texty.Core.Models;

public sealed record SnippetSearchQuery(
    string? Term,
    Guid? FolderId,
    string? Tag,
    string? TargetProcess,
    int Limit = 50,
    bool IncludeHidden = false);

public sealed record SnippetSearchResult(
    Snippet Snippet,
    double Score);
