using Microsoft.UI.Xaml.Media;

namespace Texty.App.ViewModels.Models;

public sealed record FolderItemModel(
    Guid Id,
    string Name,
    SolidColorBrush ColorBrush,
    int SortOrder,
    Guid? ParentFolderId,
    string? ColorHex);
