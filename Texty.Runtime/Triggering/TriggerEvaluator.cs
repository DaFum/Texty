using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Triggering;

public sealed class TriggerEvaluator : ITriggerEvaluator
{
    public Task<IReadOnlyList<TriggerMatch>> EvaluateAsync(
        IEnumerable<TriggerRule> rules,
        TriggerSignal signal,
        CancellationToken cancellationToken = default)
    {
        var matches = new List<TriggerMatch>();
        foreach (var rule in rules.Where(r => r.Enabled && r.Type == signal.Type))
        {
            if (rule.Scope != TriggerScope.Any && rule.Scope != signal.Scope)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(rule.TargetProcess) &&
                !string.Equals(rule.TargetProcess, signal.ProcessName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsMatch(rule, signal.Input))
            {
                var score = rule.Type switch
                {
                    TriggerType.Hotkey => 5,
                    TriggerType.Autotext => 4,
                    TriggerType.Regex => 3,
                    TriggerType.Clipboard => 2,
                    _ => 1,
                };

                matches.Add(new TriggerMatch(rule, score));
            }
        }

        IReadOnlyList<TriggerMatch> ordered = matches
            .OrderByDescending(x => x.Score)
            .ToList();

        return Task.FromResult(ordered);
    }

    private static bool IsMatch(TriggerRule rule, string input)
    {
        if (string.IsNullOrEmpty(rule.Pattern))
        {
            return false;
        }

        var comparison = rule.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        return rule.Type switch
        {
            TriggerType.Regex => System.Text.RegularExpressions.Regex.IsMatch(
                input,
                rule.Pattern,
                rule.CaseSensitive
                    ? System.Text.RegularExpressions.RegexOptions.None
                    : System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            TriggerType.Autotext => input.EndsWith(rule.Pattern, comparison),
            TriggerType.Hotkey => string.Equals(input, rule.Pattern, comparison),
            TriggerType.Clipboard => input.Contains(rule.Pattern, comparison),
            _ => false,
        };
    }
}
