using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Media;
using Texty.App.Services;
using Texty.App.ViewModels.Models;
using Texty.Core.Models;
using Texty.Runtime.Bootstrap;

namespace Texty.App.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private TextyRuntimeContext? _runtime;
    private CancellationTokenSource? _loadSnippetsCts;

    public MainViewModel()
    {
        Title = "Texty";
    }

    public ObservableCollection<FolderItemModel> Folders { get; } = [];

    public ObservableCollection<SnippetItemModel> VisibleSnippets { get; } = [];

    [ObservableProperty]
    private FolderItemModel? _selectedFolder;

    [ObservableProperty]
    private SnippetItemModel? _selectedSnippet;

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    private string _replaceTerm = string.Empty;

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
    private string _statusText = "Bereit";

    [ObservableProperty]
    private string _documentPreviewText = string.Empty;

    [ObservableProperty]
    private string _clipboardPreviewText = string.Empty;

    public async Task InitializeAsync()
    {
        await AppRuntimeService.InitializeAsync();
        _runtime = AppRuntimeService.RuntimeContext;
        await LoadFoldersAsync();
        await LoadSnippetsAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadSnippetsAsync();
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
        var scope = new SnippetReplaceScope(
            SelectedFolder?.Id,
            null,
            null,
            VisibleSnippets.Select(x => x.Id).ToList(),
            IncludeHidden: false);
        var result = await _runtime.SnippetWorkflowService.ReplaceAsync(
            new SnippetReplaceRequest(find, replacement, scope),
            Environment.UserName);
        StatusText = result.Errors.Count == 0
            ? $"Suchen/Ersetzen abgeschlossen. Geaendert: {result.Updated}"
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
    }

    [RelayCommand]
    private async Task RestoreLatestTrashAsync()
    {
        if (_runtime is null)
        {
            return;
        }

        var entries = await _runtime.TrashRepository.GetAllAsync();
        var latest = entries.OrderByDescending(x => x.DeletedUtc).FirstOrDefault();
        if (latest is null)
        {
            StatusText = "Papierkorb ist leer.";
            return;
        }

        var snapshot = latest.Snapshot with
        {
            Deleted = false,
            UpdatedUtc = DateTimeOffset.UtcNow,
            LastEditor = Environment.UserName,
        };

        await _runtime.SnippetWorkflowService.RestoreAsync(snapshot);
        await _runtime.TrashRepository.RemoveAsync(latest.Id);
        StatusText = $"Baustein '{snapshot.Title}' wiederhergestellt.";
        await LoadSnippetsAsync();
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
        var target = versions
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
    }

    [RelayCommand]
    private async Task LoadVersionsAsync()
    {
        if (_runtime is null || SelectedSnippet is null)
        {
            return;
        }

        var versions = await _runtime.VersionRepository.GetVersionsAsync(SelectedSnippet.Id);
        StatusText = $"Versionen gefunden: {versions.Count}";
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
        if (string.IsNullOrWhiteSpace(EditorHtmlText))
        {
            EditorHtmlText = corrected;
        }

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
        EditorHtmlText = latest.HtmlText ?? latest.PlainText ?? string.Empty;
        StatusText = "Letzten Clipboard-Eintrag in den Editor geladen.";
        return Task.CompletedTask;
    }

    partial void OnSelectedFolderChanged(FolderItemModel? value)
    {
        _ = value;
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
            return;
        }

        EditorTitle = value.Source.Title;
        EditorPlainText = value.Source.PlainText;
        EditorHtmlText = value.Source.HtmlText;
    }

    private async Task LoadFoldersAsync()
    {
        if (_runtime is null)
        {
            return;
        }

        Folders.Clear();
        var folders = await _runtime.FolderRepository.GetAllAsync();
        foreach (var folder in folders)
        {
            var brush = new SolidColorBrush(ParseColor(folder.ColorHex));
            Folders.Add(new FolderItemModel(folder.Id, folder.Name, brush));
        }

        SelectedFolder ??= Folders.FirstOrDefault();
    }

    private async Task LoadSnippetsAsync(CancellationToken cancellationToken = default)
    {
        if (_runtime is null)
        {
            return;
        }

        var requestedFolderId = SelectedFolder?.Id;
        var query = new SnippetSearchQuery(
            string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm,
            SelectedFolder?.Id,
            null,
            null,
            500,
            false);
        var found = await _runtime.SnippetWorkflowService.SearchAsync(query, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (requestedFolderId != SelectedFolder?.Id)
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
