using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Texty.App.ViewModels;
using Texty.Core.Models;

namespace Texty.App.Pages;

public sealed partial class MainPage : Page
{
    private static readonly Regex PlaceholderRegex = new(@"\{\{\s*(?<key>[a-zA-Z0-9_.-]+)\s*\}\}", RegexOptions.Compiled);

    private readonly MainViewModel _viewModel = new();

    private readonly Grid _mainGrid;
    private readonly Border _headerCard;
    private readonly Border _leftPaneCard;
    private readonly Border _centerPaneCard;
    private readonly Grid _rightColumnGrid;
    private readonly Border _editorCard;
    private readonly Border _triggerCard;

    private readonly TextBlock _shellStatusText;
    private readonly TextBlock _triggerSummaryText;
    private readonly TextBlock _triggerTargetText;
    private readonly StackPanel _triggerItemsPanel;

    private Grid? _headerGrid;
    private StackPanel? _headerRightPanel;
    private ListView? _folderList;
    private ListView? _snippetList;
    private Grid? _editorSplitGrid;
    private TextBox? _documentPreview;
    private TextBox? _clipboardPreview;

    private readonly Grid _previewHost;
    private readonly TextBox _htmlFallbackPreview;
    private readonly TextBox _placeholderTokenInput;
    private readonly NumberBox _tableRowsInput;
    private readonly NumberBox _tableColumnsInput;
    private readonly TextBox _imagePathInput;
    private WebView2? _htmlPreview;
    private bool _isWebViewInitialized;
    private bool _isEditorDomReady;
    private bool _isUpdatingFromEditor;

    private bool _hasRunRevealAnimation;

    public MainPage()
    {
        _mainGrid = new Grid { Margin = new Thickness(24, 18, 24, 24), ColumnSpacing = 18, RowSpacing = 18 };

        _headerCard = new Border
        {
            Margin = new Thickness(24, 24, 24, 0),
            Padding = new Thickness(24, 20, 24, 18),
            CornerRadius = new CornerRadius(24),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(210, 249, 246, 239)),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(88, 25, 63, 76)),
            BorderThickness = new Thickness(1),
        };

        _leftPaneCard = CreateCard(
            Microsoft.UI.ColorHelper.FromArgb(237, 15, 31, 44),
            Microsoft.UI.ColorHelper.FromArgb(96, 64, 132, 149));
        _centerPaneCard = CreateCard(
            Microsoft.UI.ColorHelper.FromArgb(228, 252, 248, 242),
            Microsoft.UI.ColorHelper.FromArgb(78, 30, 75, 88));

        _rightColumnGrid = new Grid { RowSpacing = 16 };
        _rightColumnGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _rightColumnGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(280) });

        _editorCard = CreateCard(
            Microsoft.UI.ColorHelper.FromArgb(228, 247, 251, 255),
            Microsoft.UI.ColorHelper.FromArgb(72, 35, 87, 102));
        _triggerCard = CreateCard(
            Microsoft.UI.ColorHelper.FromArgb(234, 24, 42, 56),
            Microsoft.UI.ColorHelper.FromArgb(92, 109, 206, 202));

        _shellStatusText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 33, 69, 80)),
            FontFamily = new FontFamily("Corbel"),
            FontSize = 13,
            HorizontalAlignment = HorizontalAlignment.Right,
            MaxWidth = 420,
        };

        _triggerSummaryText = new TextBlock
        {
            FontFamily = new FontFamily("Constantia"),
            FontSize = 17,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 237, 247, 249)),
        };

        _triggerTargetText = new TextBlock
        {
            FontFamily = new FontFamily("Corbel"),
            FontSize = 13,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 191, 223, 229)),
        };

        _triggerItemsPanel = new StackPanel { Spacing = 8 };

        _htmlFallbackPreview = new TextBox
        {
            Header = "HTML Editor (Fallback)",
            AcceptsReturn = true,
            IsReadOnly = false,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 160,
            FontFamily = new FontFamily("Consolas"),
        };
        _htmlFallbackPreview.TextChanged += (_, _) =>
        {
            if (_isUpdatingFromEditor)
            {
                return;
            }

            _isUpdatingFromEditor = true;
            _viewModel.EditorHtmlText = _htmlFallbackPreview.Text;
            _isUpdatingFromEditor = false;
        };
        ScrollViewer.SetVerticalScrollBarVisibility(_htmlFallbackPreview, ScrollBarVisibility.Auto);

        _placeholderTokenInput = new TextBox
        {
            PlaceholderText = "placeholder_key",
            MinWidth = 150,
            FontFamily = new FontFamily("Consolas"),
            CornerRadius = new CornerRadius(8),
        };
        _tableRowsInput = new NumberBox
        {
            Value = 2,
            SmallChange = 1,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
            MinWidth = 80,
            Width = 90,
        };
        _tableColumnsInput = new NumberBox
        {
            Value = 2,
            SmallChange = 1,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
            MinWidth = 80,
            Width = 90,
        };
        _imagePathInput = new TextBox
        {
            PlaceholderText = "Bildpfad (png/jpg/gif/webp/bmp)",
            MinWidth = 260,
            FontFamily = new FontFamily("Corbel"),
            CornerRadius = new CornerRadius(8),
        };

        _previewHost = new Grid { MinHeight = 160 };

        DataContext = _viewModel;
        Content = BuildLayout();

        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    public MainViewModel ViewModel => _viewModel;

    private UIElement BuildLayout()
    {
        var root = new Grid
        {
            Background = CreateBackdropBrush(),
        };

        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var atmosphere = BuildAtmosphereLayer();
        Grid.SetRowSpan(atmosphere, 2);
        root.Children.Add(atmosphere);

        _headerCard.Child = BuildHeaderContent();
        Grid.SetRow(_headerCard, 0);
        root.Children.Add(_headerCard);

        _leftPaneCard.Child = BuildLeftPane();
        _centerPaneCard.Child = BuildCenterPane();
        _editorCard.Child = BuildEditorPane();
        _triggerCard.Child = BuildTriggerPane();

        _rightColumnGrid.Children.Add(_editorCard);
        _rightColumnGrid.Children.Add(_triggerCard);
        Grid.SetRow(_editorCard, 0);
        Grid.SetRow(_triggerCard, 1);

        _mainGrid.Children.Add(_leftPaneCard);
        _mainGrid.Children.Add(_centerPaneCard);
        _mainGrid.Children.Add(_rightColumnGrid);

        Grid.SetRow(_mainGrid, 1);
        root.Children.Add(_mainGrid);

        ApplyResponsiveLayout(1440);
        RefreshTriggerPanel();

        return new ScrollViewer
        {
            Content = root,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollMode = ScrollMode.Auto,
            HorizontalScrollMode = ScrollMode.Disabled,
        };
    }

    private UIElement BuildHeaderContent()
    {
        _headerGrid = new Grid
        {
            ColumnSpacing = 24,
            RowSpacing = 8,
        };
        _headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _headerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _headerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0) });

        var left = new StackPanel { Spacing = 2 };
        left.Children.Add(new TextBlock
        {
            Text = "Texty Control Deck",
            FontFamily = new FontFamily("Constantia"),
            FontSize = 36,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 17, 42, 52)),
            CharacterSpacing = 22,
        });
        left.Children.Add(new TextBlock
        {
            Text = "Snippet-Orchestrierung fuer Explorer, Editor und Trigger in einem Arbeitsfluss.",
            FontFamily = new FontFamily("Corbel"),
            FontSize = 14,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 53, 87, 98)),
        });
        _headerGrid.Children.Add(left);

        _headerRightPanel = new StackPanel
        {
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        _headerRightPanel.Children.Add(CreateBadge("GLOBAL HOOK AKTIV", Microsoft.UI.ColorHelper.FromArgb(255, 29, 155, 152), Microsoft.UI.ColorHelper.FromArgb(255, 240, 255, 255)));

        _shellStatusText.SetBinding(TextBlock.TextProperty, CreateBinding(nameof(MainViewModel.StatusText)));
        _headerRightPanel.Children.Add(_shellStatusText);

        Grid.SetColumn(_headerRightPanel, 1);
        Grid.SetRow(_headerRightPanel, 0);
        _headerGrid.Children.Add(_headerRightPanel);

        return _headerGrid;
    }
    private UIElement BuildLeftPane()
    {
        var panel = new StackPanel
        {
            Spacing = 12,
            Padding = new Thickness(18, 18, 18, 18),
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Folders",
            FontFamily = new FontFamily("Constantia"),
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 231, 246, 248)),
        });

        panel.Children.Add(new TextBlock
        {
            Text = "Ordne Bausteine nach Kontext und halte Favoriten in Reichweite.",
            FontFamily = new FontFamily("Corbel"),
            FontSize = 13,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 194, 230, 236)),
            TextWrapping = TextWrapping.Wrap,
        });

        _folderList = new ListView
        {
            MinHeight = 340,
            MaxHeight = 900,
            DisplayMemberPath = "Name",
            SelectionMode = ListViewSelectionMode.Single,
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(35, 223, 242, 247)),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8),
            CornerRadius = new CornerRadius(16),
        };
        _folderList.SetBinding(ItemsControl.ItemsSourceProperty, CreateBinding(nameof(MainViewModel.Folders)));
        _folderList.SetBinding(ListView.SelectedItemProperty, CreateBinding(nameof(MainViewModel.SelectedFolder), BindingMode.TwoWay));
        panel.Children.Add(_folderList);

        var createButton = MakeCommandButton("Neuer Baustein", nameof(MainViewModel.CreateSnippetCommand), emphasized: true);
        createButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        panel.Children.Add(createButton);

        var statusCard = new Border
        {
            Padding = new Thickness(12, 10, 12, 10),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(130, 17, 98, 109)),
            CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(142, 101, 182, 191)),
            BorderThickness = new Thickness(1),
        };
        var status = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Corbel"),
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 234, 252, 253)),
            FontSize = 12,
        };
        status.SetBinding(TextBlock.TextProperty, CreateBinding(nameof(MainViewModel.StatusText)));
        statusCard.Child = status;
        panel.Children.Add(statusCard);

        AttachHoverMotion(_leftPaneCard, -3);
        return panel;
    }

    private UIElement BuildCenterPane()
    {
        var panel = new StackPanel
        {
            Spacing = 12,
            Padding = new Thickness(18, 18, 18, 18),
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Library",
            FontFamily = new FontFamily("Constantia"),
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 22, 54, 66)),
        });

        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
        };
        toolbar.Children.Add(MakeCommandButton("Suchen", nameof(MainViewModel.SearchCommand), emphasized: true));
        toolbar.Children.Add(MakeCommandButton("Ersetzen", nameof(MainViewModel.SearchReplaceVisibleCommand), emphasized: true));
        toolbar.Children.Add(MakeCommandButton("Duplikate", nameof(MainViewModel.RemoveDuplicatesCommand), emphasized: false));
        toolbar.Children.Add(MakeCommandButton("Bulk Font", nameof(MainViewModel.ApplyBulkFontCommand), emphasized: false));
        toolbar.Children.Add(MakeCommandButton("Duplizieren", nameof(MainViewModel.DuplicateSnippetCommand), emphasized: false));
        toolbar.Children.Add(MakeCommandButton("Verschieben", nameof(MainViewModel.MoveSnippetToSelectedFolderCommand), emphasized: false));
        toolbar.Children.Add(MakeCommandButton("Hervorheben", nameof(MainViewModel.ToggleHighlightCommand), emphasized: false));
        toolbar.Children.Add(MakeCommandButton("Ausblenden", nameof(MainViewModel.ToggleHiddenCommand), emphasized: false));
        panel.Children.Add(toolbar);

        var searchFrame = new Border
        {
            CornerRadius = new CornerRadius(14),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(84, 72, 121, 132)),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(190, 255, 255, 255)),
            Padding = new Thickness(10, 6, 10, 6),
        };
        var search = new AutoSuggestBox
        {
            PlaceholderText = "Suchen, ersetzen, filtern...",
            BorderThickness = new Thickness(0),
            FontFamily = new FontFamily("Corbel"),
            FontSize = 14,
        };
        search.SetBinding(AutoSuggestBox.TextProperty, CreateBinding(nameof(MainViewModel.SearchTerm), BindingMode.TwoWay));
        searchFrame.Child = search;
        panel.Children.Add(searchFrame);

        var replaceFrame = new Border
        {
            CornerRadius = new CornerRadius(14),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(84, 72, 121, 132)),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(190, 255, 255, 255)),
            Padding = new Thickness(10, 6, 10, 6),
        };
        var replace = new TextBox
        {
            PlaceholderText = "Ersetzen durch...",
            BorderThickness = new Thickness(0),
            FontFamily = new FontFamily("Corbel"),
            FontSize = 14,
        };
        replace.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.ReplaceTerm), BindingMode.TwoWay));
        replaceFrame.Child = replace;
        panel.Children.Add(replaceFrame);

        _snippetList = new ListView
        {
            DisplayMemberPath = "Title",
            MinHeight = 450,
            MaxHeight = 980,
            SelectionMode = ListViewSelectionMode.Single,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8),
            CornerRadius = new CornerRadius(16),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(152, 245, 252, 255)),
        };
        _snippetList.SetBinding(ItemsControl.ItemsSourceProperty, CreateBinding(nameof(MainViewModel.VisibleSnippets)));
        _snippetList.SetBinding(ListView.SelectedItemProperty, CreateBinding(nameof(MainViewModel.SelectedSnippet), BindingMode.TwoWay));
        panel.Children.Add(_snippetList);

        AttachHoverMotion(_centerPaneCard, -3);
        return panel;
    }

    private UIElement BuildEditorPane()
    {
        var panel = new StackPanel
        {
            Spacing = 10,
            Padding = new Thickness(18, 18, 18, 18),
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Editor Studio",
            FontFamily = new FontFamily("Constantia"),
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 22, 61, 74)),
        });

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
        };
        actions.Children.Add(MakeCommandButton("Speichern", nameof(MainViewModel.SaveSnippetCommand), emphasized: true));
        actions.Children.Add(MakeActionButton("Einfuegen", OnInsertClicked, emphasized: true));
        actions.Children.Add(MakeCommandButton("Simulieren", nameof(MainViewModel.SimulateInsertCommand), emphasized: false));
        actions.Children.Add(MakeCommandButton("Versionen", nameof(MainViewModel.LoadVersionsCommand), emphasized: false));
        actions.Children.Add(MakeCommandButton("Rollback", nameof(MainViewModel.RollbackToPreviousVersionCommand), emphasized: false));
        actions.Children.Add(MakeCommandButton("Papierkorb", nameof(MainViewModel.MoveToTrashCommand), emphasized: false));
        actions.Children.Add(MakeCommandButton("Aus Trash", nameof(MainViewModel.RestoreLatestTrashCommand), emphasized: false));
        actions.Children.Add(MakeCommandButton("Trash leeren", nameof(MainViewModel.PurgeTrashCommand), emphasized: false));
        actions.Children.Add(MakeCommandButton("Dokument", nameof(MainViewModel.GenerateDocumentCommand), emphasized: false));
        actions.Children.Add(MakeCommandButton("Korrektur", nameof(MainViewModel.ApplyTextCorrectionsCommand), emphasized: false));
        actions.Children.Add(MakeCommandButton("Clip Verlauf", nameof(MainViewModel.LoadClipboardHistoryCommand), emphasized: false));
        actions.Children.Add(MakeCommandButton("Clip laden", nameof(MainViewModel.InsertLatestClipboardHistoryCommand), emphasized: false));
        panel.Children.Add(new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = actions,
        });

        var editorToolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
        };
        editorToolbar.Children.Add(MakeActionButton("Fett", OnFormatBoldClicked, emphasized: false));
        editorToolbar.Children.Add(MakeActionButton("Kursiv", OnFormatItalicClicked, emphasized: false));
        editorToolbar.Children.Add(MakeActionButton("Unterstrichen", OnFormatUnderlineClicked, emphasized: false));
        editorToolbar.Children.Add(MakeActionButton("Liste", OnBulletListClicked, emphasized: false));
        editorToolbar.Children.Add(MakeActionButton("Nummeriert", OnNumberedListClicked, emphasized: false));

        editorToolbar.Children.Add(new Border
        {
            Width = 1,
            Height = 28,
            Margin = new Thickness(4, 0, 4, 0),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(72, 54, 102, 117)),
        });

        editorToolbar.Children.Add(_placeholderTokenInput);
        editorToolbar.Children.Add(MakeActionButton("Placeholder", OnInsertPlaceholderClicked, emphasized: false));

        editorToolbar.Children.Add(new TextBlock
        {
            Text = "Tabelle",
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Corbel"),
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 42, 77, 91)),
        });
        editorToolbar.Children.Add(_tableRowsInput);
        editorToolbar.Children.Add(new TextBlock
        {
            Text = "x",
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Corbel"),
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 42, 77, 91)),
        });
        editorToolbar.Children.Add(_tableColumnsInput);
        editorToolbar.Children.Add(MakeActionButton("Einfuegen", OnInsertTableClicked, emphasized: false));

        editorToolbar.Children.Add(_imagePathInput);
        editorToolbar.Children.Add(MakeActionButton("Bild", OnInsertImageClicked, emphasized: false));

        panel.Children.Add(new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = editorToolbar,
        });

        var titleBox = new TextBox
        {
            Header = "Titel",
            FontFamily = new FontFamily("Corbel"),
            CornerRadius = new CornerRadius(10),
        };
        titleBox.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.EditorTitle), BindingMode.TwoWay));
        panel.Children.Add(titleBox);

        var tagGrid = new Grid
        {
            ColumnSpacing = 8,
        };
        tagGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        tagGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var tagInput = new TextBox
        {
            Header = "Tag",
            FontFamily = new FontFamily("Corbel"),
            CornerRadius = new CornerRadius(10),
        };
        tagInput.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.EditorTagInput), BindingMode.TwoWay));
        tagGrid.Children.Add(tagInput);

        var tagButton = MakeCommandButton("Tag speichern", nameof(MainViewModel.AddTagCommand), emphasized: false);
        Grid.SetColumn(tagButton, 1);
        tagGrid.Children.Add(tagButton);
        panel.Children.Add(tagGrid);

        var commentGrid = new Grid
        {
            ColumnSpacing = 8,
        };
        commentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        commentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var commentInput = new TextBox
        {
            Header = "Kommentar",
            FontFamily = new FontFamily("Corbel"),
            CornerRadius = new CornerRadius(10),
        };
        commentInput.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.EditorCommentInput), BindingMode.TwoWay));
        commentGrid.Children.Add(commentInput);

        var commentButton = MakeCommandButton("Kommentar speichern", nameof(MainViewModel.AddCommentCommand), emphasized: false);
        Grid.SetColumn(commentButton, 1);
        commentGrid.Children.Add(commentButton);
        panel.Children.Add(commentGrid);

        _editorSplitGrid = new Grid
        {
            RowSpacing = 8,
            MinHeight = 460,
        };
        _editorSplitGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0.95, GridUnitType.Star) });
        _editorSplitGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.45, GridUnitType.Star) });

        var plain = new TextBox
        {
            Header = "Text (plain)",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Corbel"),
            FontSize = 14,
            CornerRadius = new CornerRadius(14),
        };
        ScrollViewer.SetVerticalScrollBarVisibility(plain, ScrollBarVisibility.Auto);
        plain.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.EditorPlainText), BindingMode.TwoWay));
        Grid.SetRow(plain, 0);
        _editorSplitGrid.Children.Add(plain);
        var editorFrame = new Border
        {
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(220, 236, 244, 248)),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(78, 56, 111, 127)),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(10),
        };

        _previewHost.Children.Clear();
        _previewHost.Children.Add(BuildPreviewControl());
        editorFrame.Child = _previewHost;

        Grid.SetRow(editorFrame, 1);
        _editorSplitGrid.Children.Add(editorFrame);

        panel.Children.Add(_editorSplitGrid);

        panel.Children.Add(new TextBlock
        {
            Text = "WYSIWYG Editor: Formatierung, Tabellen, Bilder und Placeholder mit sicherer Sanitization.",
            FontFamily = new FontFamily("Corbel"),
            FontSize = 12,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 74, 116, 127)),
        });

        var productivityGrid = new Grid
        {
            ColumnSpacing = 10,
        };
        productivityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        productivityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _documentPreview = new TextBox
        {
            Header = "Dokumentgenerator Vorschau",
            AcceptsReturn = true,
            IsReadOnly = true,
            MinHeight = 110,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Consolas"),
            CornerRadius = new CornerRadius(10),
        };
        ScrollViewer.SetVerticalScrollBarVisibility(_documentPreview, ScrollBarVisibility.Auto);
        _documentPreview.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.DocumentPreviewText), BindingMode.OneWay));
        productivityGrid.Children.Add(_documentPreview);

        _clipboardPreview = new TextBox
        {
            Header = "Mehrfach-Clipboard Vorschau",
            AcceptsReturn = true,
            IsReadOnly = true,
            MinHeight = 110,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Consolas"),
            CornerRadius = new CornerRadius(10),
        };
        ScrollViewer.SetVerticalScrollBarVisibility(_clipboardPreview, ScrollBarVisibility.Auto);
        _clipboardPreview.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.ClipboardPreviewText), BindingMode.OneWay));
        Grid.SetColumn(_clipboardPreview, 1);
        productivityGrid.Children.Add(_clipboardPreview);
        panel.Children.Add(productivityGrid);

        AttachHoverMotion(_editorCard, -3);
        return panel;
    }

    private UIElement BuildTriggerPane()
    {
        var panel = new Grid
        {
            Padding = new Thickness(16, 14, 16, 14),
        };
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new Grid
        {
            ColumnSpacing = 12,
        };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _triggerSummaryText.Text = "Keine Trigger geladen";
        header.Children.Add(_triggerSummaryText);

        var liveBadge = CreateBadge("LISTENING", Microsoft.UI.ColorHelper.FromArgb(255, 41, 188, 182), Microsoft.UI.ColorHelper.FromArgb(255, 241, 255, 253));
        Grid.SetColumn(liveBadge, 1);
        header.Children.Add(liveBadge);

        Grid.SetRow(header, 0);
        panel.Children.Add(header);

        _triggerTargetText.Margin = new Thickness(0, 6, 0, 10);
        Grid.SetRow(_triggerTargetText, 1);
        panel.Children.Add(_triggerTargetText);

        var scroller = new ScrollViewer
        {
            Content = _triggerItemsPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        Grid.SetRow(scroller, 2);
        panel.Children.Add(scroller);

        AttachHoverMotion(_triggerCard, -3);
        return panel;
    }

    private FrameworkElement BuildPreviewControl()
    {
        try
        {
            _htmlPreview = new WebView2
            {
                DefaultBackgroundColor = Microsoft.UI.Colors.Transparent,
            };
            _isWebViewInitialized = false;
            _isEditorDomReady = false;

            return _htmlPreview;
        }
        catch (Exception ex)
        {
            _htmlPreview = null;
            _isWebViewInitialized = false;
            _isEditorDomReady = false;
            _viewModel.StatusText = $"WebView2 Fallback aktiv: {ex.GetType().Name}";
            _htmlFallbackPreview.Text = BuildEditorHtmlFragment(_viewModel.EditorPlainText, _viewModel.EditorHtmlText);
            return _htmlFallbackPreview;
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.InitializeAsync();
            await EnsurePreviewInitializedAsync();
            await PushViewModelContentToEditorAsync();
            RefreshTriggerPanel();
            RunRevealAnimation();
            ApplyResponsiveLayout(ActualWidth);
        }
        catch (Exception ex)
        {
            _viewModel.StatusText = $"Initialisierung fehlgeschlagen: {ex.Message}";
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyResponsiveLayout(e.NewSize.Width);
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.EditorHtmlText) or nameof(MainViewModel.EditorPlainText))
        {
            if (!_isUpdatingFromEditor)
            {
                _ = PushViewModelContentToEditorAsync();
            }
        }

        if (e.PropertyName is nameof(MainViewModel.SelectedSnippet))
        {
            RefreshTriggerPanel();
        }
    }

    private async Task PushViewModelContentToEditorAsync()
    {
        var htmlFragment = BuildEditorHtmlFragment(_viewModel.EditorPlainText, _viewModel.EditorHtmlText);

        if (_htmlPreview?.CoreWebView2 is not null && _isWebViewInitialized && _isEditorDomReady)
        {
            try
            {
                var jsonPayload = JsonSerializer.Serialize(htmlFragment);
                await _htmlPreview.CoreWebView2.ExecuteScriptAsync($"window.textyEditor?.setHtml({jsonPayload});");
                return;
            }
            catch
            {
                ActivateFallbackPreview("WebView2 Editor nicht verfuegbar, Fallback aktiv.", htmlFragment);
            }
        }

        _htmlFallbackPreview.Text = htmlFragment;
    }
    private void RefreshTriggerPanel()
    {
        _triggerItemsPanel.Children.Clear();

        var snippet = _viewModel.SelectedSnippet?.Source;
        if (snippet is null)
        {
            _triggerSummaryText.Text = "Keine Auswahl";
            _triggerTargetText.Text = "Zielprozess: -";
            _triggerItemsPanel.Children.Add(BuildHintCard("Waehle einen Baustein, um Trigger, Scope und Zielprozess zu sehen."));
            return;
        }

        var triggers = snippet.Triggers;
        var enabledCount = triggers.Count(t => t.Enabled);
        _triggerSummaryText.Text = enabledCount == 1
            ? "1 aktiver Trigger"
            : $"{enabledCount} aktive Trigger";

        var distinctTargets = triggers
            .Where(t => t.Enabled && !string.IsNullOrWhiteSpace(t.TargetProcess))
            .Select(t => NormalizeProcessName(t.TargetProcess))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _triggerTargetText.Text = distinctTargets.Count == 0
            ? "Zielprozess: alle Anwendungen"
            : $"Zielprozess: {string.Join(" | ", distinctTargets)}";

        if (triggers.Count == 0)
        {
            _triggerItemsPanel.Children.Add(BuildHintCard("Fuer diesen Baustein sind noch keine Trigger-Regeln gespeichert."));
            return;
        }

        foreach (var trigger in triggers.OrderByDescending(t => t.Enabled).ThenBy(t => t.Type.ToString()))
        {
            _triggerItemsPanel.Children.Add(BuildTriggerItem(trigger));
        }
    }

    private UIElement BuildTriggerItem(TriggerRule trigger)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(trigger.Enabled
                ? Microsoft.UI.ColorHelper.FromArgb(80, 63, 141, 149)
                : Microsoft.UI.ColorHelper.FromArgb(60, 56, 79, 93)),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(trigger.Enabled
                ? Microsoft.UI.ColorHelper.FromArgb(130, 123, 225, 221)
                : Microsoft.UI.ColorHelper.FromArgb(68, 110, 144, 151)),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(10),
        };

        var panel = new StackPanel { Spacing = 6 };

        var top = new Grid();
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        top.Children.Add(new TextBlock
        {
            Text = trigger.Pattern,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 232, 250, 250)),
        });

        var stateBadge = CreateBadge(
            trigger.Enabled ? "AKTIV" : "PAUSIERT",
            trigger.Enabled
                ? Microsoft.UI.ColorHelper.FromArgb(255, 35, 168, 164)
                : Microsoft.UI.ColorHelper.FromArgb(255, 74, 104, 116),
            Microsoft.UI.ColorHelper.FromArgb(255, 240, 255, 255));
        Grid.SetColumn(stateBadge, 1);
        top.Children.Add(stateBadge);

        panel.Children.Add(top);

        var chipRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
        };
        chipRow.Children.Add(CreateBadge(trigger.Type.ToString().ToUpperInvariant(), Microsoft.UI.ColorHelper.FromArgb(255, 21, 114, 158), Microsoft.UI.ColorHelper.FromArgb(255, 236, 248, 255)));
        chipRow.Children.Add(CreateBadge(trigger.Scope.ToString().ToUpperInvariant(), Microsoft.UI.ColorHelper.FromArgb(255, 29, 128, 120), Microsoft.UI.ColorHelper.FromArgb(255, 234, 253, 248)));

        var targetLabel = string.IsNullOrWhiteSpace(trigger.TargetProcess)
            ? "ALL"
            : NormalizeProcessName(trigger.TargetProcess);
        chipRow.Children.Add(CreateBadge(targetLabel.ToUpperInvariant(), Microsoft.UI.ColorHelper.FromArgb(255, 120, 86, 35), Microsoft.UI.ColorHelper.FromArgb(255, 255, 246, 232)));

        panel.Children.Add(chipRow);

        border.Child = panel;
        return border;
    }

    private static string NormalizeProcessName(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return "all";
        }

        var trimmed = processName.Trim();
        return trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? trimmed[..^4]
            : trimmed;
    }

    private static Border BuildHintCard(string text)
    {
        return new Border
        {
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(60, 66, 94, 109)),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(75, 121, 169, 178)),
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Corbel"),
                Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 202, 233, 237)),
            },
        };
    }

    private static Button MakeCommandButton(string text, string commandPath, bool emphasized)
    {
        var button = new Button
        {
            Content = text,
            Padding = new Thickness(14, 7, 14, 7),
            CornerRadius = new CornerRadius(10),
            FontFamily = new FontFamily("Corbel"),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(emphasized
                ? Microsoft.UI.ColorHelper.FromArgb(255, 34, 170, 165)
                : Microsoft.UI.ColorHelper.FromArgb(130, 80, 126, 138)),
            Background = new SolidColorBrush(emphasized
                ? Microsoft.UI.ColorHelper.FromArgb(255, 20, 126, 121)
                : Microsoft.UI.ColorHelper.FromArgb(165, 240, 247, 250)),
            Foreground = new SolidColorBrush(emphasized
                ? Microsoft.UI.ColorHelper.FromArgb(255, 244, 255, 255)
                : Microsoft.UI.ColorHelper.FromArgb(255, 29, 62, 74)),
        };

        AttachHoverMotion(button, -2);
        button.SetBinding(Button.CommandProperty, CreateBinding(commandPath));
        return button;
    }
    private static Border CreateBadge(string text, Windows.UI.Color background, Windows.UI.Color foreground)
    {
        var safeForeground = EnsureReadableForeground(background, foreground);
        return new Border
        {
            Background = new SolidColorBrush(background),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 2, 8, 2),
            Child = new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Corbel"),
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(safeForeground),
            },
        };
    }

    private static Border CreateCard(Windows.UI.Color background, Windows.UI.Color border)
    {
        return new Border
        {
            Background = new SolidColorBrush(background),
            BorderBrush = new SolidColorBrush(border),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(22),
        };
    }

    private static LinearGradientBrush CreateBackdropBrush()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0),
            EndPoint = new Windows.Foundation.Point(1, 1),
        };
        brush.GradientStops.Add(new GradientStop { Color = Microsoft.UI.ColorHelper.FromArgb(255, 248, 243, 232), Offset = 0 });
        brush.GradientStops.Add(new GradientStop { Color = Microsoft.UI.ColorHelper.FromArgb(255, 228, 240, 244), Offset = 0.55 });
        brush.GradientStops.Add(new GradientStop { Color = Microsoft.UI.ColorHelper.FromArgb(255, 244, 237, 226), Offset = 1 });
        return brush;
    }

    private static FrameworkElement BuildAtmosphereLayer()
    {
        var canvas = new Canvas
        {
            IsHitTestVisible = false,
            Opacity = 0.6,
        };

        canvas.Children.Add(new Ellipse
        {
            Width = 420,
            Height = 420,
            Fill = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 33, 193, 186)),
            Margin = new Thickness(-80, -110, 0, 0),
        });

        canvas.Children.Add(new Ellipse
        {
            Width = 340,
            Height = 340,
            Fill = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(35, 230, 145, 68)),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, -70, -110, 0),
        });

        canvas.Children.Add(new Rectangle
        {
            Height = 280,
            Width = 840,
            Fill = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(20, 12, 42, 55)),
            Margin = new Thickness(140, 340, 0, 0),
            RenderTransform = new RotateTransform { Angle = -9 },
        });

        return canvas;
    }

    private static Windows.UI.Color EnsureReadableForeground(Windows.UI.Color background, Windows.UI.Color preferredForeground)
    {
        if (GetContrastRatio(background, preferredForeground) >= 4.5)
        {
            return preferredForeground;
        }

        var light = Microsoft.UI.ColorHelper.FromArgb(255, 245, 253, 255);
        var dark = Microsoft.UI.ColorHelper.FromArgb(255, 18, 45, 57);
        return GetContrastRatio(background, light) >= GetContrastRatio(background, dark)
            ? light
            : dark;
    }

    private static double GetContrastRatio(Windows.UI.Color background, Windows.UI.Color foreground)
    {
        var l1 = GetRelativeLuminance(background);
        var l2 = GetRelativeLuminance(foreground);
        var lighter = Math.Max(l1, l2);
        var darker = Math.Min(l1, l2);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double GetRelativeLuminance(Windows.UI.Color color)
    {
        static double ConvertChannel(byte channel)
        {
            var value = channel / 255d;
            return value <= 0.03928
                ? value / 12.92
                : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        var r = ConvertChannel(color.R);
        var g = ConvertChannel(color.G);
        var b = ConvertChannel(color.B);
        return (0.2126 * r) + (0.7152 * g) + (0.0722 * b);
    }

    private void ApplyResponsiveLayout(double width)
    {
        var effective = width > 0 ? width : 1400;
        var stackedLayout = effective < 1280;
        var compact = effective < 900;

        _headerCard.Margin = compact
            ? new Thickness(12, 12, 12, 0)
            : stackedLayout
                ? new Thickness(16, 14, 16, 0)
                : new Thickness(24, 24, 24, 0);
        _headerCard.Padding = compact
            ? new Thickness(16, 14, 16, 12)
            : stackedLayout
                ? new Thickness(20, 16, 20, 14)
                : new Thickness(24, 20, 24, 18);

        _mainGrid.Margin = compact
            ? new Thickness(12, 12, 12, 16)
            : stackedLayout
                ? new Thickness(16, 14, 16, 20)
                : new Thickness(24, 18, 24, 24);
        _mainGrid.ColumnSpacing = compact ? 10 : stackedLayout ? 14 : 18;
        _mainGrid.RowSpacing = compact ? 10 : stackedLayout ? 14 : 18;

        ConfigureHeaderLayout(stackedLayout);
        ApplyDensityByWidth(effective);

        _mainGrid.ColumnDefinitions.Clear();
        _mainGrid.RowDefinitions.Clear();

        if (stackedLayout)
        {
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetColumn(_leftPaneCard, 0);
            Grid.SetRow(_leftPaneCard, 0);

            Grid.SetColumn(_centerPaneCard, 0);
            Grid.SetRow(_centerPaneCard, 1);

            Grid.SetColumn(_rightColumnGrid, 0);
            Grid.SetRow(_rightColumnGrid, 2);

            _rightColumnGrid.RowDefinitions[0].Height = GridLength.Auto;
            _rightColumnGrid.RowDefinitions[1].Height = GridLength.Auto;
        }
        else
        {
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(430) });
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(_leftPaneCard, 0);
            Grid.SetRow(_leftPaneCard, 0);

            Grid.SetColumn(_centerPaneCard, 1);
            Grid.SetRow(_centerPaneCard, 0);

            Grid.SetColumn(_rightColumnGrid, 2);
            Grid.SetRow(_rightColumnGrid, 0);

            _rightColumnGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
            _rightColumnGrid.RowDefinitions[1].Height = new GridLength(effective < 1500 ? 240 : 280);
        }
    }

    private void ConfigureHeaderLayout(bool stackedLayout)
    {
        if (_headerGrid is null || _headerRightPanel is null)
        {
            return;
        }

        if (stackedLayout)
        {
            _headerGrid.ColumnSpacing = 8;
            _headerGrid.ColumnDefinitions[1].Width = new GridLength(0);
            _headerGrid.RowDefinitions[1].Height = GridLength.Auto;

            Grid.SetColumn(_headerRightPanel, 0);
            Grid.SetColumnSpan(_headerRightPanel, 2);
            Grid.SetRow(_headerRightPanel, 1);

            _headerRightPanel.HorizontalAlignment = HorizontalAlignment.Left;
            _shellStatusText.HorizontalAlignment = HorizontalAlignment.Left;
            _shellStatusText.MaxWidth = double.PositiveInfinity;
        }
        else
        {
            _headerGrid.ColumnSpacing = 24;
            _headerGrid.ColumnDefinitions[1].Width = GridLength.Auto;
            _headerGrid.RowDefinitions[1].Height = new GridLength(0);

            Grid.SetColumn(_headerRightPanel, 1);
            Grid.SetColumnSpan(_headerRightPanel, 1);
            Grid.SetRow(_headerRightPanel, 0);

            _headerRightPanel.HorizontalAlignment = HorizontalAlignment.Right;
            _shellStatusText.HorizontalAlignment = HorizontalAlignment.Right;
            _shellStatusText.MaxWidth = 420;
        }
    }

    private void ApplyDensityByWidth(double width)
    {
        var compact = width < 900;
        var stacked = width < 1280;

        if (_folderList is not null)
        {
            _folderList.MinHeight = compact ? 180 : stacked ? 220 : 340;
            _folderList.MaxHeight = compact ? 420 : 900;
        }

        if (_snippetList is not null)
        {
            _snippetList.MinHeight = compact ? 240 : stacked ? 320 : 450;
            _snippetList.MaxHeight = compact ? 520 : 980;
        }

        if (_editorSplitGrid is not null)
        {
            _editorSplitGrid.MinHeight = compact ? 320 : stacked ? 380 : 460;
        }

        if (_documentPreview is not null)
        {
            _documentPreview.MinHeight = compact ? 86 : 110;
        }

        if (_clipboardPreview is not null)
        {
            _clipboardPreview.MinHeight = compact ? 86 : 110;
        }
    }

    private void RunRevealAnimation()
    {
        if (_hasRunRevealAnimation)
        {
            return;
        }

        _hasRunRevealAnimation = true;

        var revealTargets = new FrameworkElement[]
        {
            _headerCard,
            _leftPaneCard,
            _centerPaneCard,
            _editorCard,
            _triggerCard,
        };

        for (var i = 0; i < revealTargets.Length; i++)
        {
            var target = revealTargets[i];
            target.Opacity = 0;
            var transform = target.RenderTransform as TranslateTransform;
            if (transform is null)
            {
                transform = new TranslateTransform();
                target.RenderTransform = transform;
            }

            transform.Y = 22;

            var storyboard = new Storyboard();

            var fade = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(420),
                BeginTime = TimeSpan.FromMilliseconds(i * 85),
            };
            Storyboard.SetTarget(fade, target);
            Storyboard.SetTargetProperty(fade, nameof(UIElement.Opacity));
            storyboard.Children.Add(fade);

            var slide = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(460),
                BeginTime = TimeSpan.FromMilliseconds(i * 85),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            };
            Storyboard.SetTarget(slide, transform);
            Storyboard.SetTargetProperty(slide, nameof(TranslateTransform.Y));
            storyboard.Children.Add(slide);

            storyboard.Begin();
        }
    }

    private static void AttachHoverMotion(UIElement element, double lift)
    {
        if (element is not FrameworkElement framework)
        {
            return;
        }

        var transform = framework.RenderTransform as TranslateTransform;
        if (transform is null)
        {
            transform = new TranslateTransform();
            framework.RenderTransform = transform;
        }

        framework.PointerEntered += (_, _) => AnimateTranslateY(transform, lift);
        framework.PointerExited += (_, _) => AnimateTranslateY(transform, 0);
        framework.PointerCanceled += (_, _) => AnimateTranslateY(transform, 0);
    }
    private static void AnimateTranslateY(TranslateTransform transform, double target)
    {
        var storyboard = new Storyboard();
        var animation = new DoubleAnimation
        {
            To = target,
            Duration = TimeSpan.FromMilliseconds(160),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };

        Storyboard.SetTarget(animation, transform);
        Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.Y));
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    private static Binding CreateBinding(string path, BindingMode mode = BindingMode.OneWay)
    {
        return new Binding
        {
            Path = new PropertyPath(path),
            Mode = mode,
        };
    }

    private static string BuildSafePreviewHtml(string plainText, string? htmlText)
    {
        return BuildEditorHtmlFragment(plainText, htmlText);
    }

    private void ActivateFallbackPreview(string status, string html)
    {
        _htmlPreview = null;
        _isWebViewInitialized = false;
        _isEditorDomReady = false;
        _viewModel.StatusText = status;
        _htmlFallbackPreview.Text = html;
        _previewHost.Children.Clear();
        _previewHost.Children.Add(_htmlFallbackPreview);
    }

    private async Task EnsurePreviewInitializedAsync()
    {
        if (_htmlPreview is null || _isWebViewInitialized)
        {
            return;
        }

        try
        {
            await _htmlPreview.EnsureCoreWebView2Async();
            if (_htmlPreview.CoreWebView2 is null)
            {
                throw new InvalidOperationException("CoreWebView2 initialization failed.");
            }

            _htmlPreview.NavigationCompleted -= OnHtmlPreviewNavigationCompleted;
            _htmlPreview.NavigationCompleted += OnHtmlPreviewNavigationCompleted;
            _htmlPreview.CoreWebView2.WebMessageReceived -= OnHtmlPreviewWebMessageReceived;
            _htmlPreview.CoreWebView2.WebMessageReceived += OnHtmlPreviewWebMessageReceived;
            _htmlPreview.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _htmlPreview.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _htmlPreview.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = true;

            _htmlPreview.NavigateToString(BuildEditorShellHtml());
            _isWebViewInitialized = true;
            _isEditorDomReady = false;
        }
        catch
        {
            ActivateFallbackPreview(
                "WebView2 Editor nicht verfuegbar, Fallback aktiv.",
                BuildEditorHtmlFragment(_viewModel.EditorPlainText, _viewModel.EditorHtmlText));
        }
    }

    private static string WrapPreviewHtml(string bodyContent)
    {
        return bodyContent;
    }

    private static string SanitizeHtmlFragment(string html)
    {
        var cleaned = html;

        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"<\s*script\b[^>]*>[\s\S]*?<\s*/\s*script\s*>",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"<\s*(iframe|object|embed|link|meta)\b[^>]*>[\s\S]*?(<\s*/\s*\1\s*>)?",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"\s+on\w+\s*=\s*(['""]).*?\1",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"\s+on\w+\s*=\s*[^\s>]+",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"(?i)\b(href|src)\s*=\s*(['""])\s*javascript:[^'""]*\2",
            "$1=\"#\"");

        return cleaned;
    }
}
