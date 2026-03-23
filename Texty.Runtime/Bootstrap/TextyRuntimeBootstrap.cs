using Texty.AI;
using Texty.AI.Providers;
using Texty.Core.Interfaces;
using Texty.Core.Models;
using Texty.Integrations;
using Texty.Integrations.Resolvers;
using Texty.OutlookAddin;
using Texty.Runtime.Clipboard;
using Texty.Runtime.Audit;
using Texty.Runtime.Insertion;
using Texty.Runtime.Macros;
using Texty.Runtime.Security;
using Texty.Runtime.Services;
using Texty.Runtime.Sync;
using Texty.Runtime.Templating;
using Texty.Runtime.Triggering;
using Texty.Storage.Json;
using Texty.Storage.Json.Repositories;

namespace Texty.Runtime.Bootstrap;

public static class TextyRuntimeBootstrap
{
    private static readonly HttpClient SharedHttpClient = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    });

    public static async Task<TextyRuntimeContext> CreateDefaultAsync(string? storageRoot = null, CancellationToken cancellationToken = default)
    {
        var root = storageRoot ??
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Texty", "data");

        var jsonOptions = new JsonStorageOptions { RootDirectory = root };
        Directory.CreateDirectory(jsonOptions.SnippetsDirectory);
        Directory.CreateDirectory(jsonOptions.FoldersDirectory);
        Directory.CreateDirectory(jsonOptions.VersionsDirectory);
        Directory.CreateDirectory(jsonOptions.TrashDirectory);
        Directory.CreateDirectory(jsonOptions.AssetsDirectory);

        ISnippetRepository snippetRepository = new JsonSnippetRepository(jsonOptions);
        IFolderRepository folderRepository = new JsonFolderRepository(jsonOptions);
        IVersionRepository versionRepository = new JsonVersionRepository(jsonOptions);
        ITrashRepository trashRepository = new JsonTrashRepository(jsonOptions);
        ISnippetSearchIndex searchIndex = new JsonSnippetSearchIndex();
        IAuditLogger auditLogger = new JsonlAuditLogger(root);

        await EnsureDefaultDataAsync(snippetRepository, folderRepository, cancellationToken);
        await searchIndex.RebuildAsync(await snippetRepository.GetAllAsync(cancellationToken), cancellationToken);

        var triggerEvaluator = new TriggerEvaluator();
        IForegroundProcessProvider? foregroundProcessProvider = OperatingSystem.IsWindows()
            ? new WindowsForegroundProcessProvider()
            : null;
        IClipboardGateway clipboardGateway = OperatingSystem.IsWindows()
            ? new WindowsClipboardGateway()
            : new InMemoryClipboardGateway();
        IKeystrokeEmitter keyEmitter = OperatingSystem.IsWindows()
            ? new WindowsKeystrokeEmitter()
            : new NoOpKeystrokeEmitter();
        ITriggerProvider triggerProvider;
        if (OperatingSystem.IsWindows())
        {
            var hotkeyProvider = new WindowsKeyboardHookTriggerProvider(foregroundProcessProvider!);
            var clipboardProvider = new ClipboardTriggerProvider(clipboardGateway, foregroundProcessProvider!);
            triggerProvider = new CompositeTriggerProvider("WindowsCompositeTriggerProvider", [hotkeyProvider, clipboardProvider]);
        }
        else
        {
            triggerProvider = new NoOpTriggerProvider("NoOpTriggerProvider");
        }

        var templateRenderer = new TemplateRenderer();
        var formSchemaValidator = new FormSchemaValidator();
        var insertionPipeline = new ClipboardInsertionPipeline(
            clipboardGateway,
            keyEmitter,
            foregroundProcessProvider,
            auditLogger);

        var resolvers = new IExternalDataResolver[]
        {
            new EnvResolver(auditLogger),
            new CsvResolver(auditLogger),
            new XmlResolver(auditLogger),
            new SqlResolver(auditLogger),
            new AdResolver(auditLogger),
            new ExcelResolver(auditLogger),
        };
        var resolverFactory = new CompositeExternalDataResolverFactory(resolvers);

        var httpClient = SharedHttpClient;
        IAiProvider openAi = new OpenAiProvider(httpClient);
        IAiProvider openRouter = new OpenRouterProvider(httpClient);
        IAiProvider groq = new GroqProvider(httpClient);
        IAiProvider langdock = new LangdockProvider(httpClient);
        IAiProvider anthropic = new AnthropicProvider(httpClient);
        IAiProvider ollama = new OllamaProvider(httpClient);
        IAiProvider gpt4All = new Gpt4AllProvider(httpClient);
        var aiRegistry = new AiProviderRegistry([openAi, openRouter, groq, langdock, anthropic, ollama, gpt4All]);
        var aiHealthService = new AiProviderHealthService(aiRegistry);

        ITranslationProvider openAiTranslation = new OpenAiTranslationProvider(openAi);
        var translationRegistry = new TranslationProviderRegistry([openAiTranslation]);

        var powerShellActionRunner = new PowerShellActionRunner(auditLogger);
        var macroActionExecutor = new MacroActionExecutor(powerShellActionRunner, auditLogger);
        var macroEngine = new MacroEngine(new BasicDslFunctionLibrary(), macroActionExecutor);
        var authProvider = new LocalAuthContextProvider();
        var rolePolicy = new RolePolicyService(
            new Dictionary<string, RoleName>(StringComparer.OrdinalIgnoreCase)
            {
                [$"{Environment.UserDomainName}\\{Environment.UserName}"] = RoleName.Owner,
            });
        var licenseService = new InMemoryLicenseService();
        ISecretProtector secretProtector = OperatingSystem.IsWindows()
            ? new DpapiSecretProtector()
            : new NoOpSecretProtector();
        var sync = new FolderSyncOrchestrator();

        var maintenance = new SnippetMaintenanceService(snippetRepository, searchIndex);
        ISnippetVersioningService snippetVersioning = new SnippetVersioningService(snippetRepository, versionRepository);
        ISnippetWorkflowService snippetWorkflow = new SnippetWorkflowService(
            snippetRepository,
            trashRepository,
            searchIndex,
            snippetVersioning);
        var docGenerator = new DocumentGeneratorService();
        var textCorrection = new TextCorrectionService();
        var clipboardHistory = new ClipboardHistoryService();
        var productivityStats = new ProductivityStatsService();
        var hotkeyInsertionService = new HotkeyInsertionService(
            snippetRepository,
            triggerProvider,
            triggerEvaluator,
            templateRenderer,
            insertionPipeline,
            productivityStats);
        var importService = new FileImportService(snippetRepository, DefaultFolderId, jsonOptions.AssetsDirectory, auditLogger);

        var outlookBridge = new OutlookAddinBridge(new GenderOMaticService());

        return new TextyRuntimeContext(
            snippetRepository,
            folderRepository,
            versionRepository,
            trashRepository,
            searchIndex,
            triggerProvider,
            triggerEvaluator,
            hotkeyInsertionService,
            templateRenderer,
            formSchemaValidator,
            insertionPipeline,
            resolverFactory,
            openAi,
            openAiTranslation,
            aiRegistry,
            aiHealthService,
            translationRegistry,
            macroEngine,
            macroActionExecutor,
            powerShellActionRunner,
            auditLogger,
            authProvider,
            rolePolicy,
            licenseService,
            secretProtector,
            sync,
            maintenance,
            snippetVersioning,
            snippetWorkflow,
            docGenerator,
            textCorrection,
            clipboardHistory,
            productivityStats,
            outlookBridge,
            importService);
    }

    public static Guid DefaultFolderId { get; } = Guid.Parse("7d31545f-1306-413f-8f30-ddf6ad6f3886");

    private static async Task EnsureDefaultDataAsync(
        ISnippetRepository snippets,
        IFolderRepository folders,
        CancellationToken cancellationToken)
    {
        var folderList = await folders.GetAllAsync(cancellationToken);
        if (!folderList.Any(f => f.Id == DefaultFolderId))
        {
            var now = DateTimeOffset.UtcNow;
            await folders.SaveAsync(
                new Folder(
                    DefaultFolderId,
                    "General",
                    null,
                    "#0EA5A5",
                    0,
                    now,
                    now),
                cancellationToken);
        }

        var existing = await snippets.GetAllAsync(cancellationToken);
        if (!existing.Any())
        {
            var now = DateTimeOffset.UtcNow;
            await snippets.SaveAsync(
                new Snippet(
                    Guid.NewGuid(),
                    DefaultFolderId,
                    "Welcome Snippet",
                    "txwelcome",
                    "Hello {{name}},\nthis is your first Texty snippet.",
                    "<p>Hello <strong>{{name}}</strong>,<br/>this is your first Texty snippet.</p>",
                    [new Tag("welcome")],
                    [],
                    [new TriggerRule(Guid.NewGuid(), TriggerType.Autotext, "txwelcome", false, TriggerScope.Any, null, true)],
                    new SnippetTemplate(
                        "<p>Hello <strong>{{name}}</strong>,<br/>this is your first Texty snippet.</p>",
                        [new TemplateField("name", "Name", FormFieldType.Text, true, "Recipient name", null, null, null, null)]),
                    SnippetHighlightMode.None,
                    "Segoe UI",
                    false,
                    now,
                    now,
                    "system"),
                cancellationToken);
        }
    }
}
