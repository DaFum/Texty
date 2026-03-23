namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface IAuditLogger
{
    Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
}
