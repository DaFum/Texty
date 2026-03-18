namespace Texty.Core.Models;

public sealed record ClipboardItem(
    string? PlainText,
    string? HtmlText,
    byte[]? ImageBytes);
