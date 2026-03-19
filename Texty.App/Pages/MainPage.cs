using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Texty.App.ViewModels;

namespace Texty.App.Pages;

public sealed class MainPage : Page
{
    private readonly MainViewModel _viewModel = new();
    private WebView2? _htmlPreview;
    private readonly TextBox _htmlFallbackPreview;
    private readonly Grid _previewHost;

    public MainPage()
    {
        _htmlFallbackPreview = new TextBox
        {
            Header = "HTML Vorschau (Fallback)",
            AcceptsReturn = true,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
        };
        ScrollViewer.SetVerticalScrollBarVisibility(_htmlFallbackPreview, ScrollBarVisibility.Auto);
        _previewHost = new Grid();

        DataContext = _viewModel;
        Content = BuildLayout();
        Loaded += OnLoaded;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    public MainViewModel ViewModel => _viewModel;

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

    private static Binding CreateBinding(string path, BindingMode mode = BindingMode.OneWay)
    {
        return new Binding
        {
            Path = new PropertyPath(path),
            Mode = mode,
        };
    }

    private FrameworkElement BuildPreviewControl()
    {
        try
        {
            _htmlPreview = new WebView2
            {
                DefaultBackgroundColor = Microsoft.UI.Colors.Transparent,
            };
            _previewHost.Children.Clear();
            _previewHost.Children.Add(_htmlPreview);
            return _previewHost;
        }
        catch (Exception ex)
        {
            ActivateFallbackPreview(
                $"WebView2 Fallback aktiv: {ex.GetType().Name}",
                BuildSafePreviewHtml(_viewModel.EditorPlainText, _viewModel.EditorHtmlText));
            return _previewHost;
        }
    }

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

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.EditorHtmlText) or nameof(MainViewModel.EditorPlainText))
        {
            RefreshPreview();
        }
    }

    private void RefreshPreview()
    {
        var html = BuildSafePreviewHtml(_viewModel.EditorPlainText, _viewModel.EditorHtmlText);

        if (_htmlPreview is not null)
        {
            try
            {
                _htmlPreview.NavigateToString(html);
                return;
            }
            catch
            {
                ActivateFallbackPreview("WebView2 Vorschau nicht verfuegbar, Fallback aktiv.", html);
            }
        }

        _htmlFallbackPreview.Text = html;
    }

    private static string BuildSafePreviewHtml(string plainText, string? htmlText)
    {
        var source = string.IsNullOrWhiteSpace(htmlText) ? plainText : htmlText;
        return $"<html><body><pre>{System.Net.WebUtility.HtmlEncode(source)}</pre></body></html>";
    }

    private void ActivateFallbackPreview(string status, string html)
    {
        _htmlPreview = null;
        _viewModel.StatusText = status;
        _htmlFallbackPreview.Text = html;
        _previewHost.Children.Clear();
        _previewHost.Children.Add(_htmlFallbackPreview);
    }
}
