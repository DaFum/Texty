using System.Xml.XPath;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Integrations.Resolvers;

public sealed class XmlResolver : IExternalDataResolver
{
    public string Name => "xml";

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
