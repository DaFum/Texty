using Texty.Core.Models;

namespace Texty.Runtime.Services;

public sealed class DocumentGeneratorService
{
    /// <summary>
    /// Erzeugt ein Markdown-ähnliches Dokument aus einer Auflistung von Snippets.
    /// </summary>
    /// <param name="snippets">Die Snippet-Objekte, die nacheinander als Abschnitte gerendert werden; für jedes Snippet wird eine Level-2-Überschrift mit dem Snippet-Title gefolgt vom PlainText eingefügt.</param>
    /// <param name="title">Der Haupttitel des Dokuments; unter dem Titel wird eine Trennzeile aus '=' mit mindestens drei Zeichen Länge erzeugt.</param>
    /// <returns>Der vollständige Dokumenttext als eine Zeichenfolge, Zeilen durch Environment.NewLine getrennt.</returns>
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
