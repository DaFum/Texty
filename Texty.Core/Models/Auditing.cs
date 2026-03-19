namespace Texty.Core.Models;

public enum AuditCategory
{
    Security,
    Integration,
    Insertion,
    Import,
    Resolver,
    Macro,
    Action,
}

public enum AuditDecision
{
    None,
    Allow,
    Deny,
    Error,
}

public sealed record AuditLogEntry(
    DateTimeOffset TimestampUtc,
    AuditCategory Category,
    string Action,
    AuditDecision Decision,
    string CorrelationId,
    bool Success,
    string Message,
    IReadOnlyDictionary<string, string>? Metadata = null);
