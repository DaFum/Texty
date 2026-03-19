using Texty.AI;
using Texty.Core.Interfaces;
using Texty.Integrations;
using Texty.OutlookAddin;
using Texty.Runtime.Clipboard;
using Texty.Runtime.Services;
using Texty.Runtime.Triggering;

namespace Texty.Runtime.Bootstrap;

public sealed record TextyRuntimeContext(
    ISnippetRepository SnippetRepository,
    IFolderRepository FolderRepository,
    IVersionRepository VersionRepository,
    ITrashRepository TrashRepository,
    ISnippetSearchIndex SearchIndex,
    ITriggerProvider HotkeyTriggerProvider,
    ITriggerEvaluator TriggerEvaluator,
    HotkeyInsertionService HotkeyInsertionService,
    ITemplateRenderer TemplateRenderer,
    IFormSchemaValidator FormSchemaValidator,
    IInsertionPipeline InsertionPipeline,
    IExternalDataResolverFactory ExternalResolverFactory,
    IAiProvider DefaultAiProvider,
    ITranslationProvider DefaultTranslationProvider,
    AiProviderRegistry AiProviders,
    TranslationProviderRegistry TranslationProviders,
    IMacroEngine MacroEngine,
    IMacroActionExecutor MacroActionExecutor,
    IPowerShellActionRunner PowerShellActionRunner,
    IAuditLogger AuditLogger,
    IAuthContextProvider AuthContextProvider,
    IRolePolicyService RolePolicyService,
    ILicenseService LicenseService,
    ISecretProtector SecretProtector,
    ISyncOrchestrator SyncOrchestrator,
    SnippetMaintenanceService SnippetMaintenanceService,
    ISnippetVersioningService SnippetVersioningService,
    ISnippetWorkflowService SnippetWorkflowService,
    DocumentGeneratorService DocumentGeneratorService,
    TextCorrectionService TextCorrectionService,
    ClipboardHistoryService ClipboardHistoryService,
    ProductivityStatsService ProductivityStatsService,
    OutlookAddinBridge OutlookAddinBridge,
    FileImportService ImportService) : IDisposable
{
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        var tracked = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var candidate in EnumerateDisposableCandidates())
        {
            if (candidate is IDisposable disposable && tracked.Add(candidate))
            {
                disposable.Dispose();
            }
        }
    }

    private IEnumerable<object> EnumerateDisposableCandidates()
    {
        yield return SnippetRepository;
        yield return FolderRepository;
        yield return VersionRepository;
        yield return TrashRepository;
        yield return SearchIndex;
        yield return HotkeyTriggerProvider;
        yield return TriggerEvaluator;
        yield return HotkeyInsertionService;
        yield return TemplateRenderer;
        yield return FormSchemaValidator;
        yield return InsertionPipeline;
        yield return ExternalResolverFactory;
        yield return DefaultAiProvider;
        yield return DefaultTranslationProvider;
        yield return AiProviders;
        yield return TranslationProviders;
        yield return MacroEngine;
        yield return MacroActionExecutor;
        yield return PowerShellActionRunner;
        yield return AuditLogger;
        yield return AuthContextProvider;
        yield return RolePolicyService;
        yield return LicenseService;
        yield return SecretProtector;
        yield return SyncOrchestrator;
        yield return SnippetMaintenanceService;
        yield return SnippetVersioningService;
        yield return SnippetWorkflowService;
        yield return DocumentGeneratorService;
        yield return TextCorrectionService;
        yield return ClipboardHistoryService;
        yield return ProductivityStatsService;
        yield return OutlookAddinBridge;
        yield return ImportService;
    }
}
