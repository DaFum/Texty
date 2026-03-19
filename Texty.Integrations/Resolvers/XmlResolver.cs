using System.Xml.XPath;
using System.Xml;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Integrations.Resolvers;

public sealed class XmlResolver : IExternalDataResolver
{
    public string Name => "xml";

    public async Task<string?> ResolveAsync(ExternalValueRequest request, CancellationToken cancellationToken = default)
    {
        var parts = request.Expression.Split('|', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !File.Exists(parts[0]))
        {
            return null;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var xmlContent = await File.ReadAllTextAsync(parts[0], cancellationToken);
            using var reader = XmlReader.Create(
                new StringReader(xmlContent),
                new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                });

            var doc = new XPathDocument(reader);
            var navigator = doc.CreateNavigator();
            return navigator.SelectSingleNode(parts[1])?.Value;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }
}
