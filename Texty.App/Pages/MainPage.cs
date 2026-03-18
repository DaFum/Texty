using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Texty.App.ViewModels;

namespace Texty.App.Pages;

public sealed class MainPage : Page
{
    private readonly MainViewModel _viewModel = new();
    private WebView2? _htmlPreview;
    private TextBox? _htmlFallbackPreview;

    /// <summary>
    /// Initialisiert die MainPage: setzt das DataContext auf das ViewModel, erstellt das UI-Layout und registriert die benötigten Event-Handler.
    /// </summary>
    public MainPage()
    {
        DataContext = _viewModel;
        Content = BuildLayout();
        Loaded += OnLoaded;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    public MainViewModel ViewModel => _viewModel;

    /// <summary>
    /// Erstellt das dreispaltige Hauptlayout der Seite und fügt die drei Bereichs-Paneele hinzu.
    /// </summary>
    /// <returns>Ein Grid, das als Root-UIElement dient und die linke, mittlere und rechte Spalte mit ihren Inhalten enthält.</returns>
    private UIElement BuildLayout()
    {
        var root = new Grid
        {
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 242, 247, 247)),
        };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(420) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        root.Children.Add(BuildLeftPane());
        root.Children.Add(BuildCenterPane());
        root.Children.Add(BuildRightPane());

        return root;
    }

    /// <summary>
    /// Erstellt das linke Bedienfeld der Seite mit Ordnerliste, Erstell-Button und Statusanzeige.
    /// </summary>
    /// <returns>Ein UIElement (Border), das ein gestapeltes Panel enthält: einen Titel, eine an `Folders` gebundene ListView (SelectedItem an `SelectedFolder`), einen Button gebunden an `CreateSnippetCommand` und ein TextBlock gebunden an `StatusText`.</returns>
    private UIElement BuildLeftPane()
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 18, 32, 41)),
            Padding = new Thickness(16),
        };
        Grid.SetColumn(border, 0);

        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(new TextBlock
        {
            Text = "Texty Explorer",
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 231, 242, 242)),
        });

        var folderList = new ListView
        {
            Height = 480,
            DisplayMemberPath = "Name",
        };
        folderList.SetBinding(ItemsControl.ItemsSourceProperty, CreateBinding("Folders"));
        folderList.SetBinding(ListView.SelectedItemProperty, CreateBinding("SelectedFolder", BindingMode.TwoWay));
        panel.Children.Add(folderList);

        var createButton = new Button
        {
            Content = "Neuer Baustein",
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 14, 165, 165)),
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 8, 14, 8),
        };
        createButton.SetBinding(Button.CommandProperty, CreateBinding("CreateSnippetCommand"));
        panel.Children.Add(createButton);

        var status = new TextBlock
        {
            TextWrapping = TextWrapping.WrapWholeWords,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 157, 180, 180)),
        };
        status.SetBinding(TextBlock.TextProperty, CreateBinding("StatusText"));
        panel.Children.Add(status);

        border.Child = panel;
        return border;
    }

    /// <summary>
    /// Erstellt die mittlere Spalte der Seite mit Befehlsbuttons, einer Suchbox und einer Liste der sichtbaren Snippets.
    /// </summary>
    /// <returns>Ein UIElement (StackPanel), das als zentraler Inhaltsbereich mit gebundenen Steuerelementen für Suche und Snippet-Auswahl dient.</returns>
    private UIElement BuildCenterPane()
    {
        var panel = new StackPanel
        {
            Padding = new Thickness(16),
            Spacing = 10,
        };
        Grid.SetColumn(panel, 1);

        var commands = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
        };
        commands.Children.Add(MakeCommandButton("Suchen", "SearchCommand"));
        commands.Children.Add(MakeCommandButton("Duplikate", "RemoveDuplicatesCommand"));
        commands.Children.Add(MakeCommandButton("Bulk Font", "ApplyBulkFontCommand"));
        panel.Children.Add(commands);

        var search = new AutoSuggestBox
        {
            PlaceholderText = "Suchen...",
            Width = 220,
        };
        search.SetBinding(AutoSuggestBox.TextProperty, CreateBinding("SearchTerm", BindingMode.TwoWay));
        panel.Children.Add(search);

        var snippetList = new ListView
        {
            DisplayMemberPath = "Title",
            Height = 620,
        };
        snippetList.SetBinding(ItemsControl.ItemsSourceProperty, CreateBinding("VisibleSnippets"));
        snippetList.SetBinding(ListView.SelectedItemProperty, CreateBinding("SelectedSnippet", BindingMode.TwoWay));
        panel.Children.Add(snippetList);

        return panel;
    }

    /// <summary>
    /// Erstellt das rechte Bedienfeld der Seite als Grid mit Editor-, Vorschau- und Steuerungselementen.
    /// </summary>
    /// <returns>Ein UIElement (Grid) das enthält: eine Befehlsleiste mit Schaltflächen, ein Titel-TextBox-Feld, ein mehrzeiliges Plain-Text-TextBox-Feld, die HTML-Vorschau und eine Statusanzeige.</returns>
    private UIElement BuildRightPane()
    {
        var grid = new Grid
        {
            Padding = new Thickness(16),
            RowSpacing = 8,
        };
        Grid.SetColumn(grid, 2);
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(220) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var commandRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
        };
        commandRow.Children.Add(MakeCommandButton("Speichern", "SaveSnippetCommand"));
        commandRow.Children.Add(MakeCommandButton("Einfuegen", "SimulateInsertCommand"));
        commandRow.Children.Add(MakeCommandButton("Versionen", "LoadVersionsCommand"));
        commandRow.Children.Add(MakeCommandButton("Papierkorb", "MoveToTrashCommand"));
        Grid.SetRow(commandRow, 0);
        grid.Children.Add(commandRow);

        var titleBox = new TextBox { Header = "Titel" };
        titleBox.SetBinding(TextBox.TextProperty, CreateBinding("EditorTitle", BindingMode.TwoWay));
        Grid.SetRow(titleBox, 1);
        grid.Children.Add(titleBox);

        var plain = new TextBox
        {
            Header = "Text (plain)",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
        };
        ScrollViewer.SetVerticalScrollBarVisibility(plain, ScrollBarVisibility.Auto);
        plain.SetBinding(TextBox.TextProperty, CreateBinding("EditorPlainText", BindingMode.TwoWay));
        Grid.SetRow(plain, 2);
        grid.Children.Add(plain);

        var preview = BuildPreviewControl();
        Grid.SetRow(preview, 3);
        grid.Children.Add(preview);

        var status = new TextBlock();
        status.SetBinding(TextBlock.TextProperty, CreateBinding("StatusText"));
        Grid.SetRow(status, 4);
        grid.Children.Add(status);

        return grid;
    }

    /// <summary>
    /// Erstellt einen Button mit beschriftetem Inhalt und bindet dessen Command-Eigenschaft an den angegebenen Binding-Pfad.
    /// </summary>
    /// <param name="text">Der angezeigte Text des Buttons.</param>
    /// <param name="commandPath">Der Binding-Pfad zur Command-Eigenschaft, z. B. "SaveCommand".</param>
    /// <returns>Ein Button-Element, dessen Command-Eigenschaft an den angegebenen Pfad gebunden ist.</returns>
    private static Button MakeCommandButton(string text, string commandPath)
    {
        var button = new Button
        {
            Content = text,
            Padding = new Thickness(10, 6, 10, 6),
        };
        button.SetBinding(Button.CommandProperty, CreateBinding(commandPath));
        return button;
    }

    /// <summary>
    /// Erstellt ein Binding mit dem angegebenen Eigenschaftspfad und BindingMode.
    /// </summary>
    /// <param name="path">Der Eigenschaftspfad, auf den die Bindung zeigt (z. B. "SelectedItem").</param>
    /// <param name="mode">Der BindingMode für die Bindung.</param>
    /// <returns>Ein Binding mit gesetztem Path und Mode.</returns>
    private static Binding CreateBinding(string path, BindingMode mode = BindingMode.OneWay)
    {
        return new Binding
        {
            Path = new PropertyPath(path),
            Mode = mode,
        };
    }

    /// <summary>
    /// Erstellt die HTML-Vorschau-Komponente für die Seite und wählt bei Fehlern eine TextBox-Fallbackanzeige.
    /// </summary>
    /// <returns>Ein FrameworkElement zum Anzeigen von HTML: ein initialisiertes `WebView2` wenn verfügbar, sonst eine schreibgeschützte `TextBox` als Fallback.</returns>
    private FrameworkElement BuildPreviewControl()
    {
        try
        {
            _htmlPreview = new WebView2
            {
                DefaultBackgroundColor = Microsoft.UI.Colors.Transparent,
            };
            return _htmlPreview;
        }
        catch (Exception ex)
        {
            _viewModel.StatusText = $"WebView2 Fallback aktiv: {ex.GetType().Name}";
            _htmlFallbackPreview = new TextBox
            {
                Header = "HTML Vorschau (Fallback)",
                AcceptsReturn = true,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
            };
            ScrollViewer.SetVerticalScrollBarVisibility(_htmlFallbackPreview, ScrollBarVisibility.Auto);
            return _htmlFallbackPreview;
        }
    }

    /// <summary>
    /// Behandelt das Loaded-Ereignis der Seite: initialisiert das ViewModel und aktualisiert die HTML-Vorschau; bei einem Fehler wird der Statustext des ViewModels gesetzt.
    /// </summary>
    /// <param name="sender">Der Auslöser des Ereignisses.</param>
    /// <param name="e">Ereignisargumente für das Loaded-Ereignis.</param>
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.InitializeAsync();
            RefreshPreview();
        }
        catch (Exception ex)
        {
            _viewModel.StatusText = $"Initialisierung fehlgeschlagen: {ex.Message}";
        }
    }

    /// <summary>
    /// Reagiert auf PropertyChanged-Ereignisse des ViewModels und aktualisiert die HTML-Vorschau, wenn sich EditorHtmlText oder EditorPlainText geändert hat.
    /// </summary>
    /// <param name="sender">Quelle des Ereignisses (typischerweise das ViewModel).</param>
    /// <param name="e">Enthält den Namen der geänderten Eigenschaft.</param>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.EditorHtmlText) or nameof(MainViewModel.EditorPlainText))
        {
            RefreshPreview();
        }
    }

    /// <summary>
    /// Aktualisiert die HTML-Vorschau aus den Editor-Feldern des ViewModels.
    /// </summary>
    /// <remarks>
    /// Verwendet `EditorHtmlText` wenn vorhanden; sonst wird ein einfaches HTML-Dokument mit HTML-kodiertem `EditorPlainText` erzeugt.
    /// Versucht, den Inhalt in der WebView2-Vorschau darzustellen; falls WebView2 nicht verfügbar ist, aktualisiert es `ViewModel.StatusText` und schreibt die HTML-Zeichenkette in die Fallback-TextBox, sofern diese vorhanden ist.
    /// </remarks>
    private void RefreshPreview()
    {
        var html = string.IsNullOrWhiteSpace(_viewModel.EditorHtmlText)
            ? $"<html><body><pre>{System.Net.WebUtility.HtmlEncode(_viewModel.EditorPlainText)}</pre></body></html>"
            : _viewModel.EditorHtmlText;

        if (_htmlPreview is not null)
        {
            try
            {
                _htmlPreview.NavigateToString(html);
                return;
            }
            catch
            {
                _htmlPreview = null;
                _viewModel.StatusText = "WebView2 Vorschau nicht verfuegbar, Fallback aktiv.";
            }
        }

        if (_htmlFallbackPreview is not null)
        {
            _htmlFallbackPreview.Text = html;
        }
    }
}
