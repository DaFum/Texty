using System.Xml.XPath;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Integrations.Resolvers;

public sealed class XmlResolver : IExternalDataResolver
{
    public string Name => "xml";

    /// <summary>
    /// Löst einen XPath-Ausdruck aus einer XML-Datei anhand der Angabe in request.Expression im Format "Dateipfad|XPath".
    /// </summary>
    /// <param name="request">Enthält in <c>Expression</c> den Dateipfad und den XPath-Ausdruck getrennt durch ein '|' (z. B. "C:\pfad\datei.xml|/root/node").</param>
    /// <returns>Den gefundenen Knotentext oder <c>null</c>, wenn der Ausdruck nicht im erwarteten Format ist, die Datei nicht existiert oder kein Knoten gefunden wurde.</returns>
    public Task<string?> ResolveAsync(ExternalValueRequest request, CancellationToken cancellationToken = default)
    {
        var parts = request.Expression.Split('|', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !File.Exists(parts[0]))
        {
            return Task.FromResult<string?>(null);
        }

        var doc = new XPathDocument(parts[0]);
        var navigator = doc.CreateNavigator();
        var value = navigator.SelectSingleNode(parts[1])?.Value;
        return Task.FromResult(value);
    }
}
