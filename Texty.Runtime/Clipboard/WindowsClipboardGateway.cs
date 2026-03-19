using System.Runtime.InteropServices;
using System.Text;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Clipboard;

public sealed class WindowsClipboardGateway : IClipboardGateway
{
    private const uint CfUnicodeText = 13;
    private const uint GmemMoveable = 0x0002;
    private static readonly object Sync = new();

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
                if (!IsClipboardFormatAvailable(CfUnicodeText))
                {
                    return Task.FromResult<ClipboardItem?>(null);
                }

                var handle = GetClipboardData(CfUnicodeText);
                if (handle == IntPtr.Zero)
                {
                    return Task.FromResult<ClipboardItem?>(null);
                }

                var ptr = GlobalLock(handle);
                if (ptr == IntPtr.Zero)
                {
                    return Task.FromResult<ClipboardItem?>(null);
                }

                try
                {
                    var text = Marshal.PtrToStringUni(ptr);
                    return Task.FromResult<ClipboardItem?>(new ClipboardItem(text, null, null));
                }
                finally
                {
                    _ = GlobalUnlock(handle);
                }
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

        var text = clipboardItem.PlainText ?? clipboardItem.HtmlText ?? string.Empty;
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

                var bytes = Encoding.Unicode.GetBytes(text + "\0");
                var memoryHandle = GlobalAlloc(GmemMoveable, (UIntPtr)bytes.Length);
                if (memoryHandle == IntPtr.Zero)
                {
                    return Task.CompletedTask;
                }

                var target = GlobalLock(memoryHandle);
                if (target == IntPtr.Zero)
                {
                    _ = GlobalFree(memoryHandle);
                    return Task.CompletedTask;
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

    private static bool TryOpenClipboard()
    {
        const int maxAttempts = 5;
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

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);
}
