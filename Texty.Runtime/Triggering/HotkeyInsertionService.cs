using System.Diagnostics;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Triggering;

public sealed class HotkeyInsertionService
{
    private readonly ISnippetRepository _snippetRepository;
    private readonly ITriggerProvider _triggerProvider;
    private readonly ITriggerEvaluator _triggerEvaluator;
    private readonly IInsertionPipeline _insertionPipeline;
    private readonly Services.ProductivityStatsService? _productivityStatsService;

    public HotkeyInsertionService(
        ISnippetRepository snippetRepository,
        ITriggerProvider triggerProvider,
        ITriggerEvaluator triggerEvaluator,
        IInsertionPipeline insertionPipeline,
        Services.ProductivityStatsService? productivityStatsService = null)
    {
        _snippetRepository = snippetRepository;
        _triggerProvider = triggerProvider;
        _triggerEvaluator = triggerEvaluator;
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
        var snippets = await _snippetRepository.GetAllAsync(cancellationToken);
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

        var payload = new InsertionPayload(snippet.PlainText, snippet.HtmlText, []);
        var insertionContext = new InsertionContext(
            match.Rule.TargetProcess,
            signal.Scope == TriggerScope.Email,
            signal.Scope == TriggerScope.TextFile,
            null);

        var results = await _insertionPipeline.ExecuteAsync(payload, insertionContext, cancellationToken);
        if (results.Any(r => r.Step == InsertionStep.Insert && r.Success))
        {
            _productivityStatsService?.TrackInsertion();
        }
    }
}
