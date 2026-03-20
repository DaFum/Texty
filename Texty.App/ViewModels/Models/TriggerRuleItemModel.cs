using Texty.Core.Models;

namespace Texty.App.ViewModels.Models;

public sealed record TriggerRuleItemModel(
    Guid Id,
    TriggerType Type,
    string Pattern,
    bool CaseSensitive,
    TriggerScope Scope,
    string? TargetProcess,
    bool Enabled)
{
    public string Display => $"{Type} | {Scope} | {(string.IsNullOrWhiteSpace(TargetProcess) ? "all" : TargetProcess)} | {Pattern}";
}
