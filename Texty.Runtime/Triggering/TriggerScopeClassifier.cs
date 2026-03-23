using Texty.Core.Models;

namespace Texty.Runtime.Triggering;

internal static class TriggerScopeClassifier
{
    public static TriggerScope Classify(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return TriggerScope.Any;
        }

        var normalized = NormalizeProcessName(processName);
        if (string.Equals(normalized, "outlook", StringComparison.OrdinalIgnoreCase))
        {
            return TriggerScope.Email;
        }

        return normalized switch
        {
            "notepad" => TriggerScope.TextFile,
            "notepad++" => TriggerScope.TextFile,
            "code" => TriggerScope.TextFile,
            "wordpad" => TriggerScope.TextFile,
            _ => TriggerScope.Any,
        };
    }

    public static string NormalizeProcessName(string processName)
    {
        var value = processName.Trim();
        return value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? value[..^4]
            : value;
    }
}
