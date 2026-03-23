using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using Texty.Core.Interfaces;
using Texty.Core.Models;
using Texty.Runtime.Insertion;

namespace Texty.Runtime.Triggering;

public sealed class WindowsKeyboardHookTriggerProvider : ITriggerProvider, IDisposable
{
    private readonly IForegroundProcessProvider _foregroundProcessProvider;
    private readonly Channel<TriggerSignal> _signals = Channel.CreateUnbounded<TriggerSignal>();
    private readonly object _sync = new();
    private readonly StringBuilder _textBuffer = new();

    private Thread? _hookThread;
    private IntPtr _hookHandle;
    private LowLevelKeyboardProc? _hookProc;
    private uint _hookThreadId;
    private bool _disposed;
    private int _lastHotkey;
    private long _lastHotkeyTick;

    public WindowsKeyboardHookTriggerProvider(IForegroundProcessProvider foregroundProcessProvider)
    {
        _foregroundProcessProvider = foregroundProcessProvider;
    }

    public string Name => "WindowsKeyboardHook";

    public IAsyncEnumerable<TriggerSignal> ListenAsync(CancellationToken cancellationToken = default)
    {
        EnsureStarted();
        return _signals.Reader.ReadAllAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopHookThread();
        _signals.Writer.TryComplete();
    }

    private void EnsureStarted()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        lock (_sync)
        {
            if (_hookThread is not null)
            {
                return;
            }

            _hookThread = new Thread(HookThreadMain)
            {
                IsBackground = true,
                Name = "Texty-HotkeyHook",
            };
            _hookThread.SetApartmentState(ApartmentState.STA);
            _hookThread.Start();
        }
    }

    private void HookThreadMain()
    {
        _hookThreadId = GetCurrentThreadId();
        _hookProc = HookCallback;
        var moduleHandle = GetModuleHandle(null);
        _hookHandle = SetWindowsHookEx(WhKeyboardLl, _hookProc, moduleHandle, 0);
        if (_hookHandle == IntPtr.Zero)
        {
            _signals.Writer.TryComplete(new InvalidOperationException("Failed to install low-level keyboard hook."));
            return;
        }

        while (GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
        {
            _ = TranslateMessage(ref message);
            _ = DispatchMessage(ref message);
        }

        _ = UnhookWindowsHookEx(_hookHandle);
    }

    private void StopHookThread()
    {
        lock (_sync)
        {
            if (_hookThreadId != 0)
            {
                _ = PostThreadMessage(_hookThreadId, WmQuit, IntPtr.Zero, IntPtr.Zero);
            }

            if (_hookThread is not null && _hookThread.IsAlive)
            {
                _hookThread.Join(TimeSpan.FromSeconds(1));
            }
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 &&
            (wParam == (IntPtr)WmKeyDown || wParam == (IntPtr)WmSysKeyDown))
        {
            foreach (var signal in CreateSignals(lParam))
            {
                _signals.Writer.TryWrite(signal);
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private IReadOnlyList<TriggerSignal> CreateSignals(IntPtr lParam)
    {
        var signals = new List<TriggerSignal>(2);
        var info = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
        if ((info.flags & LlkhfInjected) != 0 || IsModifierKey(info.vkCode))
        {
            return signals;
        }

        var key = MapVirtualKeyToToken(info.vkCode);
        if (string.IsNullOrWhiteSpace(key))
        {
            return signals;
        }

        var parts = new List<string>(4);
        if (IsPressed(VkControl))
        {
            parts.Add("CTRL");
        }

        if (IsPressed(VkShift))
        {
            parts.Add("SHIFT");
        }

        if (IsPressed(VkMenu))
        {
            parts.Add("ALT");
        }

        if (IsPressed(VkLWin) || IsPressed(VkRWin))
        {
            parts.Add("WIN");
        }

        var processName = _foregroundProcessProvider.GetForegroundProcessName();
        var scope = TriggerScopeClassifier.Classify(processName);
        var hasModifier = parts.Count > 0;
        if (hasModifier || IsFunctionKeyToken(key))
        {
            var now = Environment.TickCount64;
            if (_lastHotkey == (int)info.vkCode && now - _lastHotkeyTick < 100)
            {
                return signals;
            }

            _lastHotkey = (int)info.vkCode;
            _lastHotkeyTick = now;

            parts.Add(key);
            var hotkey = string.Join("+", parts);
            signals.Add(new TriggerSignal(TriggerType.Hotkey, hotkey, processName, scope));
            return signals;
        }

        if (!TryUpdateTextBuffer(info.vkCode, key, out var textInput))
        {
            return signals;
        }

        signals.Add(new TriggerSignal(TriggerType.Autotext, textInput, processName, scope));
        signals.Add(new TriggerSignal(TriggerType.Regex, textInput, processName, scope));
        return signals;
    }

    private bool TryUpdateTextBuffer(uint virtualKey, string token, out string value)
    {
        lock (_textBuffer)
        {
            if (virtualKey == 0x08) // BACKSPACE
            {
                if (_textBuffer.Length > 0)
                {
                    _textBuffer.Length--;
                }

                value = _textBuffer.ToString();
                return !string.IsNullOrWhiteSpace(value);
            }

            if (IsDelimiterToken(token))
            {
                value = _textBuffer.ToString();
                _textBuffer.Clear();
                return !string.IsNullOrWhiteSpace(value);
            }

            var charToken = TryConvertToText(token);
            if (charToken is null)
            {
                value = _textBuffer.ToString();
                return false;
            }

            _textBuffer.Append(charToken.Value);
            if (_textBuffer.Length > 256)
            {
                _textBuffer.Remove(0, _textBuffer.Length - 256);
            }

            value = _textBuffer.ToString();
            return !string.IsNullOrWhiteSpace(value);
        }
    }

    private static bool IsDelimiterToken(string token)
    {
        return token is "SPACE" or "ENTER" or "TAB" or "." or "," or ";" or ":" or "!" or "?";
    }

    private static char? TryConvertToText(string token)
    {
        if (token.Length != 1)
        {
            return null;
        }

        var ch = token[0];
        if (char.IsLetter(ch))
        {
            var useUpper = IsPressed(VkShift) ^ IsCapsLockEnabled();
            return useUpper ? char.ToUpperInvariant(ch) : char.ToLowerInvariant(ch);
        }

        if (char.IsDigit(ch))
        {
            return ch;
        }

        return token[0] is '.' or ',' or ';' or ':' or '!' or '?'
            ? token[0]
            : null;
    }

    private static bool IsCapsLockEnabled()
    {
        return (GetKeyState(VkCapital) & 0x0001) != 0;
    }

    private static bool IsModifierKey(uint virtualKey)
    {
        return virtualKey is VkShift or VkControl or VkMenu or VkLWin or VkRWin;
    }

    private static bool IsPressed(int virtualKey)
    {
        return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
    }

    private static bool IsFunctionKeyToken(string token)
    {
        if (!token.StartsWith("F", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return int.TryParse(token[1..], out var functionKey) && functionKey is >= 1 and <= 24;
    }

    private static string? MapVirtualKeyToToken(uint virtualKey)
    {
        if (virtualKey is >= 0x30 and <= 0x39)
        {
            return ((char)virtualKey).ToString();
        }

        if (virtualKey is >= 0x41 and <= 0x5A)
        {
            return ((char)virtualKey).ToString();
        }

        if (virtualKey is >= 0x70 and <= 0x87)
        {
            return "F" + (virtualKey - 0x6F);
        }

        return virtualKey switch
        {
            0x0D => "ENTER",
            0x09 => "TAB",
            0x1B => "ESC",
            0x08 => "BACKSPACE",
            0x2E => "DELETE",
            0x24 => "HOME",
            0x23 => "END",
            0x21 => "PGUP",
            0x22 => "PGDN",
            0x25 => "LEFT",
            0x26 => "UP",
            0x27 => "RIGHT",
            0x28 => "DOWN",
            0x20 => "SPACE",
            0xBA => ";",
            0xBB => "=",
            0xBC => ",",
            0xBD => "-",
            0xBE => ".",
            0xBF => "/",
            0xC0 => "`",
            0xDB => "[",
            0xDC => "\\",
            0xDD => "]",
            0xDE => "'",
            _ => null,
        };
    }

    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int WmQuit = 0x0012;
    private const int VkShift = 0x10;
    private const int VkControl = 0x11;
    private const int VkMenu = 0x12;
    private const int VkLWin = 0x5B;
    private const int VkRWin = 0x5C;
    private const int VkCapital = 0x14;
    private const uint LlkhfInjected = 0x10;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHookStruct
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public IntPtr hwnd;
        public uint message;
        public UIntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int ptX;
        public int ptY;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    [DllImport("user32.dll")]
    private static extern sbyte GetMessage(out Msg lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage([In] ref Msg lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage([In] ref Msg lpmsg);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
