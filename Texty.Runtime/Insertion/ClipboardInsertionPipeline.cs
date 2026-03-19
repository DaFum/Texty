using Texty.Core.Interfaces;
using Texty.Core.Models;
using Texty.Runtime.Audit;
using System.Runtime.ExceptionServices;

namespace Texty.Runtime.Insertion;

public sealed class ClipboardInsertionPipeline : IInsertionPipeline
{
    private readonly IClipboardGateway _clipboardGateway;
    private readonly IKeystrokeEmitter _keystrokeEmitter;
    private readonly IForegroundProcessProvider? _foregroundProcessProvider;
    private readonly IAuditLogger _auditLogger;

    public ClipboardInsertionPipeline(
        IClipboardGateway clipboardGateway,
        IKeystrokeEmitter keystrokeEmitter,
        IForegroundProcessProvider? foregroundProcessProvider = null,
        IAuditLogger? auditLogger = null)
    {
        _clipboardGateway = clipboardGateway;
        _keystrokeEmitter = keystrokeEmitter;
        _foregroundProcessProvider = foregroundProcessProvider;
        _auditLogger = auditLogger ?? NoOpAuditLogger.Instance;
    }

    public async Task<IReadOnlyList<InsertionStepResult>> ExecuteAsync(
        InsertionPayload payload,
        InsertionContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<InsertionStepResult>();
        ClipboardItem? snapshot = null;
        var correlationId = CreateCorrelationId(context);
        ExceptionDispatchInfo? capturedException = null;

        if (!CanInsertIntoTarget(context.TargetProcess))
        {
            await _auditLogger.WriteAsync(
                new AuditLogEntry(
                    DateTimeOffset.UtcNow,
                    AuditCategory.Insertion,
                    "target-process-check",
                    AuditDecision.Deny,
                    correlationId,
                    false,
                    $"Insertion blocked for target '{context.TargetProcess ?? "<any>"}'.",
                    new Dictionary<string, string>
                    {
                        ["targetProcess"] = context.TargetProcess ?? string.Empty,
                        ["isEmail"] = context.IsEmail ? "true" : "false",
                        ["isTextFile"] = context.IsTextFile ? "true" : "false",
                    }),
                cancellationToken);
            results.Add(new InsertionStepResult(
                InsertionStep.Prepare,
                false,
                $"Insertion blocked. Foreground process does not match target '{context.TargetProcess}'."));            
            return results;
        }

        try
        {
            snapshot = await _clipboardGateway.SnapshotAsync(cancellationToken);
            results.Add(new InsertionStepResult(InsertionStep.Prepare, true, "Clipboard snapshot captured."));
            await _auditLogger.WriteAsync(
                new AuditLogEntry(
                    DateTimeOffset.UtcNow,
                    AuditCategory.Insertion,
                    "prepare",
                    AuditDecision.Allow,
                    correlationId,
                    true,
                    "Clipboard snapshot captured."),
                cancellationToken);

            await _clipboardGateway.SetAsync(new ClipboardItem(payload.PlainText, payload.HtmlText, null), cancellationToken);
            await _keystrokeEmitter.SendPasteAsync(cancellationToken);
            results.Add(new InsertionStepResult(InsertionStep.Insert, true, "Paste command emitted."));
            await _auditLogger.WriteAsync(
                new AuditLogEntry(
                    DateTimeOffset.UtcNow,
                    AuditCategory.Insertion,
                    "insert",
                    AuditDecision.Allow,
                    correlationId,
                    true,
                    "Paste emitted."),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await _auditLogger.WriteAsync(
                new AuditLogEntry(
                    DateTimeOffset.UtcNow,
                    AuditCategory.Insertion,
                    "insert",
                    AuditDecision.Error,
                    correlationId,
                    false,
                    "Insertion canceled."),
                CancellationToken.None);
            capturedException = ExceptionDispatchInfo.Capture(new OperationCanceledException(cancellationToken));
        }
        catch (Exception ex)
        {
            results.Add(new InsertionStepResult(InsertionStep.Insert, false, ex.Message));
            await _auditLogger.WriteAsync(
                new AuditLogEntry(
                    DateTimeOffset.UtcNow,
                    AuditCategory.Insertion,
                    "insert",
                    AuditDecision.Error,
                    correlationId,
                    false,
                    ex.Message),
                cancellationToken);
        }

        try
        {
            await _clipboardGateway.RestoreAsync(snapshot, cancellationToken);
            results.Add(new InsertionStepResult(InsertionStep.Restore, true, "Clipboard restored."));
            await _auditLogger.WriteAsync(
                new AuditLogEntry(
                    DateTimeOffset.UtcNow,
                    AuditCategory.Insertion,
                    "restore",
                    AuditDecision.Allow,
                    correlationId,
                    true,
                    "Clipboard restored."),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await _auditLogger.WriteAsync(
                new AuditLogEntry(
                    DateTimeOffset.UtcNow,
                    AuditCategory.Insertion,
                    "restore",
                    AuditDecision.Error,
                    correlationId,
                    false,
                    "Restore canceled."),
                CancellationToken.None);
            capturedException ??= ExceptionDispatchInfo.Capture(new OperationCanceledException(cancellationToken));
        }
        catch (Exception ex)
        {
            results.Add(new InsertionStepResult(InsertionStep.Restore, false, ex.Message));
            await _auditLogger.WriteAsync(
                new AuditLogEntry(
                    DateTimeOffset.UtcNow,
                    AuditCategory.Insertion,
                    "restore",
                    AuditDecision.Error,
                    correlationId,
                    false,
                    ex.Message),
                cancellationToken);
        }

        if (capturedException is not null)
        {
            capturedException.Throw();
        }

        var insertSucceeded = results.Any(r => r.Step == InsertionStep.Insert && r.Success);
        if (insertSucceeded && payload.PostActions.Count > 0)
        {
            try
            {
                await _keystrokeEmitter.SendSequenceAsync(payload.PostActions, cancellationToken);
                results.Add(new InsertionStepResult(InsertionStep.PostProcess, true, "Post-actions executed."));
                await _auditLogger.WriteAsync(
                    new AuditLogEntry(
                        DateTimeOffset.UtcNow,
                        AuditCategory.Insertion,
                        "post-actions",
                        AuditDecision.Allow,
                        correlationId,
                        true,
                        "Post-actions executed.",
                        new Dictionary<string, string> { ["count"] = payload.PostActions.Count.ToString() }),
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await _auditLogger.WriteAsync(
                    new AuditLogEntry(
                        DateTimeOffset.UtcNow,
                        AuditCategory.Insertion,
                        "post-actions",
                        AuditDecision.Error,
                        correlationId,
                        false,
                        "Post-actions canceled."),
                    CancellationToken.None);
                throw;
            }
            catch (Exception ex)
            {
                results.Add(new InsertionStepResult(InsertionStep.PostProcess, false, ex.Message));
                await _auditLogger.WriteAsync(
                    new AuditLogEntry(
                        DateTimeOffset.UtcNow,
                        AuditCategory.Insertion,
                        "post-actions",
                        AuditDecision.Error,
                        correlationId,
                        false,
                        ex.Message),
                    cancellationToken);
            }
        }

        return results;
    }

    private bool CanInsertIntoTarget(string? targetProcess)
    {
        if (string.IsNullOrWhiteSpace(targetProcess) || _foregroundProcessProvider is null)
        {
            return true;
        }

        var foreground = _foregroundProcessProvider.GetForegroundProcessName();
        return ProcessNameEquals(targetProcess, foreground);
    }

    private static bool ProcessNameEquals(string? expected, string? actual)
    {
        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(actual))
        {
            return false;
        }

        var normalizedExpected = NormalizeProcessName(expected);
        var normalizedActual = NormalizeProcessName(actual);
        return string.Equals(normalizedExpected, normalizedActual, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeProcessName(string processName)
    {
        var value = processName.Trim();
        return value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? value[..^4]
            : value;
    }

    private static string CreateCorrelationId(InsertionContext context)
    {
        if (context.Variables is not null &&
            context.Variables.TryGetValue("correlationId", out var correlationId) &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        return Guid.NewGuid().ToString("N");
    }
}
