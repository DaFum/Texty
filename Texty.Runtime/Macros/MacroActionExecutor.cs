using System.Diagnostics;
using Texty.Core.Interfaces;
using Texty.Core.Models;
using Texty.Runtime.Audit;

namespace Texty.Runtime.Macros;

public sealed class MacroActionExecutor : IMacroActionExecutor
{
    private readonly IPowerShellActionRunner _powerShellActionRunner;
    private readonly IAuditLogger _auditLogger;

    public MacroActionExecutor(IPowerShellActionRunner powerShellActionRunner, IAuditLogger? auditLogger = null)
    {
        _powerShellActionRunner = powerShellActionRunner;
        _auditLogger = auditLogger ?? NoOpAuditLogger.Instance;
    }

    public async Task<MacroActionResult> ExecuteAsync(MacroActionRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var action = request.Name.Trim().ToLowerInvariant();
        try
        {
            var result = action switch
            {
                "run-program" => await RunProgramAsync(request, cancellationToken),
                "open-file" => await OpenFileOrUrlAsync(request, isUrl: false, cancellationToken),
                "open-url" => await OpenFileOrUrlAsync(request, isUrl: true, cancellationToken),
                "open-explorer" => await OpenExplorerAsync(request, cancellationToken),
                "write-file" => await WriteFileAsync(request, cancellationToken),
                "notify" => await NotifyAsync(request, cancellationToken),
                "powershell" => await RunPowerShellAsync(request, cancellationToken),
                _ => new MacroActionResult(false, false, $"Unknown action '{request.Name}'."),
            };

            await WriteAuditAsync(
                request.CorrelationId,
                result.Success ? AuditDecision.Allow : result.BlockedByPolicy ? AuditDecision.Deny : AuditDecision.Error,
                result.Success,
                $"action.{action}",
                result.Message,
                cancellationToken);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await WriteAuditAsync(
                request.CorrelationId,
                AuditDecision.Error,
                false,
                $"action.{action}",
                "Action canceled.",
                CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            await WriteAuditAsync(
                request.CorrelationId,
                AuditDecision.Error,
                false,
                $"action.{action}",
                ex.Message,
                cancellationToken);
            return new MacroActionResult(false, false, ex.Message);
        }
    }

    private static Task<MacroActionResult> RunProgramAsync(MacroActionRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!request.Policy.AllowProcessStart)
        {
            return Task.FromResult(new MacroActionResult(false, true, "Policy blocked process start."));
        }

        if (request.Arguments.Count == 0)
        {
            return Task.FromResult(new MacroActionResult(false, false, "Missing executable path."));
        }

        var fileName = request.Arguments[0];
        var arguments = request.Arguments.Count > 1 ? string.Join(' ', request.Arguments.Skip(1)) : string.Empty;
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = true,
        };
        _ = Process.Start(psi);
        return Task.FromResult(new MacroActionResult(true, false, "Program started."));
    }

    private static Task<MacroActionResult> OpenFileOrUrlAsync(
        MacroActionRequest request,
        bool isUrl,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!request.Policy.AllowExternalOpen)
        {
            return Task.FromResult(new MacroActionResult(false, true, "Policy blocked external open."));
        }

        if (request.Arguments.Count == 0)
        {
            return Task.FromResult(new MacroActionResult(false, false, isUrl ? "Missing URL." : "Missing file path."));
        }

        var target = request.Arguments[0];
        if (!isUrl && !File.Exists(target))
        {
            return Task.FromResult(new MacroActionResult(false, false, $"File not found: {target}"));
        }

        var psi = new ProcessStartInfo
        {
            FileName = target,
            UseShellExecute = true,
        };
        _ = Process.Start(psi);
        return Task.FromResult(new MacroActionResult(true, false, isUrl ? "URL opened." : "File opened."));
    }

    private static Task<MacroActionResult> OpenExplorerAsync(MacroActionRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!request.Policy.AllowExternalOpen)
        {
            return Task.FromResult(new MacroActionResult(false, true, "Policy blocked explorer open."));
        }

        var path = request.Arguments.Count > 0 ? request.Arguments[0] : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var psi = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{path}\"",
            UseShellExecute = true,
        };
        _ = Process.Start(psi);
        return Task.FromResult(new MacroActionResult(true, false, "Explorer opened."));
    }

    private static async Task<MacroActionResult> WriteFileAsync(MacroActionRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!request.Policy.AllowFileSystemWrite)
        {
            return new MacroActionResult(false, true, "Policy blocked file write.");
        }

        if (request.Arguments.Count < 2)
        {
            return new MacroActionResult(false, false, "write-file requires path and content.");
        }

        var path = request.Arguments[0];
        var content = string.Join(' ', request.Arguments.Skip(1));
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, content, cancellationToken);
        return new MacroActionResult(true, false, "File written.", path);
    }

    private static Task<MacroActionResult> NotifyAsync(MacroActionRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!request.Policy.AllowNotifications)
        {
            return Task.FromResult(new MacroActionResult(false, true, "Policy blocked notification."));
        }

        var text = request.Arguments.Count == 0 ? "Texty macro notification." : string.Join(' ', request.Arguments);
        Trace.TraceInformation($"Texty notification: {text}");
        return Task.FromResult(new MacroActionResult(true, false, "Notification emitted.", text));
    }

    private async Task<MacroActionResult> RunPowerShellAsync(MacroActionRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!request.Policy.AllowPowerShell)
        {
            return new MacroActionResult(false, true, "Policy blocked PowerShell action.");
        }

        if (request.Arguments.Count == 0)
        {
            return new MacroActionResult(false, false, "Missing PowerShell script.");
        }

        var script = string.Join(' ', request.Arguments);
        var result = await _powerShellActionRunner.ExecuteAsync(
            script,
            new PowerShellExecutionPolicy(true, "macro-action"),
            cancellationToken);
        return result.Success
            ? new MacroActionResult(true, false, "PowerShell action executed.", result.Output)
            : new MacroActionResult(false, false, "PowerShell action failed.", result.Error);
    }

    private Task WriteAuditAsync(
        string correlationId,
        AuditDecision decision,
        bool success,
        string action,
        string message,
        CancellationToken cancellationToken)
    {
        return _auditLogger.WriteAsync(
            new AuditLogEntry(
                DateTimeOffset.UtcNow,
                AuditCategory.Action,
                action,
                decision,
                correlationId,
                success,
                message),
            cancellationToken);
    }
}
