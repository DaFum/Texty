namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface IDslFunctionLibrary
{
    /// <summary>
/// Versucht, eine DSL-Funktion mit dem gegebenen Namen und Argumenten im angegebenen Ausführungs-Kontext auszuführen.
/// </summary>
/// <param name="functionName">Name der aufzurufenden DSL-Funktion.</param>
/// <param name="args">Positionsgebundene Argumente für den Funktionsaufruf.</param>
/// <param name="context">Kontext der Makroausführung, enthält Umgebungsdaten und Zustand, die dem Funktionsaufruf zur Verfügung stehen.</param>
/// <param name="result">Enthält bei Erfolg das Ergebnis der Funktion, sonst `null`.</param>
/// <returns>`true`, wenn die Funktion erfolgreich aufgerufen wurde und `result` ein Ergebnis enthält; `false` sonst.</returns>
bool TryInvoke(string functionName, IReadOnlyList<string> args, MacroExecutionContext context, out string? result);
}

public interface IMacroEngine
{
    /// <summary>
/// Führt das angegebene Makroskript im bereitgestellten Ausführungskontext aus.
/// </summary>
/// <param name="script">Das Makroskript, das ausgeführt werden soll.</param>
/// <param name="context">Der Ausführungskontext, der Variablen, Funktionen und Laufzeitinformationen für die Ausführung bereitstellt.</param>
/// <returns>Ein MacroExecutionResult, das den Ausführungsstatus, erzeugte Ausgabe und gegebenenfalls Fehlerdetails enthält.</returns>
MacroExecutionResult Execute(string script, MacroExecutionContext context);
}

public interface IPowerShellActionRunner
{
    /// <summary>
/// Führt das angegebene PowerShell-Skript aus und liefert das Ergebnis der Ausführung zurück.
/// </summary>
/// <param name="script">Der auszuführende PowerShell-Skriptinhalt.</param>
/// <param name="trusted">Gibt an, ob das Skript als vertrauenswürdig behandelt werden soll (kann Ausführungsrichtlinien und Sicherheitsprüfungen beeinflussen).</param>
/// <returns>Ein PowerShellExecutionResult mit Status, Ausgabe und eventuellen Fehlerinformationen der Ausführung.</returns>
Task<PowerShellExecutionResult> ExecuteAsync(string script, bool trusted, CancellationToken cancellationToken = default);
}
