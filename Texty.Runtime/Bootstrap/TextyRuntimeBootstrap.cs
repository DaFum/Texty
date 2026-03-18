using Texty.AI;
using Texty.AI.Providers;
using Texty.Core.Interfaces;
using Texty.Core.Models;
using Texty.Integrations;
using Texty.Integrations.Resolvers;
using Texty.OutlookAddin;
using Texty.Runtime.Clipboard;
using Texty.Runtime.Insertion;
using Texty.Runtime.Macros;
using Texty.Runtime.Security;
using Texty.Runtime.Services;
using Texty.Runtime.Sync;
using Texty.Runtime.Templating;
using Texty.Runtime.Triggering;
using Texty.Storage.Json;
using Texty.Storage.Json.Repositories;
using Texty.Storage.SqlServer;
using Texty.Storage.SqlServer.Repositories;

namespace Texty.Runtime.Bootstrap;

public static class TextyRuntimeBootstrap
{
    /// <summary>
    /// Erstellt und konfiguriert einen standardmäßigen TextyRuntimeContext mit lokalem JSON‑Speicher, optionaler SQL‑Team‑Unterstützung sowie allen erforderlichen Repositories, Diensten und Provider‑Registries.
    /// </summary>
    /// <param name="storageRoot">Optionales Verzeichnis für die lokale JSON‑Speicherung. Falls null, wird ein Standardpfad unter LocalApplicationData/Texty/data verwendet.</param>
    /// <param name="cancellationToken">Token zum Abbrechen asynchroner Initialisierungsschritte.</param>
    /// <returns>Eine vollständig initialisierte TextyRuntimeContext‑Instanz mit Repositories, Suchindex, Trigger‑/Template‑Komponenten, AI‑ und Übersetzungs‑Providern sowie weiteren Runtime‑Diensten.</returns>
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

        var sqlOptions = new SqlServerStorageOptions { UseInMemoryFallback = true };
        var sqlState = new SqlServerStorageState();
        ISnippetRepository teamSnippetRepository = new SqlServerSnippetRepository(sqlOptions, sqlState);
        IFolderRepository teamFolderRepository = new SqlServerFolderRepository(sqlState);
        IVersionRepository teamVersionRepository = new SqlServerVersionRepository(sqlState);
        ITrashRepository teamTrashRepository = new SqlServerTrashRepository(sqlState);

        await EnsureDefaultDataAsync(snippetRepository, folderRepository, cancellationToken);
        await searchIndex.RebuildAsync(await snippetRepository.GetAllAsync(cancellationToken), cancellationToken);

        var triggerEvaluator = new TriggerEvaluator();
        var templateRenderer = new TemplateRenderer();
        var formSchemaValidator = new FormSchemaValidator();
        var clipboardGateway = new InMemoryClipboardGateway();
        var keyEmitter = new NoOpKeystrokeEmitter();
        var insertionPipeline = new ClipboardInsertionPipeline(clipboardGateway, keyEmitter);

        var resolvers = new IExternalDataResolver[]
        {
            new EnvResolver(),
            new CsvResolver(),
            new XmlResolver(),
            new SqlResolver(),
            new AdResolver(),
            new ExcelResolver(),
        };
        var resolverFactory = new CompositeExternalDataResolverFactory(resolvers);

        var httpClient = new HttpClient();
        IAiProvider openAi = new OpenAiProvider(httpClient);
        IAiProvider openRouter = new OpenRouterProvider(httpClient);
        IAiProvider groq = new GroqProvider(httpClient);
        IAiProvider langdock = new LangdockProvider(httpClient);
        var aiRegistry = new AiProviderRegistry([openAi, openRouter, groq, langdock]);

        ITranslationProvider deepL = new DeepLTranslationProvider(httpClient);
        ITranslationProvider openAiTranslation = new OpenAiTranslationProvider(openAi);
        var translationRegistry = new TranslationProviderRegistry([deepL, openAiTranslation]);

        var macroEngine = new MacroEngine(new BasicDslFunctionLibrary());
        var powerShellActionRunner = new PowerShellActionRunner();
        var authProvider = new LocalAuthContextProvider();
        var rolePolicy = new RolePolicyService(
            new Dictionary<string, RoleName>(StringComparer.OrdinalIgnoreCase)
            {
                [$"{Environment.UserDomainName}\\{Environment.UserName}"] = RoleName.Owner,
            });
        var licenseService = new InMemoryLicenseService();
        var secretProtector = new DpapiSecretProtector();
        var sync = new FolderSyncOrchestrator();

        var maintenance = new SnippetMaintenanceService(snippetRepository);
        var docGenerator = new DocumentGeneratorService();
        var textCorrection = new TextCorrectionService();
        var clipboardHistory = new ClipboardHistoryService();
        var productivityStats = new ProductivityStatsService();
        var importService = new FileImportService(snippetRepository, DefaultFolderId);

        var outlookBridge = new OutlookAddinBridge(new GenderOMaticService());

        return new TextyRuntimeContext(
            snippetRepository,
            folderRepository,
            versionRepository,
            trashRepository,
            teamSnippetRepository,
            teamFolderRepository,
            teamVersionRepository,
            teamTrashRepository,
            searchIndex,
            triggerEvaluator,
            templateRenderer,
            formSchemaValidator,
            insertionPipeline,
            resolverFactory,
            openAi,
            deepL,
            aiRegistry,
            translationRegistry,
            macroEngine,
            powerShellActionRunner,
            authProvider,
            rolePolicy,
            licenseService,
            secretProtector,
            sync,
            maintenance,
            docGenerator,
            textCorrection,
            clipboardHistory,
            productivityStats,
            outlookBridge,
            importService);
    }

    public static Guid DefaultFolderId { get; } = Guid.Parse("7d31545f-1306-413f-8f30-ddf6ad6f3886");

    /// <summary>
    /// Stellt sicher, dass mindestens ein Standardordner und ein Standardsnippet vorhanden sind; falls keine Ordner existieren, wird der Ordner "General" mit <see cref="DefaultFolderId"/> angelegt, und falls keine Snippets existieren, wird ein "Welcome Snippet" erstellt.
    /// </summary>
    /// <param name="snippets">Repository zum Lesen und Speichern von Snippets; wird verwendet, um bei Bedarf ein Willkommenssnippet anzulegen.</param>
    /// <param name="folders">Repository zum Lesen und Speichern von Ordnern; wird verwendet, um bei Bedarf den Standardordner anzulegen.</param>
    /// <param name="cancellationToken">Abbruchtoken für die asynchronen Operationen.</param>
    private static async Task EnsureDefaultDataAsync(
        ISnippetRepository snippets,
        IFolderRepository folders,
        CancellationToken cancellationToken)
    {
        var folderList = await folders.GetAllAsync(cancellationToken);
        if (!folderList.Any())
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
