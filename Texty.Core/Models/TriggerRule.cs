namespace Texty.Core.Models;

public sealed record TriggerRule(
    Guid Id,
    TriggerType Type,
    string Pattern,
    bool CaseSensitive,
    TriggerScope Scope,
    string? TargetProcess,
    bool Enabled);
