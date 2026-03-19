namespace Texty.Core.Models;

public sealed record MacroActionRequest(
    string Name,
    IReadOnlyList<string> Arguments,
    string CorrelationId,
    MacroActionPolicy Policy);

public sealed record MacroActionPolicy(
    bool AllowProcessStart,
    bool AllowFileSystemWrite,
    bool AllowExternalOpen,
    bool AllowNotifications,
    bool AllowPowerShell);

public sealed record MacroActionResult(
    bool Success,
    bool BlockedByPolicy,
    string Message,
    string? Output = null);
