using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Macros;

public sealed class MacroEngine : IMacroEngine
{
    private readonly IDslFunctionLibrary _functionLibrary;
    private readonly IMacroActionExecutor? _actionExecutor;

    public MacroEngine(IDslFunctionLibrary functionLibrary, IMacroActionExecutor? actionExecutor = null)
    {
        _functionLibrary = functionLibrary;
        _actionExecutor = actionExecutor;
    }

    public MacroExecutionResult Execute(
        string script,
        MacroExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var lines = script
            .Split(Environment.NewLine, StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        var output = new List<string>();
        var audit = new List<string>();
        var errors = new List<string>();
        var vars = new Dictionary<string, string>(context.Variables, StringComparer.OrdinalIgnoreCase);
        var index = 0;

        try
        {
            ExecuteBlock(lines, ref index, vars, output, audit, errors, context, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            errors.Add("Macro execution canceled.");
            throw;
        }

        return new MacroExecutionResult(
            errors.Count == 0,
            string.Join(Environment.NewLine, output),
            vars,
            audit,
            errors);
    }

    private void ExecuteBlock(
        IReadOnlyList<string> lines,
        ref int index,
        Dictionary<string, string> vars,
        ICollection<string> output,
        ICollection<string> audit,
        ICollection<string> errors,
        MacroExecutionContext context,
        CancellationToken cancellationToken)
    {
        while (index < lines.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rawLine = lines[index];
            var line = rawLine.Trim();
            index++;

            if (line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.Equals("end", StringComparison.OrdinalIgnoreCase) ||
                line.Equals("else", StringComparison.OrdinalIgnoreCase))
            {
                index--;
                return;
            }

            var tokens = SplitTokens(line, vars);
            if (tokens.Count == 0)
            {
                continue;
            }

            var command = tokens[0].ToLowerInvariant();
            switch (command)
            {
                case "set":
                case "let":
                    ExecuteSet(tokens, vars, audit, errors);
                    break;
                case "append":
                    ExecuteAppend(tokens, output, audit);
                    break;
                case "func":
                    ExecuteFunc(tokens, vars, audit, errors, context);
                    break;
                case "date":
                    ExecuteDate(tokens, vars, audit, errors);
                    break;
                case "if":
                    ExecuteIf(lines, ref index, tokens, vars, output, audit, errors, context, cancellationToken);
                    break;
                case "action":
                    ExecuteAction(tokens, vars, audit, errors, context, cancellationToken);
                    break;
                default:
                    errors.Add($"Unknown command: {line}");
                    audit.Add($"unknown:{line}");
                    break;
            }
        }
    }

    private static void ExecuteSet(
        IReadOnlyList<string> tokens,
        Dictionary<string, string> vars,
        ICollection<string> audit,
        ICollection<string> errors)
    {
        if (tokens.Count < 3)
        {
            errors.Add("set/let requires key and value.");
            return;
        }

        var key = tokens[1];
        var value = string.Join(' ', tokens.Skip(2));
        vars[key] = value;
        audit.Add($"set:{key}");
    }

    private static void ExecuteAppend(
        IReadOnlyList<string> tokens,
        ICollection<string> output,
        ICollection<string> audit)
    {
        if (tokens.Count < 2)
        {
            return;
        }

        output.Add(string.Join(' ', tokens.Skip(1)));
        audit.Add("append");
    }

    private void ExecuteFunc(
        IReadOnlyList<string> tokens,
        Dictionary<string, string> vars,
        ICollection<string> audit,
        ICollection<string> errors,
        MacroExecutionContext context)
    {
        if (tokens.Count < 4)
        {
            errors.Add("func requires target, function and args.");
            return;
        }

        var target = tokens[1];
        var functionName = tokens[2];
        var args = tokens.Skip(3).ToArray();
        if (_functionLibrary.TryInvoke(functionName, args, context with { Variables = vars }, out var result))
        {
            vars[target] = result ?? string.Empty;
            audit.Add($"func:{functionName}");
            return;
        }

        errors.Add($"Function failed: {functionName}");
        audit.Add($"func-failed:{functionName}");
    }

    private static void ExecuteDate(
        IReadOnlyList<string> tokens,
        Dictionary<string, string> vars,
        ICollection<string> audit,
        ICollection<string> errors)
    {
        if (tokens.Count < 3)
        {
            errors.Add("date requires target and expression.");
            return;
        }

        var target = tokens[1];
        vars[target] = ResolveDate(tokens[2]);
        audit.Add("date");
    }

    private void ExecuteIf(
        IReadOnlyList<string> lines,
        ref int index,
        IReadOnlyList<string> tokens,
        Dictionary<string, string> vars,
        ICollection<string> output,
        ICollection<string> audit,
        ICollection<string> errors,
        MacroExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (tokens.Count < 2)
        {
            errors.Add("if requires a condition.");
            return;
        }

        var condition = EvaluateCondition(tokens.Skip(1).ToArray(), vars);
        if (condition)
        {
            ExecuteBlock(lines, ref index, vars, output, audit, errors, context, cancellationToken);
            if (index < lines.Count && lines[index].Trim().Equals("else", StringComparison.OrdinalIgnoreCase))
            {
                SkipBlock(lines, ref index, cancellationToken);
            }
        }
        else
        {
            SkipBlock(lines, ref index, cancellationToken, stopAtElse: true);
            if (index < lines.Count && lines[index].Trim().Equals("else", StringComparison.OrdinalIgnoreCase))
            {
                index++;
                ExecuteBlock(lines, ref index, vars, output, audit, errors, context, cancellationToken);
            }
        }

        if (index < lines.Count && lines[index].Trim().Equals("end", StringComparison.OrdinalIgnoreCase))
        {
            index++;
        }
    }

    private void ExecuteAction(
        IReadOnlyList<string> tokens,
        IReadOnlyDictionary<string, string> vars,
        ICollection<string> audit,
        ICollection<string> errors,
        MacroExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (_actionExecutor is null)
        {
            errors.Add("No action executor configured.");
            return;
        }

        if (tokens.Count < 2)
        {
            errors.Add("action requires action name.");
            return;
        }

        var actionName = tokens[1];
        var args = tokens.Skip(2).ToList();
        var correlationId = vars.TryGetValue("correlationId", out var configured) && !string.IsNullOrWhiteSpace(configured)
            ? configured
            : Guid.NewGuid().ToString("N");

        var request = new MacroActionRequest(
            actionName,
            args,
            correlationId,
            context.ActionPolicy ?? new MacroActionPolicy(
                AllowProcessStart: true,
                AllowFileSystemWrite: true,
                AllowExternalOpen: true,
                AllowNotifications: true,
                AllowPowerShell: true));

        var result = _actionExecutor.ExecuteAsync(request, cancellationToken).GetAwaiter().GetResult();
        audit.Add($"action:{actionName}:{(result.Success ? "ok" : "fail")}");
        if (!result.Success)
        {
            errors.Add(result.Message);
        }
    }

    private static bool EvaluateCondition(IReadOnlyList<string> conditionTokens, IReadOnlyDictionary<string, string> vars)
    {
        if (conditionTokens.Count == 1)
        {
            return IsTruthy(conditionTokens[0]);
        }

        if (conditionTokens.Count >= 2 && conditionTokens[0].Equals("exists", StringComparison.OrdinalIgnoreCase))
        {
            return vars.ContainsKey(conditionTokens[1]);
        }

        if (conditionTokens.Count < 3)
        {
            return false;
        }

        var left = conditionTokens[0];
        var op = conditionTokens[1].ToLowerInvariant();
        var right = string.Join(' ', conditionTokens.Skip(2));

        return op switch
        {
            "eq" or "==" => string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
            "ne" or "!=" => !string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
            "contains" => left.Contains(right, StringComparison.OrdinalIgnoreCase),
            "regex" => System.Text.RegularExpressions.Regex.IsMatch(left, right),
            _ => false,
        };
    }

    private static bool IsTruthy(string value)
    {
        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    private static void SkipBlock(
        IReadOnlyList<string> lines,
        ref int index,
        CancellationToken cancellationToken,
        bool stopAtElse = false)
    {
        var depth = 0;
        while (index < lines.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = lines[index].Trim();
            if (line.StartsWith("if ", StringComparison.OrdinalIgnoreCase))
            {
                depth++;
            }
            else if (line.Equals("end", StringComparison.OrdinalIgnoreCase))
            {
                if (depth == 0)
                {
                    return;
                }

                depth--;
            }
            else if (stopAtElse && depth == 0 && line.Equals("else", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            index++;
        }
    }

    private static List<string> SplitTokens(string line, IReadOnlyDictionary<string, string> vars)
    {
        var tokens = new List<string>();
        var buffer = new List<char>();
        var inQuote = false;
        var quoteChar = '\0';

        foreach (var ch in line)
        {
            if (!inQuote && (ch == '"' || ch == '\''))
            {
                inQuote = true;
                quoteChar = ch;
                continue;
            }

            if (inQuote && ch == quoteChar)
            {
                inQuote = false;
                continue;
            }

            if (!inQuote && char.IsWhiteSpace(ch))
            {
                FlushToken(buffer, tokens, vars);
                continue;
            }

            buffer.Add(ch);
        }

        FlushToken(buffer, tokens, vars);
        return tokens;
    }

    private static void FlushToken(List<char> buffer, ICollection<string> tokens, IReadOnlyDictionary<string, string> vars)
    {
        if (buffer.Count == 0)
        {
            return;
        }

        var token = new string(buffer.ToArray());
        buffer.Clear();
        tokens.Add(ExpandVariables(token, vars));
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
