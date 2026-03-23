using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Integrations.Resolvers;

public sealed class CsvResolver : IExternalDataResolver
{
    private readonly IAuditLogger? _auditLogger;

    public CsvResolver(IAuditLogger? auditLogger = null)
    {
        _auditLogger = auditLogger;
    }

    public string Name => "csv";

    public async Task<string?> ResolveAsync(ExternalValueRequest request, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var parts = request.Expression.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            await WriteAuditAsync(
                correlationId,
                false,
                AuditDecision.Error,
                "CSV expression is invalid.",
                cancellationToken);
            return null;
        }

        if (!int.TryParse(parts[1], out var rowIndex) || !int.TryParse(parts[2], out var columnIndex))
        {
            await WriteAuditAsync(
                correlationId,
                false,
                AuditDecision.Error,
                "CSV row/column indexes are invalid.",
                cancellationToken);
            return null;
        }

        var path = parts[0];
        if (!File.Exists(path))
        {
            await WriteAuditAsync(
                correlationId,
                false,
                AuditDecision.Error,
                $"CSV source not found: {path}",
                cancellationToken);
            return null;
        }

        var lines = await File.ReadAllLinesAsync(path, cancellationToken);
        if (rowIndex < 0 || rowIndex >= lines.Length)
        {
            await WriteAuditAsync(
                correlationId,
                false,
                AuditDecision.Error,
                "CSV row is out of range.",
                cancellationToken);
            return null;
        }

        var line = lines[rowIndex];
        var cells = line.Split([',', ';', '\t']);
        if (columnIndex < 0 || columnIndex >= cells.Length)
        {
            await WriteAuditAsync(
                correlationId,
                false,
                AuditDecision.Error,
                "CSV column is out of range.",
                cancellationToken);
            return null;
        }

        var value = cells[columnIndex];
        await WriteAuditAsync(
            correlationId,
            true,
            AuditDecision.Allow,
            "CSV value resolved.",
            cancellationToken);
        return value;
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
                "resolver.csv",
                decision,
                correlationId,
                success,
                message),
            cancellationToken);
    }
}
