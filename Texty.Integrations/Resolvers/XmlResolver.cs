using System.Xml.XPath;
using System.Xml;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Integrations.Resolvers;

public sealed class XmlResolver : IExternalDataResolver
{
    private readonly IAuditLogger? _auditLogger;

    public XmlResolver(IAuditLogger? auditLogger = null)
    {
        _auditLogger = auditLogger;
    }

    public string Name => "xml";

    public async Task<string?> ResolveAsync(ExternalValueRequest request, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var parts = request.Expression.Split('|', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !File.Exists(parts[0]))
        {
            await WriteAuditAsync(
                correlationId,
                false,
                AuditDecision.Error,
                "XML expression invalid or file missing.",
                cancellationToken);
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
            var value = navigator.SelectSingleNode(parts[1])?.Value;
            await WriteAuditAsync(
                correlationId,
                value is not null,
                value is not null ? AuditDecision.Allow : AuditDecision.Error,
                value is not null ? "XML value resolved." : "XML XPath returned no result.",
                cancellationToken);
            return value;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await WriteAuditAsync(
                correlationId,
                false,
                AuditDecision.Error,
                "XML resolve canceled.",
                CancellationToken.None);
            throw;
        }
        catch
        {
            await WriteAuditAsync(
                correlationId,
                false,
                AuditDecision.Error,
                "XML resolve failed with exception.",
                cancellationToken);
            return null;
        }
    }

    private Task WriteAuditAsync(
        string correlationId,
        bool success,
        AuditDecision decision,
        string message,
        CancellationToken cancellationToken)
    {
        if (_auditLogger is null)
        {
            return Task.CompletedTask;
        }

        return _auditLogger.WriteAsync(
            new AuditLogEntry(
                DateTimeOffset.UtcNow,
                AuditCategory.Resolver,
                "resolver.xml",
                decision,
                correlationId,
                success,
                message),
            cancellationToken);
    }
}
