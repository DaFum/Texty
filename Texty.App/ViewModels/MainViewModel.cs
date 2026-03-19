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
    private string _editorTitle = string.Empty;

    [ObservableProperty]
    private string _editorPlainText = string.Empty;

    [ObservableProperty]
    private string _editorHtmlText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Bereit";

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

        await _runtime.SnippetRepository.SaveAsync(snippet);
        StatusText = "Neuer Baustein angelegt.";
        await LoadSnippetsAsync();
    }

    [RelayCommand]
    private async Task SaveSnippetAsync()
    {
        if (_runtime is null || SelectedSnippet is null)
        {
            return;
        }

        var baseSnippet = SelectedSnippet.Source;
        var updated = baseSnippet with
        {
            Title = EditorTitle,
            PlainText = EditorPlainText,
            HtmlText = string.IsNullOrWhiteSpace(EditorHtmlText) ? EditorPlainText : EditorHtmlText,
            UpdatedUtc = DateTimeOffset.UtcNow,
            LastEditor = Environment.UserName,
        };

        await _runtime.SnippetRepository.SaveAsync(updated);
        await _runtime.VersionRepository.AddVersionAsync(
            new VersionEntry(
                Guid.NewGuid(),
                updated.Id,
                (await _runtime.VersionRepository.GetVersionsAsync(updated.Id)).Count + 1,
                updated.PlainText,
                updated.HtmlText,
                DateTimeOffset.UtcNow,
                Environment.UserName));

        StatusText = "Baustein gespeichert.";
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
        await _runtime.TrashRepository.MoveToTrashAsync(entry);
        await _runtime.SnippetRepository.DeleteAsync(SelectedSnippet.Id);
        StatusText = "Baustein in Papierkorb verschoben.";
        await LoadSnippetsAsync();
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
        _runtime.ProductivityStatsService.TrackInsertion();
        StatusText = ok ? "Einfuegen simuliert." : $"Einfuegen mit Fehlern ({results.Count(r => !r.Success)}).";
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
    }

    partial void OnSelectedFolderChanged(FolderItemModel? value)
    {
        _ = LoadSnippetsSafelyAsync();
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

    private async Task LoadSnippetsAsync()
    {
        if (_runtime is null)
        {
            return;
        }

        var query = new SnippetSearchQuery(
            string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm,
            SelectedFolder?.Id,
            null,
            null,
            500,
            false);
        var found = await _runtime.SnippetRepository.SearchAsync(query);

        VisibleSnippets.Clear();
        foreach (var item in found)
        {
            VisibleSnippets.Add(
                new SnippetItemModel(
                    item.Snippet.Id,
                    item.Snippet.FolderId,
                    item.Snippet.Title,
                    item.Snippet.Shortcut,
                    $"#{item.Snippet.Shortcut}",
                    item.Snippet));
        }

        if (VisibleSnippets.Count > 0 && (SelectedSnippet is null || VisibleSnippets.All(x => x.Id != SelectedSnippet.Id)))
        {
            SelectedSnippet = VisibleSnippets[0];
        }

        var stats = _runtime.ProductivityStatsService.Snapshot();
        StatusText = $"Bausteine: {VisibleSnippets.Count} | Insertions: {stats.Insertions}";
    }

    private async Task LoadSnippetsSafelyAsync()
    {
        try
        {
            await LoadSnippetsAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Fehler beim Laden der Bausteine: {ex.Message}";
        }
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
}
