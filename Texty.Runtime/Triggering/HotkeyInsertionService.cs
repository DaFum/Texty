using System.Diagnostics;
using System.Runtime.InteropServices;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Triggering;

public sealed class HotkeyInsertionService
{
    private static readonly TimeSpan SnippetCacheDuration = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan HotkeyReleaseTimeout = TimeSpan.FromMilliseconds(350);

    private readonly ISnippetRepository _snippetRepository;
    private readonly ITriggerProvider _triggerProvider;
    private readonly ITriggerEvaluator _triggerEvaluator;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly IInsertionPipeline _insertionPipeline;
    private readonly Services.ProductivityStatsService? _productivityStatsService;
    private IReadOnlyList<Snippet>? _cachedSnippets;
    private DateTimeOffset _cachedSnippetsAt;

    public HotkeyInsertionService(
        ISnippetRepository snippetRepository,
        ITriggerProvider triggerProvider,
        ITriggerEvaluator triggerEvaluator,
        ITemplateRenderer templateRenderer,
        IInsertionPipeline insertionPipeline,
        Services.ProductivityStatsService? productivityStatsService = null)
    {
        _snippetRepository = snippetRepository;
        _triggerProvider = triggerProvider;
        _triggerEvaluator = triggerEvaluator;
        _templateRenderer = templateRenderer;
        _insertionPipeline = insertionPipeline;
        _productivityStatsService = productivityStatsService;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await foreach (var signal in _triggerProvider.ListenAsync(cancellationToken))
        {
            try
            {
                await HandleSignalAsync(signal, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Trigger insertion failed: {ex}");
            }
        }
    }

    private async Task HandleSignalAsync(TriggerSignal signal, CancellationToken cancellationToken)
    {
        var snippets = await GetSnippetsCachedAsync(cancellationToken);
        var snippetByRuleId = new Dictionary<Guid, Snippet>();
        var rules = new List<TriggerRule>();

        foreach (var snippetItem in snippets.Where(s => !s.Deleted))
        {
            foreach (var rule in snippetItem.Triggers.Where(r => r.Enabled && r.Type == signal.Type))
            {
                rules.Add(rule);
                snippetByRuleId[rule.Id] = snippetItem;
            }
        }

        if (rules.Count == 0)
        {
            return;
        }

        var matches = await _triggerEvaluator.EvaluateAsync(rules, signal, cancellationToken);
        var match = matches.FirstOrDefault();
        if (match is null || !snippetByRuleId.TryGetValue(match.Rule.Id, out var snippet))
        {
            return;
        }

        TemplateRenderResult renderResult;
        try
        {
            renderResult = await _templateRenderer.RenderAsync(
                snippet,
                BuildRenderContext(signal, match),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Trace.TraceWarning($"Template render failed for snippet '{snippet.Id}': {ex.Message}");
            renderResult = new TemplateRenderResult(snippet.PlainText, snippet.HtmlText);
        }

        var payload = new InsertionPayload(renderResult.PlainText, renderResult.HtmlText, []);
        var insertionContext = new InsertionContext(
            match.Rule.TargetProcess,
            signal.Scope == TriggerScope.Email,
            signal.Scope == TriggerScope.TextFile,
            null);

        await WaitForHotkeyModifiersReleasedAsync(signal, cancellationToken);
        var results = await _insertionPipeline.ExecuteAsync(payload, insertionContext, cancellationToken);
        if (results.Any(r => r.Step == InsertionStep.Insert && r.Success))
        {
            _productivityStatsService?.TrackInsertion();
        }
    }

    private async Task<IReadOnlyList<Snippet>> GetSnippetsCachedAsync(CancellationToken cancellationToken)
    {
        if (_cachedSnippets is not null &&
            DateTimeOffset.UtcNow - _cachedSnippetsAt < SnippetCacheDuration)
        {
            return _cachedSnippets;
        }

        var snippets = await _snippetRepository.GetAllAsync(cancellationToken);
        _cachedSnippets = snippets;
        _cachedSnippetsAt = DateTimeOffset.UtcNow;
        return snippets;
    }

    private static RenderContext BuildRenderContext(TriggerSignal signal, TriggerMatch match)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["trigger.input"] = signal.Input,
            ["trigger.process"] = signal.ProcessName ?? string.Empty,
            ["trigger.scope"] = signal.Scope.ToString(),
            ["trigger.type"] = signal.Type.ToString(),
            ["trigger.pattern"] = match.Rule.Pattern,
        };
        return new RenderContext(values);
    }

    private static async Task WaitForHotkeyModifiersReleasedAsync(TriggerSignal signal, CancellationToken cancellationToken)
    {
        if (signal.Type != TriggerType.Hotkey || !OperatingSystem.IsWindows())
        {
            return;
        }

        var requiresCtrl = signal.Input.Contains("CTRL", StringComparison.OrdinalIgnoreCase);
        var requiresShift = signal.Input.Contains("SHIFT", StringComparison.OrdinalIgnoreCase);
        var requiresAlt = signal.Input.Contains("ALT", StringComparison.OrdinalIgnoreCase);
        var requiresWin = signal.Input.Contains("WIN", StringComparison.OrdinalIgnoreCase);
        if (!requiresCtrl && !requiresShift && !requiresAlt && !requiresWin)
        {
            return;
        }

        var started = DateTime.UtcNow;
        while (DateTime.UtcNow - started < HotkeyReleaseTimeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var ctrlPressed = requiresCtrl && IsPressed(VkControl);
            var shiftPressed = requiresShift && IsPressed(VkShift);
            var altPressed = requiresAlt && IsPressed(VkMenu);
            var winPressed = requiresWin && (IsPressed(VkLWin) || IsPressed(VkRWin));
            if (!ctrlPressed && !shiftPressed && !altPressed && !winPressed)
            {
                return;
            }

            await Task.Delay(10, cancellationToken);
        }
    }

    private const int VkShift = 0x10;
    private const int VkControl = 0x11;
    private const int VkMenu = 0x12;
    private const int VkLWin = 0x5B;
    private const int VkRWin = 0x5C;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private static bool IsPressed(int virtualKey)
    {
        return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
    }
}
