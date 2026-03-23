using Texty.Core.Models;

namespace Texty.App.ViewModels.Models;

public sealed record SnippetItemModel(
    Guid Id,
    Guid FolderId,
    string Title,
    string Shortcut,
    string ShortcutPreview,
    Snippet Source);
