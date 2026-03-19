using System.Runtime.InteropServices;
using Texty.Core.Interfaces;

namespace Texty.Runtime.Insertion;

public sealed class WindowsKeystrokeEmitter : IKeystrokeEmitter
{
    private static readonly IReadOnlyDictionary<string, ushort> VirtualKeyMap = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase)
    {
        ["CTRL"] = 0x11,
        ["SHIFT"] = 0x10,
        ["ALT"] = 0x12,
        ["WIN"] = 0x5B,
        ["ENTER"] = 0x0D,
        ["TAB"] = 0x09,
        ["ESC"] = 0x1B,
        ["BACKSPACE"] = 0x08,
        ["DELETE"] = 0x2E,
        ["HOME"] = 0x24,
        ["END"] = 0x23,
        ["PGUP"] = 0x21,
        ["PGDN"] = 0x22,
        ["LEFT"] = 0x25,
        ["UP"] = 0x26,
        ["RIGHT"] = 0x27,
        ["DOWN"] = 0x28,
        ["SPACE"] = 0x20,
    };

    public Task SendPasteAsync(CancellationToken cancellationToken = default)
        => SendChordAsync("CTRL+V", cancellationToken);

    public async Task SendSequenceAsync(IReadOnlyList<string> actions, CancellationToken cancellationToken = default)
    {
        foreach (var action in actions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await SendChordAsync(action, cancellationToken);
            await Task.Delay(10, cancellationToken);
        }
    }

    private static Task SendChordAsync(string action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
        {
            return Task.CompletedTask;
        }

        var keys = ParseAction(action);
        if (keys.Count == 0)
        {
            return Task.CompletedTask;
        }

        var modifierKeys = keys.Take(keys.Count - 1).ToList();
        var primaryKey = keys[^1];

        var inputs = new List<INPUT>(modifierKeys.Count * 2 + 2);
        foreach (var modifier in modifierKeys)
        {
            inputs.Add(CreateKeyInput(modifier, keyUp: false));
        }

        inputs.Add(CreateKeyInput(primaryKey, keyUp: false));
        inputs.Add(CreateKeyInput(primaryKey, keyUp: true));

        for (var i = modifierKeys.Count - 1; i >= 0; i--)
        {
            inputs.Add(CreateKeyInput(modifierKeys[i], keyUp: true));
        }

        var sent = SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
        if (sent != (uint)inputs.Count)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"Failed to send keyboard input. Expected {inputs.Count}, sent {sent}, Win32Error={error}.");
        }

        return Task.CompletedTask;
    }

    private static List<ushort> ParseAction(string action)
    {
        var tokens = action
            .Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        var keys = new List<ushort>(tokens.Count);
        foreach (var token in tokens)
        {
            var key = TryMapTokenToVirtualKey(token);
            if (key is null)
            {
                return [];
            }

            keys.Add(key.Value);
        }

        return keys;
    }

    private static ushort? TryMapTokenToVirtualKey(string token)
    {
        if (VirtualKeyMap.TryGetValue(token, out var known))
        {
            return known;
        }

        if (token.Length == 1)
        {
            var ch = char.ToUpperInvariant(token[0]);
            if (ch is >= 'A' and <= 'Z')
            {
                return (ushort)ch;
            }

            if (ch is >= '0' and <= '9')
            {
                return (ushort)ch;
            }
        }

        if (token.StartsWith("F", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(token[1..], out var functionKeyNumber) &&
            functionKeyNumber is >= 1 and <= 24)
        {
            return (ushort)(0x70 + functionKeyNumber - 1);
        }

        return null;
    }

    private static INPUT CreateKeyInput(ushort virtualKey, bool keyUp)
    {
        return new INPUT
        {
            type = 1,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    wScan = 0,
                    dwFlags = keyUp ? 0x0002u : 0u,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero,
                },
            },
        };
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }
}
