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

    /// <summary>
    /// Initialisiert eine neue Instanz von MainViewModel und setzt den Anwendungs- bzw. Fenstertitel auf "Texty".
    /// </summary>
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

    /// <summary>
    /// Initialisiert die Laufzeitumgebung und lädt danach die Ordner- und Snippet-Daten in das ViewModel.
    /// </summary>
    public async Task InitializeAsync()
    {
        await AppRuntimeService.InitializeAsync();
        _runtime = AppRuntimeService.RuntimeContext;
        await LoadFoldersAsync();
        await LoadSnippetsAsync();
    }

    /// <summary>
    /// Lädt die Liste der sichtbaren Snippets basierend auf den aktuellen Such- und Filtereinstellungen.
    /// </summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadSnippetsAsync();
    }

    /// <summary>
    /// Erzeugt einen neuen Snippet-Eintrag im aktuell ausgewählten Ordner (oder im Standardordner) und speichert ihn im Repository.
    /// </summary>
    /// <remarks>
    /// Aktualisiert anschließend den Statustext und lädt die Snippet-Liste neu, damit der neue Eintrag in der UI sichtbar ist.
    /// </remarks>
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

    /// <summary>
    /// Speichert den aktuell ausgewählten Baustein mit den aktuellen Editorfeldern und legt eine neue Versionsaufnahme an.
    /// </summary>
    /// <remarks>
    /// Wenn kein Runtime-Kontext vorhanden ist oder kein Baustein ausgewählt ist, wird keine Aktion ausgeführt.
    /// Nach erfolgreichem Speichern wird der Statustext aktualisiert und die Snippet-Liste neu geladen.
    /// </remarks>
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

    /// <summary>
    /// Verschiebt das aktuell ausgewählte Snippet in den Papierkorb und entfernt es aus der Sammlung.
    /// </summary>
    /// <remarks>
    /// Macht nichts, wenn kein Laufzeitkontext verfügbar ist oder kein Snippet ausgewählt ist.
    /// Aktualisiert danach den Statustext und lädt die Snippet-Liste neu.
    /// </remarks>
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

    /// <summary>
    /// Simuliert das Einfügen des aktuellen Editorinhalts über die InsertionPipeline, erhöht die Einfüge-Statistik und aktualisiert den Statustext entsprechend dem Ergebnis.
    /// </summary>
    /// <remarks>
    /// Setzt <see cref="StatusText"/> auf "Einfuegen simuliert." wenn alle Pipeline-Schritte erfolgreich waren; andernfalls auf "Einfuegen mit Fehlern (N)." wobei N die Anzahl fehlgeschlagener Schritte ist.
    /// </remarks>
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

    /// <summary>
    /// Lädt die Versionshistorie des aktuell ausgewählten Snippets und aktualisiert den Statustext mit der Anzahl gefundener Versionen.
    /// </summary>
    /// <remarks>
    /// Keine Aktion, wenn keine Runtime vorhanden ist oder kein Snippet ausgewählt wurde.
    /// </remarks>
    /// <returns>Ein Task, der abgeschlossen wird, nachdem die Versionen geladen und der Status aktualisiert wurden.</returns>
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

    /// <summary>
    /// Entfernt doppelte Snippets aus dem Repository und aktualisiert danach die sichtbare Snippet-Liste.
    /// </summary>
    /// <returns>Eine Task, die abgeschlossen ist, wenn die Duplikatentfernung und das Nachladen der Snippets beendet sind.</returns>
    /// <remarks>Wenn der Laufzeitkontext noch nicht initialisiert ist, wird nichts unternommen.</remarks>
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

    /// <summary>
    /// Wendet die Schriftart "Segoe UI" auf alle derzeit in VisibleSnippets sichtbaren Snippets an.
    /// </summary>
    /// <remarks>
    /// Aktualisiert die Eigenschaft <see cref="StatusText"/> mit der Anzahl der aktualisierten Bausteine.
    /// </remarks>
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

    /// <summary>
    /// Reagiert auf eine Änderung der ausgewählten Ordnerauswahl und lädt die Snippets für die neue Auswahl nach.
    /// </summary>
    /// <param name="value">Das neu ausgewählte Ordner-Element, oder `null`, wenn keine Auswahl vorhanden ist.</param>
    partial void OnSelectedFolderChanged(FolderItemModel? value)
    {
        _ = LoadSnippetsAsync();
    }

    /// <summary>
    /// Aktualisiert die Editor-Felder (Titel, Klartext, HTML) entsprechend der neu ausgewählten Snippet-Eintragung.
    /// </summary>
    /// <param name="value">Das neu ausgewählte Snippet-Modell oder <c>null</c>, um die Editor-Felder zu leeren.</param>
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

    /// <summary>
    /// Lädt alle Ordner aus dem aktuellen Runtime-Repository in die ObservableCollection <see cref="Folders"/>.
    /// </summary>
    /// <remarks>
    /// Wenn kein Runtime-Kontext vorhanden ist, bleibt die Methode ohne Wirkung.
    /// Die vorhandene <see cref="Folders"/>-Sammlung wird geleert und neu befüllt; anschließend wird <see cref="SelectedFolder"/> gesetzt, falls noch kein Eintrag ausgewählt ist.
    /// </remarks>
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

    /// <summary>
    /// Lädt Schnipsel basierend auf dem aktuellen Suchbegriff und dem gewählten Ordner und aktualisiert die UI-Collections.
    /// </summary>
    /// <remarks>
    /// Aktualisiert die ObservableCollection <c>VisibleSnippets</c>, setzt gegebenenfalls <c>SelectedSnippet</c> auf das erste gefundene Element
    /// und schreibt eine Statuszeile mit Anzahl der Schnipsel und Insertions-Statistik in <c>StatusText</c>.
    /// Wenn der Laufzeitkontext (_runtime) null ist, werden keine Änderungen vorgenommen.
    /// </remarks>
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

    /// <summary>
    /// Konvertiert eine hexadezimale RGB-Farbzeichenfolge in eine Windows.UI.Color-Struktur.
    /// </summary>
    /// <param name="colorHex">Hexadezimale RGB-Farbzeichenfolge im Format RRGGBB, optional mit führendem '#'. Null oder leere Werte sind erlaubt.</param>
    /// <returns>Die geparste Farbe mit voller Deckkraft (Alpha = 255). Bei null, leerem oder nicht parsebarem Input wird die Standardfarbe #0EA5A5 (RGB 14,165,165) zurückgegeben.</returns>
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
