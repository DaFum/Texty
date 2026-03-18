using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Macros;

public sealed class MacroEngine : IMacroEngine
{
    private readonly IDslFunctionLibrary _functionLibrary;

    public MacroEngine(IDslFunctionLibrary functionLibrary)
    {
        _functionLibrary = functionLibrary;
    }

    public MacroExecutionResult Execute(string script, MacroExecutionContext context)
    {
        var lines = script
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var output = new List<string>();
        var audit = new List<string>();
        var vars = new Dictionary<string, string>(context.Variables, StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            if (line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
            {
                continue;
            }

            var command = parts[0].ToLowerInvariant();
            switch (command)
            {
                case "set" when parts.Length >= 3:
                {
                    var key = parts[1];
                    var value = string.Join(' ', parts.Skip(2));
                    vars[key] = value;
                    audit.Add($"set:{key}");
                    break;
                }

                case "append" when parts.Length >= 2:
                {
                    var text = string.Join(' ', parts.Skip(1));
                    output.Add(ExpandVariables(text, vars));
                    audit.Add("append");
                    break;
                }

                case "func" when parts.Length >= 4:
                {
                    var target = parts[1];
                    var functionName = parts[2];
                    var args = parts.Skip(3).Select(x => ExpandVariables(x, vars)).ToArray();
                    if (_functionLibrary.TryInvoke(functionName, args, context with { Variables = vars }, out var result))
                    {
                        vars[target] = result ?? string.Empty;
                        audit.Add($"func:{functionName}");
                    }
                    else
                    {
                        audit.Add($"func-failed:{functionName}");
                    }

                    break;
                }

                case "date" when parts.Length >= 3:
                {
                    var target = parts[1];
                    vars[target] = ResolveDate(parts[2]);
                    audit.Add("date");
                    break;
                }

                default:
                    audit.Add($"unknown:{line}");
                    break;
            }
        }

        return new MacroExecutionResult(string.Join(Environment.NewLine, output), vars, audit);
    }

    private static string ExpandVariables(string text, IReadOnlyDictionary<string, string> vars)
    {
        var value = text;
        foreach (var kvp in vars)
        {
            value = value.Replace($"${kvp.Key}", kvp.Value, StringComparison.OrdinalIgnoreCase);
        }

        return value;
    }

    private static string ResolveDate(string expression)
    {
        var now = DateTimeOffset.Now;
        if (expression.Equals("today", StringComparison.OrdinalIgnoreCase))
        {
            return now.ToString("yyyy-MM-dd");
        }

        if (expression.StartsWith("today+", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(expression.AsSpan("today+".Length).TrimEnd('d'), out var plusDays))
        {
            return now.AddDays(plusDays).ToString("yyyy-MM-dd");
        }

        if (expression.StartsWith("today-", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(expression.AsSpan("today-".Length).TrimEnd('d'), out var minusDays))
        {
            return now.AddDays(-minusDays).ToString("yyyy-MM-dd");
        }

        return now.ToString("yyyy-MM-dd");
    }
}
