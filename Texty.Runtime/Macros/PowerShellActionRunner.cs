using System.Diagnostics;
using System.Text;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Macros;

public sealed class PowerShellActionRunner : IPowerShellActionRunner
{
    /// <summary>
    /// Führt das übergebene PowerShell-Skript aus und sammelt dabei Standardausgabe und Standardfehler.
    /// </summary>
    /// <param name="script">Der auszuführende PowerShell-Befehl oder -Skript.</param>
    /// <param name="trusted">Gibt an, ob die Ausführung erlaubt ist; ist dieser Wert `false`, wird die Ausführung nicht gestartet.</param>
    /// <param name="cancellationToken">Token zum Abbrechen der Ausführung.</param>
    /// <returns>Ein PowerShellExecutionResult mit einem Erfolgsflag (`true` wenn der Prozess mit ExitCode 0 endet), dem gesammelten Standardausgang und dem gesammelten Standardfehler. Wenn `trusted` `false` ist, enthält das Ergebnis `false`, leeren Standardausgang und die Fehlermeldung "PowerShell execution is not trusted."</returns>
    public async Task<PowerShellExecutionResult> ExecuteAsync(string script, bool trusted, CancellationToken cancellationToken = default)
    {
        if (!trusted)
        {
            return new PowerShellExecutionResult(false, string.Empty, "PowerShell execution is not trusted.");
        }

        var psi = new ProcessStartInfo
        {
            FileName = "pwsh",
            ArgumentList = { "-NoLogo", "-NoProfile", "-Command", script },
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = psi };
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stdOut.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stdErr.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(cancellationToken);

        var success = process.ExitCode == 0;
        return new PowerShellExecutionResult(success, stdOut.ToString(), stdErr.ToString());
    }
}
