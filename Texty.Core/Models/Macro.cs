namespace Texty.Core.Models;

public sealed record MacroExecutionContext(
    IDictionary<string, string> Variables,
    InsertionContext InsertionContext);

public sealed record MacroExecutionResult(
    string Output,
    IReadOnlyDictionary<string, string> Variables,
    IReadOnlyList<string> AuditTrail);

public sealed record PowerShellExecutionResult(bool Success, string Output, string Error);
