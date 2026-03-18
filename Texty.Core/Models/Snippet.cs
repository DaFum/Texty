namespace Texty.Core.Models;

public sealed record Snippet(
    Guid Id,
    Guid FolderId,
    string Title,
    string Shortcut,
    string PlainText,
    string HtmlText,
    IReadOnlyList<Tag> Tags,
    IReadOnlyList<Comment> Comments,
    IReadOnlyList<TriggerRule> Triggers,
    SnippetTemplate? Template,
    SnippetHighlightMode HighlightMode,
    string? FontFamily,
    bool Deleted,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc,
    string? LastEditor);
