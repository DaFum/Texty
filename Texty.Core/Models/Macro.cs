namespace Texty.Core.Models;

public sealed record MacroExecutionContext(
    IDictionary<string, string> Variables,
    InsertionContext InsertionContext,
    MacroActionPolicy? ActionPolicy = null);

public sealed record MacroExecutionResult(
    bool Success,
    string Output,
    IReadOnlyDictionary<string, string> Variables,
    IReadOnlyList<string> AuditTrail,
    IReadOnlyList<string>? Errors = null);

public sealed record PowerShellExecutionPolicy(
    bool IsTrusted,
    string? PolicySource = null);

public sealed record PowerShellExecutionResult(bool Success, string Output, string Error);
