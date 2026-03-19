using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Audit;

public sealed class NoOpAuditLogger : IAuditLogger
{
    public static NoOpAuditLogger Instance { get; } = new();

    private NoOpAuditLogger()
    {
    }

    public Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        _ = entry;
        _ = cancellationToken;
        return Task.CompletedTask;
    }
}
