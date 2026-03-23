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

public sealed record SnippetReplaceScope(
    Guid? FolderId,
    string? Tag,
    string? TargetProcess,
    IReadOnlyList<Guid>? SnippetIds,
    bool IncludeHidden = false);

public sealed record SnippetReplaceRequest(
    string FindText,
    string ReplaceText,
    SnippetReplaceScope Scope);

public sealed record SnippetReplaceResult(
    int Inspected,
    int Updated,
    IReadOnlyList<Guid> UpdatedSnippetIds,
    IReadOnlyList<string> Errors);
