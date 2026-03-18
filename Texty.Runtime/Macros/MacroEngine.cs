using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Macros;

public sealed class MacroEngine : IMacroEngine
{
    private readonly IDslFunctionLibrary _functionLibrary;

    /// <summary>
    /// Initialisiert eine neue Instanz von <see cref="MacroEngine"/> mit der angegebenen Funktionsbibliothek.
    /// </summary>
    /// <param name="functionLibrary">Bibliothek zum Aufrufen von DSL-Funktionen, die von der Engine beim Ausführen von Makros verwendet wird.</param>
    public MacroEngine(IDslFunctionLibrary functionLibrary)
    {
        _functionLibrary = functionLibrary;
    }

    /// <summary>
    /// Führt ein Text-basiertes Makroskript aus und liefert die daraus resultierende Ausgabe, die finalen Variablen und ein Audit-Protokoll.
    /// </summary>
    /// <param name="script">Das Makroskript als String; Zeilen werden getrennt, leere Zeilen und Kommentarzeilen (`#`) werden ignoriert.</param>
    /// <param name="context">Kontext für die Ausführung, einschließlich der anfänglichen Variablen und weiterer Ausführungsdaten.</param>
    /// <returns>Ein MacroExecutionResult mit der zusammengefügten Ausgabe, dem finalen Variablen-Dictionary und der Liste von Audit-Einträgen.</returns>
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

    /// <summary>
    /// Ersetzt in einem Eingabetext alle Vorkommen von `$Key` durch den zugehörigen Wert aus dem Wörterbuch.
    /// </summary>
    /// <param name="text">Der Quelltext, in dem Ersetzungen vorgenommen werden sollen.</param>
    /// <param name="vars">Ein Wörterbuch von Schlüssel/Wert-Paaren; jedes Vorkommen von `$` gefolgt vom Schlüssel wird ersetzt.</param>
    /// <returns>Der Text nach Anwendung aller Ersetzungen; Schlüsselvergleich erfolgt ohne Beachtung der Groß-/Kleinschreibung.</returns>
    private static string ExpandVariables(string text, IReadOnlyDictionary<string, string> vars)
    {
        var value = text;
        foreach (var kvp in vars)
        {
            value = value.Replace($"${kvp.Key}", kvp.Value, StringComparison.OrdinalIgnoreCase);
        }

        return value;
    }

    /// <summary>
    /// Wandelt einen Datums-Ausdruck wie "today", "today+Nd" oder "today-Nd" in ein Datum im Format yyyy-MM-dd um.
    /// </summary>
    /// <param name="expression">Ausdruck, der entweder "today" oder "today±Nd" (z. B. "today+3d", "today-1d") ist; Groß-/Kleinschreibung wird ignoriert.</param>
    /// <returns>Das berechnete Datum als Zeichenkette im Format "yyyy-MM-dd".</returns>
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
