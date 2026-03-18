namespace Texty.Core.Models;

public sealed record TriggerSignal(
    TriggerType Type,
    string Input,
    string? ProcessName,
    TriggerScope Scope);

public sealed record TriggerMatch(TriggerRule Rule, double Score);
