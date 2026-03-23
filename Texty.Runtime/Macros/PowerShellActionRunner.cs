using System.Diagnostics;
using System.Text;
using Texty.Core.Interfaces;
using Texty.Core.Models;
using Texty.Runtime.Audit;

namespace Texty.Runtime.Macros;

public sealed class PowerShellActionRunner : IPowerShellActionRunner
{
    private readonly IAuditLogger _auditLogger;

    public PowerShellActionRunner(IAuditLogger? auditLogger = null)
    {
        _auditLogger = auditLogger ?? NoOpAuditLogger.Instance;
    }

    public async Task<PowerShellExecutionResult> ExecuteAsync(
        string script,
        PowerShellExecutionPolicy policy,
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        if (!policy.IsTrusted)
        {
            var source = string.IsNullOrWhiteSpace(policy.PolicySource) ? "unknown" : policy.PolicySource;
            await _auditLogger.WriteAsync(
                new AuditLogEntry(
                    DateTimeOffset.UtcNow,
                    AuditCategory.Security,
                    "powershell.execute",
                    AuditDecision.Deny,
                    correlationId,
                    false,
                    "PowerShell blocked by policy.",
                    new Dictionary<string, string>
                    {
                        ["policySource"] = source,
                    }),
                cancellationToken);
            return new PowerShellExecutionResult(
                false,
                string.Empty,
                $"PowerShell execution is not trusted (policy source: {source}).");
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

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await _auditLogger.WriteAsync(
                new AuditLogEntry(
                    DateTimeOffset.UtcNow,
                    AuditCategory.Security,
                    "powershell.execute",
                    AuditDecision.Error,
                    correlationId,
                    false,
                    "PowerShell execution canceled."),
                CancellationToken.None);
            throw;
        }

        var success = process.ExitCode == 0;
        await _auditLogger.WriteAsync(
            new AuditLogEntry(
                DateTimeOffset.UtcNow,
                AuditCategory.Security,
                "powershell.execute",
                success ? AuditDecision.Allow : AuditDecision.Error,
                correlationId,
                success,
                success ? "PowerShell executed successfully." : "PowerShell failed.",
                new Dictionary<string, string>
                {
                    ["exitCode"] = process.ExitCode.ToString(),
                    ["policySource"] = policy.PolicySource ?? string.Empty,
                }),
            cancellationToken);

        return new PowerShellExecutionResult(success, stdOut.ToString(), stdErr.ToString());
    }
}
