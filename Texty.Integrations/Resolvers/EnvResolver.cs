using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Integrations.Resolvers;

public sealed class EnvResolver : IExternalDataResolver
{
    private readonly IAuditLogger? _auditLogger;

    public EnvResolver(IAuditLogger? auditLogger = null)
    {
        _auditLogger = auditLogger;
    }

    public string Name => "env";

    public async Task<string?> ResolveAsync(ExternalValueRequest request, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var variable = request.Expression.Trim();
        if (string.IsNullOrWhiteSpace(variable))
        {
            await WriteAuditAsync(
                correlationId,
                false,
                AuditDecision.Error,
                "Environment variable expression is empty.",
                cancellationToken);
            return null;
        }

        var value = Environment.GetEnvironmentVariable(variable);
        await WriteAuditAsync(
            correlationId,
            value is not null,
            value is not null ? AuditDecision.Allow : AuditDecision.Error,
            value is not null
                ? $"Environment variable '{variable}' resolved."
                : $"Environment variable '{variable}' not found.",
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
                "resolver.env",
                decision,
                correlationId,
                success,
                message),
            cancellationToken);
    }
}
