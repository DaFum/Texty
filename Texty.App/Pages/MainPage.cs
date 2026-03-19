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
    private static readonly FontFamily DisplayFont = new("Sitka Display");
    private static readonly FontFamily BodyFont = new("Candara");
    private static readonly FontFamily MonoFont = new("Cascadia Code");

    private static readonly Windows.UI.Color AccentTeal = Microsoft.UI.ColorHelper.FromArgb(255, 20, 126, 121);
    private static readonly Windows.UI.Color AccentTealSoft = Microsoft.UI.ColorHelper.FromArgb(255, 34, 170, 165);
    private static readonly Windows.UI.Color AccentCopper = Microsoft.UI.ColorHelper.FromArgb(255, 176, 102, 49);
    private static readonly Windows.UI.Color InkDeep = Microsoft.UI.ColorHelper.FromArgb(255, 18, 42, 54);
    private static readonly Windows.UI.Color InkStrong = Microsoft.UI.ColorHelper.FromArgb(255, 20, 48, 61);
    private static readonly Windows.UI.Color InkLabel = Microsoft.UI.ColorHelper.FromArgb(255, 28, 63, 78);
    private static readonly Windows.UI.Color InkMeta = Microsoft.UI.ColorHelper.FromArgb(255, 40, 78, 92);
    private static readonly Windows.UI.Color PanelDark = Microsoft.UI.ColorHelper.FromArgb(236, 19, 38, 52);
    private static readonly Windows.UI.Color PanelDarkBorder = Microsoft.UI.ColorHelper.FromArgb(110, 96, 169, 184);
    private static readonly Windows.UI.Color PanelLight = Microsoft.UI.ColorHelper.FromArgb(232, 246, 242, 234);
    private static readonly Windows.UI.Color PanelLightBorder = Microsoft.UI.ColorHelper.FromArgb(95, 110, 122, 131);
    private static readonly Windows.UI.Color EditorLight = Microsoft.UI.ColorHelper.FromArgb(232, 236, 245, 250);
    private static readonly Windows.UI.Color EditorLightBorder = Microsoft.UI.ColorHelper.FromArgb(90, 83, 123, 139);
    private static readonly Windows.UI.Color TriggerDark = Microsoft.UI.ColorHelper.FromArgb(236, 22, 40, 57);
    private static readonly Windows.UI.Color TriggerDarkBorder = Microsoft.UI.ColorHelper.FromArgb(112, 120, 202, 202);

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
    private Grid? _productivityGrid;
    private Expander? _insertToolsExpander;
    private TextBlock? _documentPreviewLabel;
    private TextBlock? _clipboardPreviewLabel;
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
    private bool _toolbarReflowReady;

    private bool _hasRunRevealAnimation;
    private readonly List<AdaptiveToolbarLayout> _adaptiveToolbars = [];

    private enum ButtonVisualTier
    {
        Primary,
        Secondary,
        Tertiary,
    }

    private sealed class AdaptiveToolbarLayout
    {
        public required Border Container { get; init; }
        public required StackPanel RowsHost { get; init; }
        public required IReadOnlyList<Button> Buttons { get; init; }
        public double MinRowWidth { get; init; } = 280;
    }

    public MainPage()
    {
        _mainGrid = new Grid { Margin = new Thickness(24, 18, 24, 24), ColumnSpacing = 18, RowSpacing = 18 };

        _headerCard = new Border
        {
            Margin = new Thickness(24, 24, 24, 0),
            Padding = new Thickness(24, 20, 24, 18),
            CornerRadius = new CornerRadius(24),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(214, 250, 245, 236)),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(95, 78, 104, 116)),
            BorderThickness = new Thickness(1),
        };

        _leftPaneCard = CreateCard(
            PanelDark,
            PanelDarkBorder);
        _centerPaneCard = CreateCard(
            PanelLight,
            PanelLightBorder);

        _rightColumnGrid = new Grid { RowSpacing = 12 };
        _rightColumnGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _rightColumnGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(280) });

        _editorCard = CreateCard(
            EditorLight,
            EditorLightBorder);
        _triggerCard = CreateCard(
            TriggerDark,
            TriggerDarkBorder);

        _shellStatusText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(InkLabel),
            FontFamily = BodyFont,
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Right,
            MaxWidth = 460,
        };

        _triggerSummaryText = new TextBlock
        {
            FontFamily = DisplayFont,
            FontSize = 17,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 237, 247, 249)),
        };

        _triggerTargetText = new TextBlock
        {
            FontFamily = BodyFont,
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
            FontFamily = MonoFont,
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
            FontFamily = MonoFont,
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
            FontFamily = BodyFont,
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
            FontFamily = DisplayFont,
            FontSize = 36,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 17, 42, 52)),
            CharacterSpacing = 22,
        });
        left.Children.Add(new TextBlock
        {
            Text = "Snippet-Orchestrierung fuer Explorer, Editor und Trigger in einem Arbeitsfluss.",
            FontFamily = BodyFont,
            FontSize = 14,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 53, 87, 98)),
        });
        _headerGrid.Children.Add(left);

        _headerRightPanel = new StackPanel
        {
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        _headerRightPanel.Children.Add(CreateBadge("GLOBAL HOOK AKTIV", AccentTeal, Microsoft.UI.ColorHelper.FromArgb(255, 240, 255, 255)));

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
            FontFamily = DisplayFont,
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 231, 246, 248)),
        });

        panel.Children.Add(new TextBlock
        {
            Text = "Ordne Bausteine nach Kontext und halte Favoriten in Reichweite.",
            FontFamily = BodyFont,
            FontSize = 13,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 194, 230, 236)),
            TextWrapping = TextWrapping.Wrap,
        });

        _folderList = new ListView
        {
            MinHeight = 280,
            MaxHeight = 760,
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

        var createButton = MakeCommandButton("Neuer Baustein", nameof(MainViewModel.CreateSnippetCommand), ButtonVisualTier.Primary);
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
            FontFamily = BodyFont,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 234, 252, 253)),
            FontSize = 12,
            Text = "Schnellstart:\n- Baustein auswaehlen\n- Inhalte im Editor anpassen\n- Mit 'Einfuegen' ausgeben",
        };
        statusCard.Child = status;
        panel.Children.Add(statusCard);

        AttachHoverMotion(_leftPaneCard, -3);
        return panel;
    }

    private UIElement BuildCenterPane()
    {
        var panel = new StackPanel
        {
            Spacing = 10,
            Padding = new Thickness(18, 18, 18, 18),
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Library",
            FontFamily = DisplayFont,
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(InkStrong),
        });

        panel.Children.Add(BuildLibraryCommandBar());

        panel.Children.Add(CreateSectionLabel("Suche"));

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
            FontFamily = BodyFont,
            FontSize = 14,
        };
        ApplyInputChrome(search);
        search.SetBinding(AutoSuggestBox.TextProperty, CreateBinding(nameof(MainViewModel.SearchTerm), BindingMode.TwoWay));
        searchFrame.Child = search;
        panel.Children.Add(searchFrame);

        panel.Children.Add(CreateSectionLabel("Ersetzen (Scope: aktuelle Trefferliste)"));

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
            FontFamily = BodyFont,
            FontSize = 14,
        };
        ApplyInputChrome(replace);
        replace.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.ReplaceTerm), BindingMode.TwoWay));
        replaceFrame.Child = replace;
        panel.Children.Add(replaceFrame);

        _snippetList = new ListView
        {
            DisplayMemberPath = "Title",
            MinHeight = 340,
            MaxHeight = 780,
            SelectionMode = ListViewSelectionMode.Single,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8),
            CornerRadius = new CornerRadius(16),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(196, 245, 252, 255)),
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
            Spacing = 8,
            Padding = new Thickness(18, 18, 18, 18),
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Editor Studio",
            FontFamily = DisplayFont,
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(InkStrong),
        });

        panel.Children.Add(CreateSectionLabel("Workflow"));
        panel.Children.Add(BuildEditorWorkflowCommandBar());

        panel.Children.Add(CreateSectionLabel("Formatierung"));
        panel.Children.Add(BuildFormattingCommandBar());

        _insertToolsExpander = new Expander
        {
            Header = "Einfuegen-Tools (Placeholder, Tabelle, Bild)",
            IsExpanded = false,
            Margin = new Thickness(0, 2, 0, 4),
            Content = BuildInsertToolsPanel(),
        };
        panel.Children.Add(_insertToolsExpander);

        var titleBox = new TextBox
        {
            Header = "Titel",
            FontFamily = BodyFont,
            CornerRadius = new CornerRadius(10),
        };
        ApplyInputChrome(titleBox);
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
            FontFamily = BodyFont,
            CornerRadius = new CornerRadius(10),
        };
        ApplyInputChrome(tagInput);
        tagInput.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.EditorTagInput), BindingMode.TwoWay));
        tagGrid.Children.Add(tagInput);

        var tagButton = MakeCommandButton("Tag speichern", nameof(MainViewModel.AddTagCommand), ButtonVisualTier.Secondary);
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
            FontFamily = BodyFont,
            CornerRadius = new CornerRadius(10),
        };
        ApplyInputChrome(commentInput);
        commentInput.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.EditorCommentInput), BindingMode.TwoWay));
        commentGrid.Children.Add(commentInput);

        var commentButton = MakeCommandButton("Kommentar speichern", nameof(MainViewModel.AddCommentCommand), ButtonVisualTier.Secondary);
        Grid.SetColumn(commentButton, 1);
        commentGrid.Children.Add(commentButton);
        panel.Children.Add(commentGrid);

        _editorSplitGrid = new Grid
        {
            RowSpacing = 8,
            MinHeight = 360,
        };
        _editorSplitGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _editorSplitGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.12, GridUnitType.Star) });

        var plain = new TextBox
        {
            Header = "Text (plain)",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = BodyFont,
            FontSize = 14,
            CornerRadius = new CornerRadius(14),
        };
        ApplyInputChrome(plain);
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
            FontFamily = BodyFont,
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(InkMeta),
        });

        _productivityGrid = new Grid
        {
            ColumnSpacing = 10,
            RowSpacing = 6,
        };
        _productivityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _productivityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _productivityGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _productivityGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _documentPreviewLabel = new TextBlock
        {
            Text = "Dokumentgenerator Vorschau",
            FontFamily = BodyFont,
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(InkLabel),
        };
        _productivityGrid.Children.Add(_documentPreviewLabel);

        _documentPreview = new TextBox
        {
            AcceptsReturn = true,
            IsReadOnly = true,
            MinHeight = 110,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = MonoFont,
            CornerRadius = new CornerRadius(10),
        };
        ApplyInputChrome(_documentPreview);
        ScrollViewer.SetVerticalScrollBarVisibility(_documentPreview, ScrollBarVisibility.Auto);
        _documentPreview.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.DocumentPreviewText), BindingMode.OneWay));
        Grid.SetRow(_documentPreview, 1);
        _productivityGrid.Children.Add(_documentPreview);

        _clipboardPreviewLabel = new TextBlock
        {
            Text = "Mehrfach-Clipboard Vorschau",
            FontFamily = BodyFont,
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(InkLabel),
        };
        Grid.SetColumn(_clipboardPreviewLabel, 1);
        _productivityGrid.Children.Add(_clipboardPreviewLabel);

        _clipboardPreview = new TextBox
        {
            AcceptsReturn = true,
            IsReadOnly = true,
            MinHeight = 110,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = MonoFont,
            CornerRadius = new CornerRadius(10),
        };
        ApplyInputChrome(_clipboardPreview);
        ScrollViewer.SetVerticalScrollBarVisibility(_clipboardPreview, ScrollBarVisibility.Auto);
        _clipboardPreview.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.ClipboardPreviewText), BindingMode.OneWay));
        Grid.SetColumn(_clipboardPreview, 1);
        Grid.SetRow(_clipboardPreview, 1);
        _productivityGrid.Children.Add(_clipboardPreview);
        panel.Children.Add(_productivityGrid);

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

        var liveBadge = CreateBadge("LISTENING", AccentTealSoft, Microsoft.UI.ColorHelper.FromArgb(255, 241, 255, 253));
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

    private UIElement BuildLibraryCommandBar()
    {
        var buttons = new List<Button>
        {
            MakeToolbarCommandButton("Suchen", nameof(MainViewModel.SearchCommand), ButtonVisualTier.Primary),
            MakeToolbarCommandButton("Ersetzen", nameof(MainViewModel.SearchReplaceVisibleCommand), ButtonVisualTier.Secondary),
            MakeToolbarCommandButton("Duplikate", nameof(MainViewModel.RemoveDuplicatesCommand), ButtonVisualTier.Secondary),
            MakeToolbarCommandButton("Duplizieren", nameof(MainViewModel.DuplicateSnippetCommand), ButtonVisualTier.Secondary),
            MakeToolbarCommandButton("Verschieben", nameof(MainViewModel.MoveSnippetToSelectedFolderCommand), ButtonVisualTier.Tertiary),
            MakeToolbarCommandButton("Bulk Font", nameof(MainViewModel.ApplyBulkFontCommand), ButtonVisualTier.Tertiary),
            MakeToolbarCommandButton("Hervorheben", nameof(MainViewModel.ToggleHighlightCommand), ButtonVisualTier.Tertiary),
            MakeToolbarCommandButton("Ausblenden", nameof(MainViewModel.ToggleHiddenCommand), ButtonVisualTier.Tertiary),
        };
        return BuildAdaptiveToolbar(buttons, 300);
    }

    private UIElement BuildEditorWorkflowCommandBar()
    {
        var buttons = new List<Button>
        {
            MakeToolbarActionButton("Einfuegen", OnInsertClicked, ButtonVisualTier.Primary),
            MakeToolbarCommandButton("Speichern", nameof(MainViewModel.SaveSnippetCommand), ButtonVisualTier.Secondary),
            MakeToolbarCommandButton("Simulieren", nameof(MainViewModel.SimulateInsertCommand), ButtonVisualTier.Secondary),
            MakeToolbarCommandButton("Versionen", nameof(MainViewModel.LoadVersionsCommand), ButtonVisualTier.Secondary),
            MakeToolbarCommandButton("Rollback", nameof(MainViewModel.RollbackToPreviousVersionCommand), ButtonVisualTier.Secondary),
            MakeToolbarCommandButton("Papierkorb", nameof(MainViewModel.MoveToTrashCommand), ButtonVisualTier.Tertiary),
            MakeToolbarCommandButton("Aus Trash", nameof(MainViewModel.RestoreLatestTrashCommand), ButtonVisualTier.Tertiary),
            MakeToolbarCommandButton("Trash leeren", nameof(MainViewModel.PurgeTrashCommand), ButtonVisualTier.Tertiary),
            MakeToolbarCommandButton("Dokument", nameof(MainViewModel.GenerateDocumentCommand), ButtonVisualTier.Tertiary),
            MakeToolbarCommandButton("Korrektur", nameof(MainViewModel.ApplyTextCorrectionsCommand), ButtonVisualTier.Tertiary),
            MakeToolbarCommandButton("Clip Verlauf", nameof(MainViewModel.LoadClipboardHistoryCommand), ButtonVisualTier.Tertiary),
            MakeToolbarCommandButton("Clip laden", nameof(MainViewModel.InsertLatestClipboardHistoryCommand), ButtonVisualTier.Tertiary),
        };
        return BuildAdaptiveToolbar(buttons, 340);
    }

    private UIElement BuildFormattingCommandBar()
    {
        var buttons = new List<Button>
        {
            MakeToolbarActionButton("Fett", OnFormatBoldClicked, ButtonVisualTier.Primary),
            MakeToolbarActionButton("Kursiv", OnFormatItalicClicked, ButtonVisualTier.Secondary),
            MakeToolbarActionButton("Unterstrichen", OnFormatUnderlineClicked, ButtonVisualTier.Secondary),
            MakeToolbarActionButton("Liste", OnBulletListClicked, ButtonVisualTier.Tertiary),
            MakeToolbarActionButton("Nummeriert", OnNumberedListClicked, ButtonVisualTier.Tertiary),
        };
        return BuildAdaptiveToolbar(buttons, 280);
    }

    private UIElement BuildInsertToolsPanel()
    {
        ApplyInputChrome(_placeholderTokenInput);
        ApplyInputChrome(_tableRowsInput);
        ApplyInputChrome(_tableColumnsInput);
        ApplyInputChrome(_imagePathInput);

        var panel = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(2, 6, 2, 4),
        };

        var placeholderRow = new Grid { ColumnSpacing = 8 };
        placeholderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        placeholderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        placeholderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        placeholderRow.Children.Add(new TextBlock
        {
            Text = "Placeholder",
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = BodyFont,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(InkLabel),
        });
        Grid.SetColumn(_placeholderTokenInput, 1);
        placeholderRow.Children.Add(_placeholderTokenInput);
        var placeholderButton = MakeActionButton("Einsetzen", OnInsertPlaceholderClicked, ButtonVisualTier.Secondary);
        Grid.SetColumn(placeholderButton, 2);
        placeholderRow.Children.Add(placeholderButton);
        panel.Children.Add(placeholderRow);

        var tableRow = new Grid { ColumnSpacing = 8 };
        tableRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tableRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
        tableRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tableRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
        tableRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        tableRow.Children.Add(new TextBlock
        {
            Text = "Tabelle",
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = BodyFont,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(InkLabel),
        });
        Grid.SetColumn(_tableRowsInput, 1);
        tableRow.Children.Add(_tableRowsInput);
        var byText = new TextBlock
        {
            Text = "x",
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = BodyFont,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(InkLabel),
        };
        Grid.SetColumn(byText, 2);
        tableRow.Children.Add(byText);
        Grid.SetColumn(_tableColumnsInput, 3);
        tableRow.Children.Add(_tableColumnsInput);
        var tableButton = MakeActionButton("Einfuegen", OnInsertTableClicked, ButtonVisualTier.Secondary);
        Grid.SetColumn(tableButton, 4);
        tableRow.Children.Add(tableButton);
        panel.Children.Add(tableRow);

        var imageRow = new Grid { ColumnSpacing = 8 };
        imageRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        imageRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        imageRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        imageRow.Children.Add(new TextBlock
        {
            Text = "Bildpfad",
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = BodyFont,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(InkLabel),
        });
        Grid.SetColumn(_imagePathInput, 1);
        imageRow.Children.Add(_imagePathInput);
        var imageButton = MakeActionButton("Bild einfuegen", OnInsertImageClicked, ButtonVisualTier.Secondary);
        Grid.SetColumn(imageButton, 2);
        imageRow.Children.Add(imageButton);
        panel.Children.Add(imageRow);

        return panel;
    }

    private UIElement BuildAdaptiveToolbar(IReadOnlyList<Button> buttons, double minRowWidth)
    {
        var rowsHost = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
        };

        var container = new Border
        {
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(118, 86, 121, 136)),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(200, 246, 251, 255)),
            Padding = new Thickness(8),
            Child = rowsHost,
        };

        var layout = new AdaptiveToolbarLayout
        {
            Container = container,
            RowsHost = rowsHost,
            Buttons = buttons.ToList(),
            MinRowWidth = minRowWidth,
        };

        // Initial render without dynamic re-parenting during early page construction.
        var initialRow = CreateToolbarRow();
        foreach (var button in layout.Buttons)
        {
            button.Margin = new Thickness(0, 0, 8, 0);
            initialRow.Children.Add(button);
        }

        layout.RowsHost.Children.Add(initialRow);
        _adaptiveToolbars.Add(layout);
        container.SizeChanged += (_, args) =>
        {
            if (!_toolbarReflowReady)
            {
                return;
            }

            try
            {
                ReflowAdaptiveToolbar(layout, args.NewSize.Width);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"Adaptive toolbar reflow failed on size change: {ex}");
            }
        };

        return container;
    }

    private void ReflowAdaptiveToolbars()
    {
        if (!_toolbarReflowReady)
        {
            return;
        }

        foreach (var layout in _adaptiveToolbars)
        {
            try
            {
                ReflowAdaptiveToolbar(layout, layout.Container.ActualWidth);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"Adaptive toolbar reflow failed: {ex}");
            }
        }
    }

    private static void ReflowAdaptiveToolbar(AdaptiveToolbarLayout layout, double widthHint)
    {
        var availableWidth = Math.Max(layout.MinRowWidth, widthHint > 0 ? widthHint - 22 : layout.MinRowWidth);
        layout.RowsHost.Children.Clear();

        var currentRow = CreateToolbarRow();
        var currentWidth = 0d;

        foreach (var button in layout.Buttons)
        {
            // Buttons can already belong to a previous row from an earlier reflow.
            // Detach first to avoid "already has a parent" exceptions on startup/resize.
            if (button.Parent is Panel previousParent)
            {
                previousParent.Children.Remove(button);
            }

            var estimatedWidth = EstimateButtonWidth(button);
            if (currentRow.Children.Count > 0 && currentWidth + estimatedWidth > availableWidth)
            {
                layout.RowsHost.Children.Add(currentRow);
                currentRow = CreateToolbarRow();
                currentWidth = 0;
            }

            button.Margin = new Thickness(0, 0, 8, 0);
            currentRow.Children.Add(button);
            currentWidth += estimatedWidth;
        }

        if (currentRow.Children.Count > 0)
        {
            layout.RowsHost.Children.Add(currentRow);
        }
    }

    private static StackPanel CreateToolbarRow()
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 0,
        };
    }

    private static double EstimateButtonWidth(Button button)
    {
        var text = button.Content as string ?? string.Empty;
        return Math.Max(98, (text.Length * 8) + 54);
    }

    private static Button MakeToolbarCommandButton(string text, string commandPath, ButtonVisualTier tier)
    {
        var button = MakeCommandButton(text, commandPath, tier);
        ApplyToolbarButtonMetrics(button);
        return button;
    }

    private static Button MakeToolbarActionButton(string text, RoutedEventHandler onClick, ButtonVisualTier tier)
    {
        var button = MakeActionButton(text, onClick, tier);
        ApplyToolbarButtonMetrics(button);
        return button;
    }

    private static void ApplyToolbarButtonMetrics(Button button)
    {
        button.MinWidth = 98;
        button.MinHeight = 34;
        button.Margin = new Thickness(0, 0, 8, 0);
    }

    private static TextBlock CreateSectionLabel(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontFamily = BodyFont,
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(InkLabel),
            Margin = new Thickness(0, 2, 0, 2),
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
            _toolbarReflowReady = true;
            ReflowAdaptiveToolbars();
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
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"WebView2 setHtml failed: {ex}");
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
            FontFamily = MonoFont,
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
                FontFamily = BodyFont,
                Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 202, 233, 237)),
            },
        };
    }

    private static void ApplyInputChrome(Control control)
    {
        var foreground = new SolidColorBrush(InkStrong);
        control.Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(248, 253, 255, 255));
        control.BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(178, 74, 111, 127));
        control.BorderThickness = new Thickness(1);
        control.Foreground = foreground;
        var headerForeground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 41, 77, 92));
        control.Resources["TextControlPlaceholderForeground"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 71, 107, 122));
        control.Resources["TextControlForeground"] = foreground;
        control.Resources["TextControlBackground"] = control.Background;
        control.Resources["TextControlBorderBrush"] = control.BorderBrush;
        control.Resources["TextControlHeaderForeground"] = headerForeground;
        control.UseSystemFocusVisuals = true;
    }

    private static Button MakeCommandButton(string text, string commandPath, ButtonVisualTier tier)
    {
        var button = BuildStyledButton(text, tier);

        AttachHoverMotion(button, -2);
        button.SetBinding(Button.CommandProperty, CreateBinding(commandPath));
        return button;
    }

    private static Button BuildStyledButton(string text, ButtonVisualTier tier)
    {
        var palette = ResolveButtonPalette(tier);
        return new Button
        {
            Content = text,
            Padding = new Thickness(14, 7, 14, 7),
            CornerRadius = new CornerRadius(10),
            FontFamily = BodyFont,
            FontWeight = tier == ButtonVisualTier.Tertiary
                ? Microsoft.UI.Text.FontWeights.Normal
                : Microsoft.UI.Text.FontWeights.SemiBold,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(palette.Border),
            Background = new SolidColorBrush(palette.Background),
            Foreground = new SolidColorBrush(palette.Foreground),
        };
    }

    private static (Windows.UI.Color Background, Windows.UI.Color Border, Windows.UI.Color Foreground) ResolveButtonPalette(ButtonVisualTier tier)
    {
        return tier switch
        {
            ButtonVisualTier.Primary => (
                AccentTeal,
                Microsoft.UI.ColorHelper.FromArgb(255, 14, 131, 127),
                Microsoft.UI.ColorHelper.FromArgb(255, 245, 255, 255)),
            ButtonVisualTier.Tertiary => (
                Microsoft.UI.ColorHelper.FromArgb(36, 255, 255, 255),
                Microsoft.UI.ColorHelper.FromArgb(112, 86, 120, 136),
                Microsoft.UI.ColorHelper.FromArgb(255, 51, 82, 96)),
            _ => (
                Microsoft.UI.ColorHelper.FromArgb(244, 239, 248, 252),
                Microsoft.UI.ColorHelper.FromArgb(176, 69, 106, 122),
                Microsoft.UI.ColorHelper.FromArgb(255, 28, 63, 78)),
        };
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
                FontFamily = BodyFont,
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
        brush.GradientStops.Add(new GradientStop { Color = Microsoft.UI.ColorHelper.FromArgb(255, 246, 236, 220), Offset = 0 });
        brush.GradientStops.Add(new GradientStop { Color = Microsoft.UI.ColorHelper.FromArgb(255, 220, 236, 242), Offset = 0.52 });
        brush.GradientStops.Add(new GradientStop { Color = Microsoft.UI.ColorHelper.FromArgb(255, 238, 230, 220), Offset = 1 });
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
            Fill = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(44, AccentTeal.R, AccentTeal.G, AccentTeal.B)),
            Margin = new Thickness(-80, -110, 0, 0),
        });

        canvas.Children.Add(new Ellipse
        {
            Width = 340,
            Height = 340,
            Fill = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(38, AccentCopper.R, AccentCopper.G, AccentCopper.B)),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, -70, -110, 0),
        });

        canvas.Children.Add(new Rectangle
        {
            Height = 280,
            Width = 840,
            Fill = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(22, InkDeep.R, InkDeep.G, InkDeep.B)),
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
        var stackedLayout = effective < 1300;
        var compact = effective < 980;
        var wide = effective >= 1620;

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
        _mainGrid.ColumnSpacing = compact ? 8 : stackedLayout ? 12 : wide ? 20 : 16;
        _mainGrid.RowSpacing = compact ? 8 : stackedLayout ? 12 : wide ? 20 : 16;

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
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(wide ? 320 : 280) });
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(wide ? 460 : 390) });
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(_leftPaneCard, 0);
            Grid.SetRow(_leftPaneCard, 0);

            Grid.SetColumn(_centerPaneCard, 1);
            Grid.SetRow(_centerPaneCard, 0);

            Grid.SetColumn(_rightColumnGrid, 2);
            Grid.SetRow(_rightColumnGrid, 0);

            _rightColumnGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
            _rightColumnGrid.RowDefinitions[1].Height = new GridLength(wide ? 220 : 180);
        }

        ReflowAdaptiveToolbars();
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
        var compact = width < 980;
        var stacked = width < 1300;
        var medium = width >= 980 && width < 1300;

        if (_folderList is not null)
        {
            _folderList.MinHeight = compact ? 150 : stacked ? 190 : 260;
            _folderList.MaxHeight = compact ? 300 : medium ? 420 : 760;
        }

        if (_snippetList is not null)
        {
            _snippetList.MinHeight = compact ? 190 : stacked ? 250 : 340;
            _snippetList.MaxHeight = compact ? 340 : medium ? 520 : 780;
        }

        if (_editorSplitGrid is not null)
        {
            _editorSplitGrid.MinHeight = compact ? 230 : stacked ? 290 : 350;
        }

        if (_documentPreview is not null)
        {
            _documentPreview.MinHeight = compact ? 72 : 88;
        }

        if (_clipboardPreview is not null)
        {
            _clipboardPreview.MinHeight = compact ? 72 : 88;
        }

        if (_insertToolsExpander is not null && compact)
        {
            _insertToolsExpander.IsExpanded = false;
        }

        if (_productivityGrid is not null &&
            _documentPreview is not null &&
            _clipboardPreview is not null &&
            _documentPreviewLabel is not null &&
            _clipboardPreviewLabel is not null)
        {
            _productivityGrid.RowDefinitions.Clear();
            _productivityGrid.ColumnDefinitions.Clear();
            if (compact || medium)
            {
                _productivityGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                _productivityGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                _productivityGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                _productivityGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                _productivityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                Grid.SetColumn(_documentPreviewLabel, 0);
                Grid.SetRow(_documentPreviewLabel, 0);
                Grid.SetColumn(_documentPreview, 0);
                Grid.SetRow(_documentPreview, 1);
                _documentPreview.Margin = new Thickness(0);

                Grid.SetColumn(_clipboardPreviewLabel, 0);
                Grid.SetRow(_clipboardPreviewLabel, 2);
                Grid.SetColumn(_clipboardPreview, 0);
                Grid.SetRow(_clipboardPreview, 3);
                _clipboardPreview.Margin = new Thickness(0, 8, 0, 0);
            }
            else
            {
                _productivityGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                _productivityGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                _productivityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                _productivityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                Grid.SetColumn(_documentPreviewLabel, 0);
                Grid.SetRow(_documentPreviewLabel, 0);
                Grid.SetColumn(_documentPreview, 0);
                Grid.SetRow(_documentPreview, 1);
                _documentPreview.Margin = new Thickness(0);

                Grid.SetColumn(_clipboardPreviewLabel, 1);
                Grid.SetRow(_clipboardPreviewLabel, 0);
                Grid.SetColumn(_clipboardPreview, 1);
                Grid.SetRow(_clipboardPreview, 1);
                _clipboardPreview.Margin = new Thickness(0);
            }
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
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"WebView2 initialization failed: {ex}");
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

