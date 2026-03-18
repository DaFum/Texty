using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Triggering;

public sealed class TriggerEvaluator : ITriggerEvaluator
{
    /// <summary>
    /// Bewertet eine Sammlung von TriggerRules gegen ein TriggerSignal und liefert gefundene TriggerMatches mit zugehörigen Scores.
    /// </summary>
    /// <param name="rules">Die zu prüfenden TriggerRules. Nur aktivierte Regeln mit übereinstimmendem TriggerType werden berücksichtigt.</param>
    /// <param name="signal">Das auslösende Signal; dessen Type, Scope, ProcessName und Input werden zur Filterung und Musterprüfung verwendet.</param>
    /// <param name="cancellationToken">Nicht beobachteter CancellationToken (wird in der aktuellen Implementierung nicht berücksichtigt).</param>
    /// <returns>Eine Liste gefundener TriggerMatch-Objekte, sortiert nach Score in absteigender Reihenfolge.</returns>
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

    /// <summary>
    /// Bestimmt, ob die übergebene Eingabe mit dem Muster und Typ der angegebenen TriggerRule übereinstimmt.
    /// </summary>
    /// <param name="rule">Die zu prüfende Regel; ihr Pattern, Typ und die Groß-/Kleinschreibungs-Einstellung werden verwendet.</param>
    /// <param name="input">Der zu prüfende Eingabetext.</param>
    /// <returns>`true` wenn die Eingabe dem Regelmuster entsprechend dem TriggerType und der Groß-/Kleinschreibungs-Einstellung entspricht, `false` ansonsten.</returns>
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
