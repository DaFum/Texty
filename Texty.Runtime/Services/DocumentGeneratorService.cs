using Texty.Core.Models;

namespace Texty.Runtime.Services;

public sealed class DocumentGeneratorService
{
    public string GenerateDocument(IEnumerable<Snippet> snippets, string title)
    {
        var lines = new List<string>
        {
            title,
            new string('=', Math.Max(3, title.Length)),
            string.Empty,
        };

        foreach (var snippet in snippets)
        {
            lines.Add($"## {snippet.Title}");
            lines.Add(snippet.PlainText);
            lines.Add(string.Empty);
        }

        return string.Join(Environment.NewLine, lines);
    }
}
