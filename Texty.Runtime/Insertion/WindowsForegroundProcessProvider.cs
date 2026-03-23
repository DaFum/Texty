using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Texty.Runtime.Insertion;

public sealed class WindowsForegroundProcessProvider : IForegroundProcessProvider
{
    public string? GetForegroundProcessName()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return null;
        }

        _ = GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == 0)
        {
            return null;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName + ".exe";
        }
        catch (ArgumentException ex)
        {
            Trace.TraceWarning($"Foreground process lookup failed (invalid pid {processId}): {ex.Message}");
            return null;
        }
        catch (InvalidOperationException ex)
        {
            Trace.TraceWarning($"Foreground process lookup failed (terminated pid {processId}): {ex.Message}");
            return null;
        }
        catch (Win32Exception ex)
        {
            Trace.TraceWarning($"Foreground process lookup failed (win32 pid {processId}): {ex.Message}");
            return null;
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
