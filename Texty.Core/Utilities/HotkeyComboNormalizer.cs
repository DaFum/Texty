namespace Texty.Core.Utilities;

public static class HotkeyComboNormalizer
{
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var compact = input.Replace(" ", string.Empty);
        var rawTokens = compact.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (rawTokens.Length == 0)
        {
            return string.Empty;
        }

        var modifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? keyToken = null;

        foreach (var raw in rawTokens)
        {
            var token = NormalizeToken(raw);
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            if (IsModifier(token))
            {
                modifiers.Add(token);
                continue;
            }

            keyToken = token;
        }

        var ordered = new List<string>(5);
        if (modifiers.Contains("CTRL"))
        {
            ordered.Add("CTRL");
        }

        if (modifiers.Contains("SHIFT"))
        {
            ordered.Add("SHIFT");
        }

        if (modifiers.Contains("ALT"))
        {
            ordered.Add("ALT");
        }

        if (modifiers.Contains("WIN"))
        {
            ordered.Add("WIN");
        }

        if (!string.IsNullOrWhiteSpace(keyToken))
        {
            ordered.Add(keyToken);
        }

        return string.Join("+", ordered);
    }

    private static bool IsModifier(string token) =>
        token is "CTRL" or "SHIFT" or "ALT" or "WIN";

    private static string NormalizeToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var token = value.Trim().ToUpperInvariant();

        return token switch
        {
            "CTRL" or "CONTROL" or "STRG" => "CTRL",
            "SHIFT" or "UMSCHALT" => "SHIFT",
            "ALT" or "MENU" or "OPTION" => "ALT",
            "WIN" or "WINDOWS" or "META" or "CMD" or "SUPER" => "WIN",
            "RETURN" => "ENTER",
            "ESCAPE" => "ESC",
            "DEL" => "DELETE",
            "PAGEUP" => "PGUP",
            "PAGEDOWN" => "PGDN",
            _ when token.Length == 1 => token,
            _ when token.StartsWith("F", StringComparison.Ordinal) &&
                   int.TryParse(token[1..], out var functionKey) &&
                   functionKey is >= 1 and <= 24 => $"F{functionKey}",
            _ => token,
        };
    }
}
