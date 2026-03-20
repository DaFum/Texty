using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Clipboard;

public sealed class WindowsClipboardGateway : IClipboardGateway
{
    private const uint CfUnicodeText = 13;
    private const uint CfBitmap = 2;
    private const uint GmemMoveable = 0x0002;
    private static readonly object Sync = new();
    private static readonly uint CfHtml = RegisterClipboardFormat("HTML Format");
    private static readonly Regex DataUrlImageRegex = new(
        "<img[^>]+src\\s*=\\s*['\"]data:image/(?<mime>[^;\"']+);base64,(?<data>[^\"']+)['\"]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ImgSrcRegex = new(
        "<img[^>]+src\\s*=\\s*['\"](?<src>[^\"']+)['\"]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public Task<ClipboardItem?> SnapshotAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult<ClipboardItem?>(null);
        }

        lock (Sync)
        {
            if (!TryOpenClipboard())
            {
                return Task.FromResult<ClipboardItem?>(null);
            }

            try
            {
                var plainText = ReadUnicodeText();
                var htmlText = ReadHtmlText();
                var imageBytes = ReadBitmapBytes();
                if (string.IsNullOrWhiteSpace(plainText) &&
                    string.IsNullOrWhiteSpace(htmlText) &&
                    (imageBytes is null || imageBytes.Length == 0))
                {
                    return Task.FromResult<ClipboardItem?>(null);
                }

                return Task.FromResult<ClipboardItem?>(new ClipboardItem(plainText, htmlText, imageBytes));
            }
            finally
            {
                _ = CloseClipboard();
            }
        }
    }

    public Task SetAsync(ClipboardItem clipboardItem, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows())
        {
            return Task.CompletedTask;
        }

        var plainText = clipboardItem.PlainText ?? string.Empty;
        var htmlText = string.IsNullOrWhiteSpace(clipboardItem.HtmlText) ? null : clipboardItem.HtmlText;
        var imageBytes = clipboardItem.ImageBytes;
        if ((imageBytes is null || imageBytes.Length == 0) && !string.IsNullOrWhiteSpace(htmlText))
        {
            imageBytes = TryExtractFirstImageFromHtml(htmlText);
        }

        lock (Sync)
        {
            if (!TryOpenClipboard())
            {
                return Task.CompletedTask;
            }

            try
            {
                if (!EmptyClipboard())
                {
                    return Task.CompletedTask;
                }

                if (!string.IsNullOrEmpty(plainText))
                {
                    _ = TrySetUnicodeText(plainText);
                }
                else if (!string.IsNullOrWhiteSpace(htmlText))
                {
                    // Keep text fallback for plain-text targets.
                    _ = TrySetUnicodeText(ConvertHtmlToPlainText(htmlText));
                }

                if (!string.IsNullOrWhiteSpace(htmlText))
                {
                    _ = TrySetHtmlText(htmlText);
                }

                if (imageBytes is { Length: > 0 })
                {
                    _ = TrySetBitmap(imageBytes);
                }
            }
            finally
            {
                _ = CloseClipboard();
            }
        }

        return Task.CompletedTask;
    }

    public Task RestoreAsync(ClipboardItem? snapshot, CancellationToken cancellationToken = default)
    {
        if (snapshot is null)
        {
            return Task.CompletedTask;
        }

        return SetAsync(snapshot, cancellationToken);
    }

    private static bool TrySetUnicodeText(string text)
    {
        var bytes = Encoding.Unicode.GetBytes(text + "\0");
        var memoryHandle = GlobalAlloc(GmemMoveable, (UIntPtr)bytes.Length);
        if (memoryHandle == IntPtr.Zero)
        {
            return false;
        }

        var target = GlobalLock(memoryHandle);
        if (target == IntPtr.Zero)
        {
            _ = GlobalFree(memoryHandle);
            return false;
        }

        try
        {
            Marshal.Copy(bytes, 0, target, bytes.Length);
        }
        finally
        {
            _ = GlobalUnlock(memoryHandle);
        }

        var setResult = SetClipboardData(CfUnicodeText, memoryHandle);
        if (setResult == IntPtr.Zero)
        {
            _ = GlobalFree(memoryHandle);
            return false;
        }

        return true;
    }

    private static bool TrySetHtmlText(string htmlFragment)
    {
        if (CfHtml == 0)
        {
            return false;
        }

        var payload = BuildCfHtmlPayload(htmlFragment);
        var memoryHandle = GlobalAlloc(GmemMoveable, (UIntPtr)payload.Length);
        if (memoryHandle == IntPtr.Zero)
        {
            return false;
        }

        var target = GlobalLock(memoryHandle);
        if (target == IntPtr.Zero)
        {
            _ = GlobalFree(memoryHandle);
            return false;
        }

        try
        {
            Marshal.Copy(payload, 0, target, payload.Length);
        }
        finally
        {
            _ = GlobalUnlock(memoryHandle);
        }

        var setResult = SetClipboardData(CfHtml, memoryHandle);
        if (setResult == IntPtr.Zero)
        {
            _ = GlobalFree(memoryHandle);
            return false;
        }

        return true;
    }

    private static bool TrySetBitmap(byte[] imageBytes)
    {
        try
        {
            using var ms = new MemoryStream(imageBytes);
            using var image = System.Drawing.Image.FromStream(ms, useEmbeddedColorManagement: false, validateImageData: true);
            using var bitmap = new System.Drawing.Bitmap(image);
            var handle = bitmap.GetHbitmap();
            var setResult = SetClipboardData(CfBitmap, handle);
            if (setResult == IntPtr.Zero)
            {
                _ = DeleteObject(handle);
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? ReadUnicodeText()
    {
        if (!IsClipboardFormatAvailable(CfUnicodeText))
        {
            return null;
        }

        var handle = GetClipboardData(CfUnicodeText);
        if (handle == IntPtr.Zero)
        {
            return null;
        }

        var ptr = GlobalLock(handle);
        if (ptr == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            return Marshal.PtrToStringUni(ptr);
        }
        finally
        {
            _ = GlobalUnlock(handle);
        }
    }

    private static string? ReadHtmlText()
    {
        if (CfHtml == 0 || !IsClipboardFormatAvailable(CfHtml))
        {
            return null;
        }

        var handle = GetClipboardData(CfHtml);
        if (handle == IntPtr.Zero)
        {
            return null;
        }

        var size = GlobalSize(handle);
        if (size == UIntPtr.Zero || (ulong)size > int.MaxValue)
        {
            return null;
        }

        var ptr = GlobalLock(handle);
        if (ptr == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var bytes = new byte[(int)(ulong)size];
            Marshal.Copy(ptr, bytes, 0, bytes.Length);
            var text = DecodeNullTerminatedUtf8(bytes);
            return ExtractHtmlFragment(text);
        }
        finally
        {
            _ = GlobalUnlock(handle);
        }
    }

    private static byte[]? ReadBitmapBytes()
    {
        if (!IsClipboardFormatAvailable(CfBitmap))
        {
            return null;
        }

        var handle = GetClipboardData(CfBitmap);
        if (handle == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            using var bitmap = System.Drawing.Image.FromHbitmap(handle);
            using var ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static string DecodeNullTerminatedUtf8(byte[] bytes)
    {
        var length = Array.IndexOf(bytes, (byte)0);
        if (length < 0)
        {
            length = bytes.Length;
        }

        if (length <= 0)
        {
            return string.Empty;
        }

        try
        {
            return Encoding.UTF8.GetString(bytes, 0, length);
        }
        catch
        {
            return Encoding.Default.GetString(bytes, 0, length);
        }
    }

    private static string? ExtractHtmlFragment(string cfHtml)
    {
        if (string.IsNullOrWhiteSpace(cfHtml))
        {
            return null;
        }

        var startFragment = TryReadOffset(cfHtml, "StartFragment:");
        var endFragment = TryReadOffset(cfHtml, "EndFragment:");
        if (startFragment is not null && endFragment is not null && endFragment > startFragment)
        {
            var bytes = Encoding.UTF8.GetBytes(cfHtml);
            if (startFragment.Value >= 0 &&
                endFragment.Value <= bytes.Length &&
                endFragment.Value > startFragment.Value)
            {
                return Encoding.UTF8.GetString(bytes, startFragment.Value, endFragment.Value - startFragment.Value);
            }
        }

        return cfHtml;
    }

    private static int? TryReadOffset(string text, string token)
    {
        var index = text.IndexOf(token, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return null;
        }

        index += token.Length;
        var end = index;
        while (end < text.Length && char.IsDigit(text[end]))
        {
            end++;
        }

        if (end <= index)
        {
            return null;
        }

        return int.TryParse(text[index..end], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static byte[] BuildCfHtmlPayload(string htmlFragment)
    {
        var normalized = string.IsNullOrWhiteSpace(htmlFragment) ? "<p><br/></p>" : htmlFragment;
        const string pre = "<html><body><!--StartFragment-->";
        const string post = "<!--EndFragment--></body></html>";
        var html = pre + normalized + post;
        const string headerTemplate =
            "Version:1.0\r\n" +
            "StartHTML:{0:0000000000}\r\n" +
            "EndHTML:{1:0000000000}\r\n" +
            "StartFragment:{2:0000000000}\r\n" +
            "EndFragment:{3:0000000000}\r\n";

        var placeholderHeader = string.Format(
            CultureInfo.InvariantCulture,
            headerTemplate,
            0,
            0,
            0,
            0);
        var startHtml = Encoding.UTF8.GetByteCount(placeholderHeader);
        var htmlBytes = Encoding.UTF8.GetBytes(html);
        var startFragment = startHtml + Encoding.UTF8.GetByteCount(pre);
        var endFragment = startFragment + Encoding.UTF8.GetByteCount(normalized);
        var endHtml = startHtml + htmlBytes.Length;

        var header = string.Format(
            CultureInfo.InvariantCulture,
            headerTemplate,
            startHtml,
            endHtml,
            startFragment,
            endFragment);
        return Encoding.UTF8.GetBytes(header + html + '\0');
    }

    private static byte[]? TryExtractFirstImageFromHtml(string html)
    {
        var match = DataUrlImageRegex.Match(html);
        if (match.Success)
        {
            try
            {
                return Convert.FromBase64String(match.Groups["data"].Value);
            }
            catch
            {
                return null;
            }
        }

        var srcMatch = ImgSrcRegex.Match(html);
        if (!srcMatch.Success)
        {
            return null;
        }

        var src = srcMatch.Groups["src"].Value.Trim();
        if (string.IsNullOrWhiteSpace(src))
        {
            return null;
        }

        try
        {
            if (src.StartsWith("file://", StringComparison.OrdinalIgnoreCase) &&
                Uri.TryCreate(src, UriKind.Absolute, out var fileUri) &&
                fileUri.IsFile &&
                File.Exists(fileUri.LocalPath))
            {
                return File.ReadAllBytes(fileUri.LocalPath);
            }

            if (File.Exists(src))
            {
                return File.ReadAllBytes(src);
            }
        }
        catch
        {
            // Ignore image extraction errors and continue with text/html payload.
        }

        return null;
    }

    private static string ConvertHtmlToPlainText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var noTags = Regex.Replace(html, "<[^>]+>", string.Empty);
        return System.Net.WebUtility.HtmlDecode(noTags).Trim();
    }

    private static bool TryOpenClipboard()
    {
        const int maxAttempts = 6;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (OpenClipboard(IntPtr.Zero))
            {
                return true;
            }

            Thread.Sleep(10);
        }

        return false;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterClipboardFormat(string lpszFormat);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern UIntPtr GlobalSize(IntPtr hMem);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr hObject);
}
