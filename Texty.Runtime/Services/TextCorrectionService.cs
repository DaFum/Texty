using System.Text.RegularExpressions;

namespace Texty.Runtime.Services;

public sealed class TextCorrectionService
{
    /// <summary>
    /// Korrigiert typische Groß-/Kleinschreibfehler im übergebenen Text (z. B. doppelt groß geschriebene Anfangsbuchstaben, Satzanfänge nach Punkt/Frage/ Ausrufezeichen).
    /// </summary>
    /// <param name="input">Zu korrigierender Text. Bei null, leerem oder nur aus Leerzeichen bestehendem Wert wird dieser unverändert zurückgegeben.</param>
    /// <returns>Der bereinigte Text nach den angewendeten Korrekturen.</returns>
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
