namespace Texty.Core.Models;

public sealed record ClipboardItem
{
    private readonly byte[]? _imageBytes;

    public ClipboardItem(string? plainText, string? htmlText, byte[]? imageBytes)
    {
        PlainText = plainText;
        HtmlText = htmlText;
        _imageBytes = imageBytes is null ? null : [.. imageBytes];
    }

    public string? PlainText { get; }

    public string? HtmlText { get; }

    public byte[]? ImageBytes => _imageBytes is null ? null : [.. _imageBytes];
}
