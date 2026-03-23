using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.UI.Xaml.Media;
using Texty.App.Services;
using Texty.App.ViewModels.Models;
using Texty.Core.Interfaces;
using Texty.Core.Models;
using Texty.Core.Utilities;
using Texty.Runtime.Bootstrap;

namespace Texty.App.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private TextyRuntimeContext? _runtime;
    private CancellationTokenSource? _loadSnippetsCts;
    private long _snippetLoadRequestCounter;
    private long _latestSnippetLoadRequestId;
    private Guid? _editingTriggerRuleId;
    private string? _editingTemplateFieldKey;

    public MainViewModel()
    {
        Title = "Texty";
    }

    public ObservableCollection<FolderItemModel> Folders { get; } = [];

    public ObservableCollection<SnippetItemModel> VisibleSnippets { get; } = [];
    public ObservableCollection<TriggerRuleItemModel> TriggerRules { get; } = [];
    public ObservableCollection<VersionEntryItemModel> VersionEntries { get; } = [];
    public ObservableCollection<TrashEntryItemModel> TrashEntries { get; } = [];
    public ObservableCollection<TemplateFieldDesignerItemModel> TemplateFields { get; } = [];

    public IReadOnlyList<TriggerType> TriggerTypeOptions { get; } = Enum.GetValues<TriggerType>();
    public IReadOnlyList<TriggerScope> TriggerScopeOptions { get; } = Enum.GetValues<TriggerScope>();
    public IReadOnlyList<FormFieldType> TemplateFieldTypeOptions { get; } = Enum.GetValues<FormFieldType>();
    public IReadOnlyList<string> ImportFormatOptions { get; } = ["text", "html", "image", "outlook", "textexpander"];
    public IReadOnlyList<string> ResolverOptions { get; } = ["excel", "csv", "xml", "sql", "env", "ad"];

    [ObservableProperty]
    private FolderItemModel? _selectedFolder;

    [ObservableProperty]
    private SnippetItemModel? _selectedSnippet;

    [ObservableProperty]
    private TriggerRuleItemModel? _selectedTriggerRule;

    [ObservableProperty]
    private VersionEntryItemModel? _selectedVersionEntry;

    [ObservableProperty]
    private TrashEntryItemModel? _selectedTrashEntry;

    [ObservableProperty]
    private TemplateFieldDesignerItemModel? _selectedTemplateField;

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    private string _replaceTerm = string.Empty;

    [ObservableProperty]
    private string _replaceScopeTag = string.Empty;

    [ObservableProperty]
    private string _replaceScopeTargetProcess = string.Empty;

    [ObservableProperty]
    private bool _replaceScopeUseFolder = true;

    [ObservableProperty]
    private bool _replaceScopeUseSelection = true;

    [ObservableProperty]
    private bool _replaceScopeIncludeHidden;

    [ObservableProperty]
    private string _replaceResultText = string.Empty;

    [ObservableProperty]
    private string _editorTitle = string.Empty;

    [ObservableProperty]
    private string _editorPlainText = string.Empty;

    [ObservableProperty]
    private string _editorHtmlText = string.Empty;

    [ObservableProperty]
    private string _editorTagInput = string.Empty;

    [ObservableProperty]
    private string _editorCommentInput = string.Empty;

    [ObservableProperty]
    private TriggerType _triggerTypeInput = TriggerType.Autotext;

    [ObservableProperty]
    private TriggerScope _triggerScopeInput = TriggerScope.Any;

    [ObservableProperty]
    private string _triggerPatternInput = string.Empty;

    [ObservableProperty]
    private bool _triggerCaseSensitiveInput;

    [ObservableProperty]
    private string _triggerTargetProcessInput = string.Empty;

    [ObservableProperty]
    private bool _triggerEnabledInput = true;

    [ObservableProperty]
    private string _triggerEditorMode = "Neuer Trigger";

    [ObservableProperty]
    private string _statusText = "Bereit";

    [ObservableProperty]
    private string _documentPreviewText = string.Empty;

    [ObservableProperty]
    private string _clipboardPreviewText = string.Empty;

    [ObservableProperty]
    private string _importSourcePath = string.Empty;

    [ObservableProperty]
    private string _importFormat = "text";

    [ObservableProperty]
    private string _importResultText = string.Empty;

    [ObservableProperty]
    private string _resolverName = "env";

    [ObservableProperty]
    private string _resolverExpression = string.Empty;

    [ObservableProperty]
    private string _resolverResultText = string.Empty;

    [ObservableProperty]
    private string _folderNameInput = string.Empty;

    [ObservableProperty]
    private string _folderColorHexInput = "#0EA5A5";

    [ObservableProperty]
    private string _templateFieldKeyInput = string.Empty;

    [ObservableProperty]
    private string _templateFieldLabelInput = string.Empty;

    [ObservableProperty]
    private FormFieldType _templateFieldTypeInput = FormFieldType.Text;

    [ObservableProperty]
    private bool _templateFieldRequiredInput = true;

    [ObservableProperty]
    private string _templateFieldPlaceholderInput = string.Empty;

    [ObservableProperty]
    private string _templateFieldMinInput = string.Empty;

    [ObservableProperty]
    private string _templateFieldMaxInput = string.Empty;

    [ObservableProperty]
    private string _templateFieldDefaultValueInput = string.Empty;

    [ObservableProperty]
    private string _templateFieldOptionsInput = string.Empty;

    [ObservableProperty]
    private string _templateDesignerMode = "Neues Feld";

    [ObservableProperty]
    private IReadOnlyList<string> _aiProviderOptions = [];

    [ObservableProperty]
    private string _selectedAiProvider = "OpenAI";

    [ObservableProperty]
    private string _aiModelInput = string.Empty;

    [ObservableProperty]
    private string _aiPromptInput = string.Empty;

    [ObservableProperty]
    private string _aiResultText = string.Empty;

    [ObservableProperty]
    private string _aiHealthReportText = string.Empty;

    [ObservableProperty]
    private string _translationSourceLanguageInput = "de";

    [ObservableProperty]
    private string _translationTargetLanguageInput = "en";

    [ObservableProperty]
    private string _macroScriptInput = "set name Andre\nappend Hallo $name";

    [ObservableProperty]
    private string _macroVariablesInput = string.Empty;

    [ObservableProperty]
    private string _macroOutputText = string.Empty;

    [ObservableProperty]
    private string _macroErrorsText = string.Empty;

    [ObservableProperty]
    private string _macroAuditText = string.Empty;

    [ObservableProperty]
    private bool _macroAllowProcessStart;

    [ObservableProperty]
    private bool _macroAllowFileSystemWrite;

    [ObservableProperty]
    private bool _macroAllowExternalOpen;

    [ObservableProperty]
    private bool _macroAllowNotifications = true;

    [ObservableProperty]
    private bool _macroAllowPowerShell;

    [ObservableProperty]
    private string _macroPolicySummaryText = string.Empty;

    public async Task InitializeAsync()
    {
        await AppRuntimeService.InitializeAsync();
        _runtime = AppRuntimeService.RuntimeContext;
        AiProviderOptions = _runtime?.AiProviders.Names ?? [];
        if (AiProviderOptions.Count > 0 && !AiProviderOptions.Contains(SelectedAiProvider, StringComparer.OrdinalIgnoreCase))
        {
            SelectedAiProvider = AiProviderOptions[0];
        }

        UpdateMacroPolicySummary();
        await LoadFoldersAsync();
        await LoadSnippetsAsync();
        await LoadTrashEntriesAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadSnippetsAsync();
    }

    [RelayCommand]
    private async Task CreateFolderAsync()
    {
        if (_runtime is null)
        {
            return;
        }

        var name = FolderNameInput.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusText = "Bitte Ordnername angeben.";
            return;
        }

        var folders = await _runtime.FolderRepository.GetAllAsync();
        if (folders.Any(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText = $"Ordner '{name}' existiert bereits.";
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var nextSort = folders.Count == 0 ? 0 : folders.Max(f => f.SortOrder) + 1;
        var folder = new Folder(
            Guid.NewGuid(),
            name,
            null,
            NormalizeFolderColor(FolderColorHexInput),
            nextSort,
            now,
            now);
        await _runtime.FolderRepository.SaveAsync(folder);

        await LoadFoldersAsync();
        SelectedFolder = Folders.FirstOrDefault(x => x.Id == folder.Id);
        FolderNameInput = string.Empty;
        StatusText = $"Ordner '{folder.Name}' erstellt.";
    }

    [RelayCommand]
    private async Task RenameSelectedFolderAsync()
    {
        if (_runtime is null || SelectedFolder is null)
        {
            return;
        }

        var newName = FolderNameInput.Trim();
        if (string.IsNullOrWhiteSpace(newName))
        {
            StatusText = "Bitte neuen Ordnernamen eingeben.";
            return;
        }

        var folders = await _runtime.FolderRepository.GetAllAsync();
        if (folders.Any(f => f.Id != SelectedFolder.Id && string.Equals(f.Name, newName, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText = $"Ordner '{newName}' existiert bereits.";
            return;
        }

        var source = folders.FirstOrDefault(f => f.Id == SelectedFolder.Id);
        if (source is null)
        {
            StatusText = "Ausgewaehlter Ordner nicht gefunden.";
            return;
        }

        var updated = source with
        {
            Name = newName,
            ColorHex = NormalizeFolderColor(FolderColorHexInput),
            UpdatedUtc = DateTimeOffset.UtcNow,
        };
        await _runtime.FolderRepository.SaveAsync(updated);
        await LoadFoldersAsync();
        SelectedFolder = Folders.FirstOrDefault(x => x.Id == updated.Id);
        StatusText = $"Ordner in '{newName}' umbenannt.";
    }

    [RelayCommand]
    private async Task DeleteSelectedFolderAsync()
    {
        if (_runtime is null || SelectedFolder is null)
        {
            return;
        }

        var folders = (await _runtime.FolderRepository.GetAllAsync())
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (folders.Count <= 1)
        {
            StatusText = "Mindestens ein Ordner muss bestehen bleiben.";
            return;
        }

        var source = folders.FirstOrDefault(f => f.Id == SelectedFolder.Id);
        if (source is null)
        {
            return;
        }

        var fallback = folders.FirstOrDefault(f => f.Id != source.Id);
        if (fallback is null)
        {
            StatusText = "Kein Reassign-Ordner verfuegbar.";
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var snippets = await _runtime.SnippetRepository.GetAllAsync();
        var affected = snippets.Where(s => s.FolderId == source.Id).ToList();
        foreach (var snippet in affected)
        {
            await _runtime.SnippetWorkflowService.SaveAsync(
                snippet with
                {
                    FolderId = fallback.Id,
                    UpdatedUtc = now,
                    LastEditor = Environment.UserName,
                },
                createVersion: true);
        }

        await _runtime.FolderRepository.DeleteAsync(source.Id);
        await NormalizeFolderSortOrderAsync();

        await LoadFoldersAsync();
        SelectedFolder = Folders.FirstOrDefault(x => x.Id == fallback.Id);
        await LoadSnippetsAsync();
        StatusText = $"Ordner '{source.Name}' geloescht, {affected.Count} Bausteine nach '{fallback.Name}' verschoben.";
    }

    [RelayCommand]
    private async Task MoveFolderUpAsync()
    {
        if (_runtime is null || SelectedFolder is null)
        {
            return;
        }

        var folders = (await _runtime.FolderRepository.GetAllAsync())
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var index = folders.FindIndex(f => f.Id == SelectedFolder.Id);
        if (index <= 0)
        {
            return;
        }

        (folders[index - 1], folders[index]) = (folders[index], folders[index - 1]);
        await PersistFolderSortOrderAsync(folders);
        await LoadFoldersAsync();
        SelectedFolder = Folders.FirstOrDefault(x => x.Id == folders[index - 1].Id);
        StatusText = "Ordner nach oben verschoben.";
    }

    [RelayCommand]
    private async Task MoveFolderDownAsync()
    {
        if (_runtime is null || SelectedFolder is null)
        {
            return;
        }

        var folders = (await _runtime.FolderRepository.GetAllAsync())
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var index = folders.FindIndex(f => f.Id == SelectedFolder.Id);
        if (index < 0 || index >= folders.Count - 1)
        {
            return;
        }

        (folders[index + 1], folders[index]) = (folders[index], folders[index + 1]);
        await PersistFolderSortOrderAsync(folders);
        await LoadFoldersAsync();
        SelectedFolder = Folders.FirstOrDefault(x => x.Id == folders[index + 1].Id);
        StatusText = "Ordner nach unten verschoben.";
    }

    [RelayCommand]
    private async Task CreateSnippetAsync()
    {
        if (_runtime is null)
        {
            return;
        }

        var folderId = SelectedFolder?.Id ?? TextyRuntimeBootstrap.DefaultFolderId;
        var now = DateTimeOffset.UtcNow;
        var snippet = new Snippet(
            Guid.NewGuid(),
            folderId,
            "Neuer Baustein",
            "txnew",
            "Bitte Inhalt bearbeiten.",
            "<p>Bitte Inhalt bearbeiten.</p>",
            [],
            [],
            [],
            null,
            SnippetHighlightMode.None,
            "Segoe UI",
            false,
            now,
            now,
            Environment.UserName);

        await _runtime.SnippetWorkflowService.SaveAsync(snippet, createVersion: true);
        StatusText = "Neuer Baustein angelegt.";
        await LoadSnippetsAsync();
    }

    [RelayCommand]
    private async Task DuplicateSnippetAsync()
    {
        if (_runtime is null || SelectedSnippet is null)
        {
            return;
        }

        var source = SelectedSnippet.Source;
        var now = DateTimeOffset.UtcNow;
        var duplicate = source with
        {
            Id = Guid.NewGuid(),
            Title = $"{source.Title} (Kopie)",
            Shortcut = BuildCopyShortcut(source.Shortcut),
            Triggers = [],
            CreatedUtc = now,
            UpdatedUtc = now,
            LastEditor = Environment.UserName,
            Deleted = false,
        };

        await _runtime.SnippetWorkflowService.SaveAsync(duplicate, createVersion: true);
        StatusText = "Baustein dupliziert.";
        await LoadSnippetsAsync();
        SelectedSnippet = VisibleSnippets.FirstOrDefault(x => x.Id == duplicate.Id);
    }

    [RelayCommand]
    private async Task SaveSnippetAsync()
    {
        if (_runtime is null || SelectedSnippet is null)
        {
            return;
        }

        var baseSnippet = SelectedSnippet.Source;
        var normalizedHtml = string.IsNullOrWhiteSpace(EditorHtmlText) ? EditorPlainText : EditorHtmlText;
        var synchronizedTemplate = baseSnippet.Template is null
            ? null
            : baseSnippet.Template with { BodyTemplate = normalizedHtml };

        var updated = baseSnippet with
        {
            Title = EditorTitle,
            PlainText = EditorPlainText,
            HtmlText = normalizedHtml,
            Template = synchronizedTemplate,
            UpdatedUtc = DateTimeOffset.UtcNow,
            LastEditor = Environment.UserName,
        };

        await _runtime.SnippetWorkflowService.SaveAsync(updated, createVersion: true);

        StatusText = "Baustein gespeichert.";
        await LoadSnippetsAsync();
    }

    [RelayCommand]
    private async Task MoveSnippetToSelectedFolderAsync()
    {
        if (_runtime is null || SelectedSnippet is null || SelectedFolder is null)
        {
            return;
        }

        var source = SelectedSnippet.Source;
        if (source.FolderId == SelectedFolder.Id)
        {
            StatusText = "Baustein ist bereits im gewaehlten Ordner.";
            return;
        }

        await _runtime.SnippetWorkflowService.SaveAsync(
            source with
            {
                FolderId = SelectedFolder.Id,
                UpdatedUtc = DateTimeOffset.UtcNow,
                LastEditor = Environment.UserName,
            },
            createVersion: true);

        StatusText = "Baustein verschoben.";
        await LoadSnippetsAsync();
    }

    [RelayCommand]
    private async Task ToggleHiddenAsync()
    {
        if (_runtime is null || SelectedSnippet is null)
        {
            return;
        }

        var source = SelectedSnippet.Source;
        var nextMode = source.HighlightMode == SnippetHighlightMode.Hidden
            ? SnippetHighlightMode.None
            : SnippetHighlightMode.Hidden;

        await _runtime.SnippetWorkflowService.SaveAsync(
            source with
            {
                HighlightMode = nextMode,
                UpdatedUtc = DateTimeOffset.UtcNow,
                LastEditor = Environment.UserName,
            },
            createVersion: true);

        StatusText = nextMode == SnippetHighlightMode.Hidden
            ? "Baustein ausgeblendet."
            : "Baustein wieder eingeblendet.";
        await LoadSnippetsAsync();
    }

    [RelayCommand]
    private async Task ToggleHighlightAsync()
    {
        if (_runtime is null || SelectedSnippet is null)
        {
            return;
        }

        var source = SelectedSnippet.Source;
        var nextMode = source.HighlightMode == SnippetHighlightMode.Highlighted
            ? SnippetHighlightMode.None
            : SnippetHighlightMode.Highlighted;

        await _runtime.SnippetWorkflowService.SaveAsync(
            source with
            {
                HighlightMode = nextMode,
                UpdatedUtc = DateTimeOffset.UtcNow,
                LastEditor = Environment.UserName,
            },
            createVersion: true);

        StatusText = nextMode == SnippetHighlightMode.Highlighted
            ? "Baustein hervorgehoben."
            : "Hervorhebung entfernt.";
        await LoadSnippetsAsync();
    }

    [RelayCommand]
    private async Task AddTagAsync()
    {
        if (_runtime is null || SelectedSnippet is null)
        {
            return;
        }

        var value = EditorTagInput.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            StatusText = "Bitte zuerst einen Tag eingeben.";
            return;
        }

        var source = SelectedSnippet.Source;
        if (source.Tags.Any(t => string.Equals(t.Value, value, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText = "Tag bereits vorhanden.";
            return;
        }

        var tags = source.Tags.Append(new Tag(value)).ToList();
        await _runtime.SnippetWorkflowService.SaveAsync(
            source with
            {
                Tags = tags,
                UpdatedUtc = DateTimeOffset.UtcNow,
                LastEditor = Environment.UserName,
            },
            createVersion: true);

        EditorTagInput = string.Empty;
        StatusText = $"Tag '{value}' hinzugefuegt.";
        await LoadSnippetsAsync();
    }

    [RelayCommand]
    private async Task AddCommentAsync()
    {
        if (_runtime is null || SelectedSnippet is null)
        {
            return;
        }

        var text = EditorCommentInput.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            StatusText = "Bitte zuerst einen Kommentar eingeben.";
            return;
        }

        var source = SelectedSnippet.Source;
        var comments = source.Comments
            .Append(new Comment(Guid.NewGuid(), source.Id, Environment.UserName, text, DateTimeOffset.UtcNow))
            .ToList();

        await _runtime.SnippetWorkflowService.SaveAsync(
            source with
            {
                Comments = comments,
                UpdatedUtc = DateTimeOffset.UtcNow,
                LastEditor = Environment.UserName,
            },
            createVersion: true);

        EditorCommentInput = string.Empty;
        StatusText = "Kommentar gespeichert.";
        await LoadSnippetsAsync();
    }

    [RelayCommand]
    private Task NewTemplateFieldAsync()
    {
        _editingTemplateFieldKey = null;
        SelectedTemplateField = null;
        TemplateFieldKeyInput = string.Empty;
        TemplateFieldLabelInput = string.Empty;
        TemplateFieldTypeInput = FormFieldType.Text;
        TemplateFieldRequiredInput = true;
        TemplateFieldPlaceholderInput = string.Empty;
        TemplateFieldMinInput = string.Empty;
        TemplateFieldMaxInput = string.Empty;
        TemplateFieldDefaultValueInput = string.Empty;
        TemplateFieldOptionsInput = string.Empty;
        TemplateDesignerMode = "Neues Feld";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task SaveTemplateFieldAsync()
    {
        var key = TemplateFieldKeyInput.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            StatusText = "Template-Feld braucht einen Key.";
            return Task.CompletedTask;
        }

        if (!IsTemplateKeyValid(key))
        {
            StatusText = "Template-Key darf nur a-z, A-Z, 0-9, _, ., - enthalten.";
            return Task.CompletedTask;
        }

        var field = new TemplateFieldDesignerItemModel(
            key,
            TemplateFieldLabelInput.Trim(),
            TemplateFieldTypeInput,
            TemplateFieldRequiredInput,
            TemplateFieldPlaceholderInput.Trim(),
            TemplateFieldMinInput.Trim(),
            TemplateFieldMaxInput.Trim(),
            TemplateFieldDefaultValueInput.Trim(),
            TemplateFieldOptionsInput.Trim());

        var existingPair = TemplateFields
            .Select((item, index) => (item, index))
            .FirstOrDefault(x => string.Equals(x.item.Key, key, StringComparison.OrdinalIgnoreCase));
        var hasExisting = existingPair.item is not null;
        var existingIndex = existingPair.index;

        if (_editingTemplateFieldKey is not null)
        {
            var index = TemplateFields
                .Select((item, idx) => (item, idx))
                .FirstOrDefault(x => string.Equals(x.item.Key, _editingTemplateFieldKey, StringComparison.OrdinalIgnoreCase))
                .idx;
            if (index >= 0)
            {
                if (hasExisting && existingIndex != index)
                {
                    StatusText = $"Template-Feld '{key}' existiert bereits.";
                    return Task.CompletedTask;
                }

                TemplateFields[index] = field;
                SelectedTemplateField = field;
                _editingTemplateFieldKey = key;
                TemplateDesignerMode = "Feld bearbeiten";
                StatusText = $"Template-Feld '{key}' aktualisiert.";
                return Task.CompletedTask;
            }
        }

        if (hasExisting)
        {
            StatusText = $"Template-Feld '{key}' existiert bereits.";
            return Task.CompletedTask;
        }

        TemplateFields.Add(field);
        SelectedTemplateField = field;
        _editingTemplateFieldKey = key;
        TemplateDesignerMode = "Feld bearbeiten";
        StatusText = $"Template-Feld '{key}' hinzugefuegt.";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task DeleteSelectedTemplateFieldAsync()
    {
        if (SelectedTemplateField is null)
        {
            return Task.CompletedTask;
        }

        var removedKey = SelectedTemplateField.Key;
        TemplateFields.Remove(SelectedTemplateField);
        _ = NewTemplateFieldAsync();
        StatusText = $"Template-Feld '{removedKey}' entfernt.";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SaveTemplateDesignAsync()
    {
        if (_runtime is null || SelectedSnippet is null)
        {
            return;
        }

        var source = SelectedSnippet.Source;
        var bodyTemplate = string.IsNullOrWhiteSpace(EditorHtmlText) ? EditorPlainText : EditorHtmlText;
        var templateFields = TemplateFields.Select(x => x.ToTemplateField()).ToList();

        SnippetTemplate? template = null;
        if (templateFields.Count > 0)
        {
            template = new SnippetTemplate(bodyTemplate, templateFields);
            var errors = _runtime.FormSchemaValidator.Validate(template);
            if (errors.Count > 0)
            {
                StatusText = $"Template ungueltig: {string.Join(" | ", errors)}";
                return;
            }
        }

        var updated = source with
        {
            HtmlText = bodyTemplate,
            Template = template,
            UpdatedUtc = DateTimeOffset.UtcNow,
            LastEditor = Environment.UserName,
        };

        await _runtime.SnippetWorkflowService.SaveAsync(updated, createVersion: true);
        StatusText = template is null
            ? "Template-Designer geleert und gespeichert."
            : $"Template gespeichert ({template.Fields.Count} Felder).";
        await LoadSnippetsAsync();
    }

    [RelayCommand]
    private Task NewTriggerRuleAsync()
    {
        _editingTriggerRuleId = null;
        TriggerPatternInput = string.Empty;
        TriggerTypeInput = TriggerType.Autotext;
        TriggerScopeInput = TriggerScope.Any;
        TriggerCaseSensitiveInput = false;
        TriggerTargetProcessInput = string.Empty;
        TriggerEnabledInput = true;
        SelectedTriggerRule = null;
        TriggerEditorMode = "Neuer Trigger";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SaveTriggerRuleAsync()
    {
        if (_runtime is null || SelectedSnippet is null)
        {
            return;
        }

        var pattern = TriggerPatternInput.Trim();
        if (TriggerTypeInput == TriggerType.Hotkey)
        {
            pattern = HotkeyComboNormalizer.Normalize(pattern);
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            StatusText = "Bitte Trigger-Muster angeben.";
            return;
        }

        var source = SelectedSnippet.Source;
        var triggers = source.Triggers.ToList();
        var targetProcess = string.IsNullOrWhiteSpace(TriggerTargetProcessInput)
            ? null
            : TriggerTargetProcessInput.Trim();

        if (_editingTriggerRuleId is Guid existingId)
        {
            var index = triggers.FindIndex(x => x.Id == existingId);
            if (index >= 0)
            {
                triggers[index] = new TriggerRule(
                    existingId,
                    TriggerTypeInput,
                    pattern,
                    TriggerCaseSensitiveInput,
                    TriggerScopeInput,
                    targetProcess,
                    TriggerEnabledInput);
            }
            else
            {
                triggers.Add(new TriggerRule(
                    existingId,
                    TriggerTypeInput,
                    pattern,
                    TriggerCaseSensitiveInput,
                    TriggerScopeInput,
                    targetProcess,
                    TriggerEnabledInput));
            }
        }
        else
        {
            triggers.Add(new TriggerRule(
                Guid.NewGuid(),
                TriggerTypeInput,
                pattern,
                TriggerCaseSensitiveInput,
                TriggerScopeInput,
                targetProcess,
                TriggerEnabledInput));
        }

        await _runtime.SnippetWorkflowService.SaveAsync(
            source with
            {
                Triggers = triggers,
                UpdatedUtc = DateTimeOffset.UtcNow,
                LastEditor = Environment.UserName,
            },
            createVersion: true);

        StatusText = _editingTriggerRuleId is null
            ? "Trigger-Regel erstellt."
            : "Trigger-Regel aktualisiert.";

        await LoadSnippetsAsync();
    }

    [RelayCommand]
    private async Task DeleteSelectedTriggerRuleAsync()
    {
        if (_runtime is null || SelectedSnippet is null || SelectedTriggerRule is null)
        {
            return;
        }

        var source = SelectedSnippet.Source;
        var triggers = source.Triggers
            .Where(x => x.Id != SelectedTriggerRule.Id)
            .ToList();

        await _runtime.SnippetWorkflowService.SaveAsync(
            source with
            {
                Triggers = triggers,
                UpdatedUtc = DateTimeOffset.UtcNow,
                LastEditor = Environment.UserName,
            },
            createVersion: true);

        await NewTriggerRuleAsync();
        StatusText = "Trigger-Regel geloescht.";
        await LoadSnippetsAsync();
    }

    [RelayCommand]
    private async Task SearchReplaceVisibleAsync()
    {
        if (_runtime is null)
        {
            return;
        }

        var find = SearchTerm.Trim();
        if (string.IsNullOrWhiteSpace(find))
        {
            StatusText = "Bitte Suchbegriff fuer Ersetzen angeben.";
            return;
        }

        var replacement = ReplaceTerm;
        var tagScope = string.IsNullOrWhiteSpace(ReplaceScopeTag) ? null : ReplaceScopeTag.Trim();
        var targetProcessScope = string.IsNullOrWhiteSpace(ReplaceScopeTargetProcess) ? null : ReplaceScopeTargetProcess.Trim();
        IReadOnlyList<Guid>? selectedIds = null;
        if (ReplaceScopeUseSelection)
        {
            if (SelectedSnippet is null)
            {
                StatusText = "Bitte fuer Selection-Scope zuerst einen Baustein auswaehlen.";
                return;
            }

            selectedIds = [SelectedSnippet.Id];
        }

        var scope = new SnippetReplaceScope(
            ReplaceScopeUseFolder ? SelectedFolder?.Id : null,
            tagScope,
            targetProcessScope,
            selectedIds,
            ReplaceScopeIncludeHidden);
        var result = await _runtime.SnippetWorkflowService.ReplaceAsync(
            new SnippetReplaceRequest(find, replacement, scope),
            Environment.UserName);
        ReplaceResultText = $"Inspected: {result.Inspected} | Updated: {result.Updated} | Errors: {result.Errors.Count}";
        StatusText = result.Errors.Count == 0
            ? $"Suchen/Ersetzen abgeschlossen. Geprueft: {result.Inspected}, geaendert: {result.Updated}"
            : $"Suchen/Ersetzen: {result.Updated} geaendert, {result.Errors.Count} Fehler.";
        await LoadSnippetsAsync();
    }

    [RelayCommand]
    private async Task MoveToTrashAsync()
    {
        if (_runtime is null || SelectedSnippet is null)
        {
            return;
        }

        var entry = new TrashEntry(Guid.NewGuid(), SelectedSnippet.Source, DateTimeOffset.UtcNow, Environment.UserName);
        await _runtime.SnippetWorkflowService.MoveToTrashAsync(entry);
        StatusText = "Baustein in Papierkorb verschoben.";
        await LoadSnippetsAsync();
        await LoadTrashEntriesAsync();
    }

    [RelayCommand]
    private async Task RestoreLatestTrashAsync()
    {
        if (_runtime is null)
        {
            return;
        }

        var entries = await _runtime.TrashRepository.GetAllAsync();
        var target = SelectedTrashEntry?.Source ?? entries.OrderByDescending(x => x.DeletedUtc).FirstOrDefault();
        if (target is null)
        {
            StatusText = "Papierkorb ist leer.";
            return;
        }

        var snapshot = target.Snapshot with
        {
            Deleted = false,
            UpdatedUtc = DateTimeOffset.UtcNow,
            LastEditor = Environment.UserName,
        };

        await _runtime.SnippetWorkflowService.RestoreAsync(snapshot);
        await _runtime.TrashRepository.RemoveAsync(target.Id);
        StatusText = $"Baustein '{snapshot.Title}' wiederhergestellt.";
        await LoadSnippetsAsync();
        await LoadTrashEntriesAsync();
    }

    [RelayCommand]
    private async Task PurgeTrashAsync()
    {
        if (_runtime is null)
        {
            return;
        }

        var entries = await _runtime.TrashRepository.GetAllAsync();
        foreach (var entry in entries)
        {
            await _runtime.TrashRepository.RemoveAsync(entry.Id);
        }

        StatusText = $"Papierkorb geleert ({entries.Count} Eintraege).";
        await LoadTrashEntriesAsync();
    }

    [RelayCommand]
    private async Task SimulateInsertAsync()
    {
        if (_runtime is null)
        {
            return;
        }

        var results = await _runtime.InsertionPipeline.ExecuteAsync(
            new InsertionPayload(EditorPlainText, EditorHtmlText, []),
            new InsertionContext(null, false, true, null));

        var ok = results.All(r => r.Success);
        StatusText = ok ? "Einfuegen simuliert." : $"Einfuegen mit Fehlern ({results.Count(r => !r.Success)}).";
    }

    [RelayCommand]
    private async Task RollbackToPreviousVersionAsync()
    {
        if (_runtime is null || SelectedSnippet is null)
        {
            return;
        }

        var versions = await _runtime.VersionRepository.GetVersionsAsync(SelectedSnippet.Id);
        var selectedVersion = SelectedVersionEntry?.Source;
        var target = selectedVersion ?? versions
            .OrderByDescending(x => x.VersionNumber)
            .Skip(1)
            .FirstOrDefault();

        if (target is null)
        {
            StatusText = "Keine vorherige Version vorhanden.";
            return;
        }

        await _runtime.SnippetWorkflowService.RollbackToVersionAsync(
            SelectedSnippet.Id,
            target,
            Environment.UserName);
        StatusText = $"Rollback auf Version {target.VersionNumber} abgeschlossen.";
        await LoadSnippetsAsync();
        await LoadVersionsAsync();
    }

    [RelayCommand]
    private async Task LoadVersionsAsync()
    {
        if (_runtime is null || SelectedSnippet is null)
        {
            return;
        }

        var selectedSnippetId = SelectedSnippet.Id;
        var versions = await _runtime.VersionRepository.GetVersionsAsync(selectedSnippetId);
        if (SelectedSnippet?.Id != selectedSnippetId)
        {
            return;
        }

        VersionEntries.Clear();
        foreach (var version in versions.OrderByDescending(x => x.VersionNumber))
        {
            VersionEntries.Add(new VersionEntryItemModel(version));
        }

        SelectedVersionEntry = VersionEntries.FirstOrDefault();
        StatusText = $"Versionen gefunden: {versions.Count}";
    }

    [RelayCommand]
    private async Task RollbackToSelectedVersionAsync()
    {
        if (_runtime is null || SelectedSnippet is null || SelectedVersionEntry is null)
        {
            return;
        }

        await _runtime.SnippetWorkflowService.RollbackToVersionAsync(
            SelectedSnippet.Id,
            SelectedVersionEntry.Source,
            Environment.UserName);
        StatusText = $"Rollback auf Version {SelectedVersionEntry.VersionNumber} abgeschlossen.";
        await LoadSnippetsAsync();
        await LoadVersionsAsync();
    }

    [RelayCommand]
    private async Task LoadTrashEntriesAsync()
    {
        if (_runtime is null)
        {
            return;
        }

        var entries = await _runtime.TrashRepository.GetAllAsync();
        TrashEntries.Clear();
        foreach (var entry in entries.OrderByDescending(x => x.DeletedUtc))
        {
            TrashEntries.Add(new TrashEntryItemModel(entry));
        }

        SelectedTrashEntry = TrashEntries.FirstOrDefault();
    }

    [RelayCommand]
    private async Task DeleteSelectedTrashEntryAsync()
    {
        if (_runtime is null || SelectedTrashEntry is null)
        {
            return;
        }

        await _runtime.TrashRepository.RemoveAsync(SelectedTrashEntry.Id);
        StatusText = $"Trash-Eintrag '{SelectedTrashEntry.Title}' entfernt.";
        await LoadTrashEntriesAsync();
    }

    [RelayCommand]
    private async Task RemoveDuplicatesAsync()
    {
        if (_runtime is null)
        {
            return;
        }

        var removed = await _runtime.SnippetMaintenanceService.RemoveDuplicatesAsync();
        StatusText = $"Duplikate entfernt: {removed}";
        await LoadSnippetsAsync();
    }

    [RelayCommand]
    private async Task ApplyBulkFontAsync()
    {
        if (_runtime is null)
        {
            return;
        }

        var ids = VisibleSnippets.Select(x => x.Id);
        var updated = await _runtime.SnippetMaintenanceService.BulkSetFontAsync(ids, "Segoe UI");
        StatusText = $"Font fuer {updated} Bausteine aktualisiert.";
        await LoadSnippetsAsync();
    }

    [RelayCommand]
    private Task GenerateDocumentAsync()
    {
        if (_runtime is null)
        {
            return Task.CompletedTask;
        }

        var snippets = VisibleSnippets.Select(x => x.Source).ToList();
        if (snippets.Count == 0)
        {
            StatusText = "Keine Bausteine fuer Dokumentgenerator vorhanden.";
            return Task.CompletedTask;
        }

        var title = $"Texty Dokument {DateTimeOffset.Now:yyyy-MM-dd HH:mm}";
        DocumentPreviewText = _runtime.DocumentGeneratorService.GenerateDocument(snippets, title);
        StatusText = $"Dokumentgenerator: {snippets.Count} Bausteine zusammengefuehrt.";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task ApplyTextCorrectionsAsync()
    {
        if (_runtime is null)
        {
            return Task.CompletedTask;
        }

        var corrected = _runtime.TextCorrectionService.ApplyCorrections(EditorPlainText);
        EditorPlainText = corrected;
        EditorHtmlText = NormalizePlainTextToHtml(corrected);

        StatusText = "Rechtschreib-/Autokorrektur angewendet.";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task LoadClipboardHistoryAsync()
    {
        if (_runtime is null)
        {
            return Task.CompletedTask;
        }

        var history = _runtime.ClipboardHistoryService.GetAll();
        if (history.Count == 0)
        {
            ClipboardPreviewText = string.Empty;
            StatusText = "Clipboard-Historie ist leer.";
            return Task.CompletedTask;
        }

        var latest = history[0];
        ClipboardPreviewText = latest.PlainText ?? latest.HtmlText ?? string.Empty;
        StatusText = $"Clipboard-Historie geladen ({history.Count} Eintraege).";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task InsertLatestClipboardHistoryAsync()
    {
        if (_runtime is null)
        {
            return Task.CompletedTask;
        }

        var history = _runtime.ClipboardHistoryService.GetAll();
        if (history.Count == 0)
        {
            StatusText = "Kein Clipboard-Eintrag verfuegbar.";
            return Task.CompletedTask;
        }

        var latest = history[0];
        EditorPlainText = latest.PlainText ?? string.Empty;
        EditorHtmlText = !string.IsNullOrWhiteSpace(latest.HtmlText)
            ? latest.HtmlText
            : NormalizePlainTextToHtml(EditorPlainText);
        StatusText = "Letzten Clipboard-Eintrag in den Editor geladen.";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ExecuteImportAsync()
    {
        if (_runtime is null)
        {
            return;
        }

        var sourcePath = ImportSourcePath.Trim();
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            StatusText = "Bitte Import-Pfad angeben.";
            return;
        }

        var format = string.IsNullOrWhiteSpace(ImportFormat) ? "text" : ImportFormat.Trim().ToLowerInvariant();
        var result = await _runtime.ImportService.ImportAsync(sourcePath, format);
        ImportResultText = result.Warnings.Count == 0
            ? $"Created: {result.ImportedSnippets}"
            : $"Created: {result.ImportedSnippets} | Warnings: {string.Join(" | ", result.Warnings)}";
        StatusText = $"Import abgeschlossen ({result.ImportedSnippets} erstellt).";
        await LoadSnippetsAsync();
    }

    [RelayCommand]
    private async Task ResolveExternalValueAsync()
    {
        if (_runtime is null)
        {
            return;
        }

        var resolverName = ResolverName.Trim().ToLowerInvariant();
        var expression = ResolverExpression.Trim();
        if (string.IsNullOrWhiteSpace(resolverName) || string.IsNullOrWhiteSpace(expression))
        {
            StatusText = "Bitte Resolver und Ausdruck angeben.";
            return;
        }

        var resolver = _runtime.ExternalResolverFactory.GetResolver(resolverName);
        if (resolver is null)
        {
            StatusText = $"Resolver '{resolverName}' nicht verfuegbar.";
            return;
        }

        var value = await resolver.ResolveAsync(new ExternalValueRequest(expression));
        ResolverResultText = value ?? string.Empty;
        StatusText = value is null
            ? $"Resolver '{resolverName}' lieferte kein Ergebnis."
            : $"Resolver '{resolverName}' erfolgreich.";
    }

    [RelayCommand]
    private async Task GenerateWithAiAsync()
    {
        if (_runtime is null)
        {
            return;
        }

        var provider = ResolveSelectedAiProvider();
        if (provider is null)
        {
            StatusText = $"KI-Provider '{SelectedAiProvider}' nicht verfuegbar.";
            return;
        }

        var prompt = string.IsNullOrWhiteSpace(AiPromptInput)
            ? EditorPlainText
            : AiPromptInput.Trim();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            StatusText = "Bitte Prompt oder Editor-Inhalt fuer Generierung angeben.";
            return;
        }

        var response = await provider.GenerateAsync(
            new AiRequest(
                prompt,
                "Du erzeugst professionelle deutsche Textbausteine.",
                string.IsNullOrWhiteSpace(AiModelInput) ? null : AiModelInput.Trim(),
                0.3));

        AiResultText = response.Text;
        StatusText = $"KI-Generierung abgeschlossen ({response.Provider}/{response.Model}).";
    }

    [RelayCommand]
    private async Task RewriteWithAiAsync()
    {
        if (_runtime is null)
        {
            return;
        }

        var provider = ResolveSelectedAiProvider();
        if (provider is null)
        {
            StatusText = $"KI-Provider '{SelectedAiProvider}' nicht verfuegbar.";
            return;
        }

        var source = EditorPlainText.Trim();
        if (string.IsNullOrWhiteSpace(source))
        {
            StatusText = "Bitte zuerst Editor-Text fuer Umformulierung angeben.";
            return;
        }

        var rewriteInstruction = string.IsNullOrWhiteSpace(AiPromptInput)
            ? "Formuliere den folgenden Text klarer und professioneller um. Behalte die Bedeutung bei."
            : AiPromptInput.Trim();

        var response = await provider.GenerateAsync(
            new AiRequest(
                $"{rewriteInstruction}\n\nText:\n{source}",
                "Du bist ein Redaktionsassistent und lieferst nur den finalen Text.",
                string.IsNullOrWhiteSpace(AiModelInput) ? null : AiModelInput.Trim(),
                0.2));

        AiResultText = response.Text;
        StatusText = $"KI-Umformulierung abgeschlossen ({response.Provider}/{response.Model}).";
    }

    [RelayCommand]
    private async Task TranslateWithAiAsync()
    {
        if (_runtime is null)
        {
            return;
        }

        var provider = ResolveSelectedAiProvider();
        if (provider is null)
        {
            StatusText = $"KI-Provider '{SelectedAiProvider}' nicht verfuegbar.";
            return;
        }

        var sourceText = EditorPlainText.Trim();
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            StatusText = "Bitte zuerst Editor-Text fuer Uebersetzung angeben.";
            return;
        }

        var sourceLanguage = string.IsNullOrWhiteSpace(TranslationSourceLanguageInput) ? "de" : TranslationSourceLanguageInput.Trim();
        var targetLanguage = string.IsNullOrWhiteSpace(TranslationTargetLanguageInput) ? "en" : TranslationTargetLanguageInput.Trim();

        var response = await provider.GenerateAsync(
            new AiRequest(
                $"Uebersetze den folgenden Text von {sourceLanguage} nach {targetLanguage}. Gib nur die Uebersetzung zurueck.\n\n{sourceText}",
                "Du bist ein praeziser Uebersetzer.",
                string.IsNullOrWhiteSpace(AiModelInput) ? null : AiModelInput.Trim(),
                0.1));

        AiResultText = response.Text;
        StatusText = $"KI-Uebersetzung abgeschlossen ({response.Provider}/{response.Model}).";
    }

    [RelayCommand]
    private Task ApplyAiResultToEditorAsync()
    {
        if (string.IsNullOrWhiteSpace(AiResultText))
        {
            StatusText = "Kein KI-Ergebnis zum Uebernehmen vorhanden.";
            return Task.CompletedTask;
        }

        EditorPlainText = AiResultText.Trim();
        EditorHtmlText = NormalizePlainTextToHtml(EditorPlainText);
        StatusText = "KI-Ergebnis in den Editor uebernommen.";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task RefreshAiProviderHealthAsync()
    {
        if (_runtime is null)
        {
            return;
        }

        var health = await _runtime.AiProviderHealthService.CheckAllAsync();
        AiHealthReportText = string.Join(
            Environment.NewLine,
            health.Select(entry => $"{entry.Provider}: {(entry.Available ? "OK" : "FAIL")} - {entry.Message}"));
        StatusText = "KI-Health-Checks aktualisiert.";
    }

    [RelayCommand]
    private Task ExecuteMacroScriptAsync()
    {
        if (_runtime is null)
        {
            return Task.CompletedTask;
        }

        var script = MacroScriptInput.Trim();
        if (string.IsNullOrWhiteSpace(script))
        {
            StatusText = "Bitte Makro-Skript eingeben.";
            return Task.CompletedTask;
        }

        var variables = ParseMacroVariables(MacroVariablesInput);
        var policy = new MacroActionPolicy(
            MacroAllowProcessStart,
            MacroAllowFileSystemWrite,
            MacroAllowExternalOpen,
            MacroAllowNotifications,
            MacroAllowPowerShell);

        var context = new MacroExecutionContext(
            variables,
            new InsertionContext(
                SelectedSnippet?.Source.Triggers.FirstOrDefault(t => t.Enabled)?.TargetProcess,
                false,
                false,
                null),
            policy);

        var result = _runtime.MacroEngine.Execute(script, context);
        MacroOutputText = result.Output;
        MacroAuditText = result.AuditTrail.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, result.AuditTrail);
        MacroErrorsText = result.Errors is { Count: > 0 }
            ? string.Join(Environment.NewLine, result.Errors)
            : string.Empty;
        MacroVariablesInput = string.Join(Environment.NewLine, result.Variables.Select(v => $"{v.Key}={v.Value}"));

        StatusText = result.Success
            ? "Makro erfolgreich ausgefuehrt."
            : $"Makro mit Fehlern beendet ({result.Errors?.Count ?? 0}).";
        return Task.CompletedTask;
    }

    partial void OnSelectedFolderChanged(FolderItemModel? value)
    {
        if (value is not null)
        {
            FolderNameInput = value.Name;
            FolderColorHexInput = value.ColorHex ?? "#0EA5A5";
        }

        var cancellationToken = ResetLoadSnippetsCancellation();
        _ = LoadSnippetsSafelyAsync(cancellationToken);
    }

    partial void OnSelectedSnippetChanged(SnippetItemModel? value)
    {
        if (value is null)
        {
            EditorTitle = string.Empty;
            EditorPlainText = string.Empty;
            EditorHtmlText = string.Empty;
            TriggerRules.Clear();
            VersionEntries.Clear();
            TemplateFields.Clear();
            _editingTriggerRuleId = null;
            _editingTemplateFieldKey = null;
            SelectedTriggerRule = null;
            return;
        }

        EditorTitle = value.Source.Title;
        EditorPlainText = value.Source.PlainText;
        EditorHtmlText = value.Source.HtmlText;
        TriggerRules.Clear();
        foreach (var trigger in value.Source.Triggers.OrderByDescending(x => x.Enabled).ThenBy(x => x.Type.ToString()))
        {
            TriggerRules.Add(new TriggerRuleItemModel(
                trigger.Id,
                trigger.Type,
                trigger.Pattern,
                trigger.CaseSensitive,
                trigger.Scope,
                trigger.TargetProcess,
                trigger.Enabled));
        }

        SelectedTriggerRule = TriggerRules.FirstOrDefault();
        LoadTemplateDesigner(value.Source);
        _ = LoadVersionsAsync();
    }

    partial void OnSelectedTriggerRuleChanged(TriggerRuleItemModel? value)
    {
        if (value is null)
        {
            _editingTriggerRuleId = null;
            TriggerEditorMode = "Neuer Trigger";
            return;
        }

        _editingTriggerRuleId = value.Id;
        TriggerTypeInput = value.Type;
        TriggerPatternInput = value.Pattern;
        TriggerCaseSensitiveInput = value.CaseSensitive;
        TriggerScopeInput = value.Scope;
        TriggerTargetProcessInput = value.TargetProcess ?? string.Empty;
        TriggerEnabledInput = value.Enabled;
        TriggerEditorMode = "Trigger bearbeiten";
    }

    partial void OnSelectedTemplateFieldChanged(TemplateFieldDesignerItemModel? value)
    {
        if (value is null)
        {
            _editingTemplateFieldKey = null;
            TemplateDesignerMode = "Neues Feld";
            return;
        }

        _editingTemplateFieldKey = value.Key;
        TemplateFieldKeyInput = value.Key;
        TemplateFieldLabelInput = value.Label;
        TemplateFieldTypeInput = value.FieldType;
        TemplateFieldRequiredInput = value.Required;
        TemplateFieldPlaceholderInput = value.Placeholder;
        TemplateFieldMinInput = value.Min;
        TemplateFieldMaxInput = value.Max;
        TemplateFieldDefaultValueInput = value.DefaultValue;
        TemplateFieldOptionsInput = value.Options;
        TemplateDesignerMode = "Feld bearbeiten";
    }

    partial void OnMacroAllowProcessStartChanged(bool value) => UpdateMacroPolicySummary();

    partial void OnMacroAllowFileSystemWriteChanged(bool value) => UpdateMacroPolicySummary();

    partial void OnMacroAllowExternalOpenChanged(bool value) => UpdateMacroPolicySummary();

    partial void OnMacroAllowNotificationsChanged(bool value) => UpdateMacroPolicySummary();

    partial void OnMacroAllowPowerShellChanged(bool value) => UpdateMacroPolicySummary();

    private async Task LoadFoldersAsync()
    {
        if (_runtime is null)
        {
            return;
        }

        Folders.Clear();
        var folders = (await _runtime.FolderRepository.GetAllAsync())
            .OrderBy(folder => folder.SortOrder)
            .ThenBy(folder => folder.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var folder in folders)
        {
            var brush = new SolidColorBrush(ParseColor(folder.ColorHex));
            Folders.Add(new FolderItemModel(folder.Id, folder.Name, brush, folder.SortOrder, folder.ParentFolderId, folder.ColorHex));
        }

        SelectedFolder ??= Folders.FirstOrDefault();
    }

    private async Task LoadSnippetsAsync(CancellationToken cancellationToken = default)
    {
        if (_runtime is null)
        {
            return;
        }

        var requestId = Interlocked.Increment(ref _snippetLoadRequestCounter);
        Interlocked.Exchange(ref _latestSnippetLoadRequestId, requestId);
        var requestedQueryIdentity = BuildCurrentSnippetQueryIdentity();
        var tagScope = string.IsNullOrWhiteSpace(ReplaceScopeTag) ? null : ReplaceScopeTag.Trim();
        var targetScope = string.IsNullOrWhiteSpace(ReplaceScopeTargetProcess) ? null : ReplaceScopeTargetProcess.Trim();
        var query = new SnippetSearchQuery(
            string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm,
            SelectedFolder?.Id,
            tagScope,
            targetScope,
            500,
            false);
        var found = await _runtime.SnippetWorkflowService.SearchAsync(query, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (requestId != Volatile.Read(ref _latestSnippetLoadRequestId) ||
            !string.Equals(requestedQueryIdentity, BuildCurrentSnippetQueryIdentity(), StringComparison.Ordinal))
        {
            return;
        }

        var rebuilt = found
            .Select(item => new SnippetItemModel(
                item.Snippet.Id,
                item.Snippet.FolderId,
                item.Snippet.Title,
                item.Snippet.Shortcut,
                $"#{item.Snippet.Shortcut}",
                item.Snippet))
            .ToList();
        cancellationToken.ThrowIfCancellationRequested();

        if (requestId != Volatile.Read(ref _latestSnippetLoadRequestId) ||
            !string.Equals(requestedQueryIdentity, BuildCurrentSnippetQueryIdentity(), StringComparison.Ordinal))
        {
            return;
        }

        VisibleSnippets.Clear();
        foreach (var item in rebuilt)
        {
            VisibleSnippets.Add(item);
        }

        var selectedId = SelectedSnippet?.Id;
        if (selectedId is not null)
        {
            SelectedSnippet = VisibleSnippets.FirstOrDefault(x => x.Id == selectedId);
        }
        else if (VisibleSnippets.Count > 0)
        {
            SelectedSnippet = VisibleSnippets[0];
        }

        var stats = _runtime.ProductivityStatsService.Snapshot();
        StatusText = $"Bausteine: {VisibleSnippets.Count} | Insertions: {stats.Insertions} | Zeitersparnis: {stats.SecondsSaved}s";
    }

    private string BuildCurrentSnippetQueryIdentity()
    {
        return string.Join(
            "|",
            (SelectedFolder?.Id.ToString("N") ?? string.Empty).Trim(),
            (SearchTerm ?? string.Empty).Trim(),
            (ReplaceScopeTag ?? string.Empty).Trim(),
            (ReplaceScopeTargetProcess ?? string.Empty).Trim());
    }

    private static string NormalizePlainTextToHtml(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return "<p><br/></p>";
        }

        var encoded = System.Net.WebUtility.HtmlEncode(plainText)
            .Replace("\r\n", "<br/>", StringComparison.Ordinal)
            .Replace("\n", "<br/>", StringComparison.Ordinal);
        return $"<p>{encoded}</p>";
    }

    private async Task LoadSnippetsSafelyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await LoadSnippetsAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Intentionally ignored: a newer folder-selection request superseded this one.
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                StatusText = $"Fehler beim Laden der Bausteine: {ex.Message}";
            }
        }
    }

    private CancellationToken ResetLoadSnippetsCancellation()
    {
        var next = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _loadSnippetsCts, next);
        if (previous is not null)
        {
            try
            {
                previous.Cancel();
            }
            finally
            {
                previous.Dispose();
            }
        }

        return next.Token;
    }

    private void LoadTemplateDesigner(Snippet snippet)
    {
        TemplateFields.Clear();
        if (snippet.Template?.Fields is { Count: > 0 })
        {
            foreach (var field in snippet.Template.Fields)
            {
                TemplateFields.Add(TemplateFieldDesignerItemModel.FromTemplateField(field));
            }
        }

        _ = NewTemplateFieldAsync();
    }

    private async Task PersistFolderSortOrderAsync(IList<Folder> folders)
    {
        if (_runtime is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < folders.Count; i++)
        {
            var folder = folders[i] with
            {
                SortOrder = i,
                UpdatedUtc = now,
            };
            await _runtime.FolderRepository.SaveAsync(folder);
            folders[i] = folder;
        }
    }

    private async Task NormalizeFolderSortOrderAsync()
    {
        if (_runtime is null)
        {
            return;
        }

        var folders = (await _runtime.FolderRepository.GetAllAsync())
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        await PersistFolderSortOrderAsync(folders);
    }

    private IAiProvider? ResolveSelectedAiProvider()
    {
        if (_runtime is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(SelectedAiProvider))
        {
            var selected = _runtime.AiProviders.Get(SelectedAiProvider);
            if (selected is not null)
            {
                return selected;
            }
        }

        return _runtime.DefaultAiProvider;
    }

    private static Dictionary<string, string> ParseMacroVariables(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var trimmed = input.Trim();
        if (trimmed.StartsWith("{", StringComparison.Ordinal))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(trimmed);
                if (parsed is not null)
                {
                    return new Dictionary<string, string>(parsed, StringComparer.OrdinalIgnoreCase);
                }
            }
            catch
            {
                // Fall back to key=value parsing.
            }
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = trimmed.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var split = line.Split('=', 2, StringSplitOptions.TrimEntries);
            if (split.Length == 0 || string.IsNullOrWhiteSpace(split[0]))
            {
                continue;
            }

            result[split[0]] = split.Length > 1 ? split[1] : string.Empty;
        }

        return result;
    }

    private static bool IsTemplateKeyValid(string value)
    {
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch) || ch is '_' or '.' or '-')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static string NormalizeFolderColor(string? input)
    {
        var value = string.IsNullOrWhiteSpace(input) ? "0EA5A5" : input.Trim();
        if (value.StartsWith("#", StringComparison.Ordinal))
        {
            value = value[1..];
        }

        if (value.Length == 6 && int.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out _))
        {
            return $"#{value.ToUpperInvariant()}";
        }

        return "#0EA5A5";
    }

    private void UpdateMacroPolicySummary()
    {
        MacroPolicySummaryText =
            $"Policy Gates | Process: {(MacroAllowProcessStart ? "allow" : "deny")} | " +
            $"File: {(MacroAllowFileSystemWrite ? "allow" : "deny")} | " +
            $"External: {(MacroAllowExternalOpen ? "allow" : "deny")} | " +
            $"Notify: {(MacroAllowNotifications ? "allow" : "deny")} | " +
            $"PowerShell: {(MacroAllowPowerShell ? "allow" : "deny")}";
    }

    private static Windows.UI.Color ParseColor(string? colorHex)
    {
        if (string.IsNullOrWhiteSpace(colorHex))
        {
            return Windows.UI.Color.FromArgb(255, 14, 165, 165);
        }

        try
        {
            if (colorHex.StartsWith("#", StringComparison.Ordinal))
            {
                colorHex = colorHex[1..];
            }

            if (colorHex.Length == 6)
            {
                var r = Convert.ToByte(colorHex[0..2], 16);
                var g = Convert.ToByte(colorHex[2..4], 16);
                var b = Convert.ToByte(colorHex[4..6], 16);
                return Windows.UI.Color.FromArgb(255, r, g, b);
            }
        }
        catch
        {
            // Fall through to default.
        }

        return Windows.UI.Color.FromArgb(255, 14, 165, 165);
    }

    private static string BuildCopyShortcut(string shortcut)
    {
        if (string.IsNullOrWhiteSpace(shortcut))
        {
            return "txcopy";
        }

        var baseValue = shortcut.Trim();
        return baseValue.EndsWith("_copy", StringComparison.OrdinalIgnoreCase)
            ? $"{baseValue}_2"
            : $"{baseValue}_copy";
    }
}
