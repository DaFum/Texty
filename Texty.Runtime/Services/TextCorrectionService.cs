using System.Text.RegularExpressions;

namespace Texty.Runtime.Services;

public sealed class TextCorrectionService
{
    public string ApplyCorrections(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        var text = input.Trim();

        // Fix accidental double uppercase at word start, e.g. "HEllo" -> "Hello".
        text = Regex.Replace(text, @"\b([A-Z]{2})([a-z]+)\b", match =>
        {
            var first = match.Groups[1].Value;
            return char.ToUpperInvariant(first[0]) + first[1].ToString().ToLowerInvariant() + match.Groups[2].Value;
        });

        // Uppercase sentence starts after punctuation.
        text = Regex.Replace(text, @"(^|[.!?]\s+)([a-z])", m => m.Groups[1].Value + m.Groups[2].Value.ToUpperInvariant());

        return text;
    }
}
