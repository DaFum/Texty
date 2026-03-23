using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Texty.App.ViewModels;
using Texty.App.ViewModels.Models;
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
    private static readonly Windows.UI.Color InkStrong = Microsoft.UI.ColorHelper.FromArgb(255, 16, 43, 55);
    private static readonly Windows.UI.Color InkLabel = Microsoft.UI.ColorHelper.FromArgb(255, 21, 56, 72);
    private static readonly Windows.UI.Color InkMeta = Microsoft.UI.ColorHelper.FromArgb(255, 22, 61, 77);
    private static readonly Windows.UI.Color InkSubtle = Microsoft.UI.ColorHelper.FromArgb(255, 31, 72, 88);
    private static readonly Windows.UI.Color PanelDark = Microsoft.UI.ColorHelper.FromArgb(236, 19, 38, 52);
    private static readonly Windows.UI.Color PanelDarkBorder = Microsoft.UI.ColorHelper.FromArgb(110, 96, 169, 184);
    private static readonly Windows.UI.Color PanelLight = Microsoft.UI.ColorHelper.FromArgb(232, 246, 242, 234);
    private static readonly Windows.UI.Color PanelLightBorder = Microsoft.UI.ColorHelper.FromArgb(95, 110, 122, 131);
    private static readonly Windows.UI.Color EditorLight = Microsoft.UI.ColorHelper.FromArgb(232, 236, 245, 250);
    private static readonly Windows.UI.Color EditorLightBorder = Microsoft.UI.ColorHelper.FromArgb(90, 83, 123, 139);
    private static readonly Windows.UI.Color TriggerDark = Microsoft.UI.ColorHelper.FromArgb(236, 22, 40, 57);
    private static readonly Windows.UI.Color TriggerDarkBorder = Microsoft.UI.ColorHelper.FromArgb(112, 120, 202, 202);
    private static readonly Windows.UI.Color CommandSurface = Microsoft.UI.ColorHelper.FromArgb(228, 245, 250, 254);
    private static readonly Windows.UI.Color CommandSurfaceBorder = Microsoft.UI.ColorHelper.FromArgb(168, 68, 108, 124);
    private static readonly Windows.UI.Color InputPlaceholder = Microsoft.UI.ColorHelper.FromArgb(255, 49, 87, 103);

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
    private ListView? _triggerRuleList;
    private ListView? _versionList;
    private ListView? _trashList;
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

    private bool _hasRunRevealAnimation;

    private enum ButtonVisualTier
    {
        Primary,
        Secondary,
        Tertiary,
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
            Foreground = new SolidColorBrush(InkStrong),
            FontFamily = BodyFont,
            FontSize = 15,
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
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 43, 78, 92)),
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
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 236, 247, 250)),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8),
            CornerRadius = new CornerRadius(16),
        };
        _folderList.Resources["ListViewItemForeground"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 236, 247, 250));
        _folderList.Resources["ListViewItemForegroundSelected"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 246, 253, 255));
        _folderList.Resources["ListViewItemBackgroundSelected"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 73, 94, 109));
        _folderList.Resources["ListViewItemBackgroundSelectedPointerOver"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 80, 104, 120));
        _folderList.SetBinding(ItemsControl.ItemsSourceProperty, CreateBinding(nameof(MainViewModel.Folders)));
        _folderList.SetBinding(ListView.SelectedItemProperty, CreateBinding(nameof(MainViewModel.SelectedFolder), BindingMode.TwoWay));
        panel.Children.Add(_folderList);

        var folderEditor = new Border
        {
            Padding = new Thickness(10, 8, 10, 10),
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(114, 96, 173, 183)),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(60, 24, 74, 93)),
        };
        var folderEditorPanel = new StackPanel { Spacing = 6 };

        var folderNameInput = new TextBox
        {
            Header = "Ordnername",
            FontFamily = BodyFont,
            FontSize = 13,
            CornerRadius = new CornerRadius(8),
        };
        ApplyInputChrome(folderNameInput);
        folderNameInput.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.FolderNameInput), BindingMode.TwoWay));
        folderEditorPanel.Children.Add(folderNameInput);

        var folderColorInput = new TextBox
        {
            Header = "Farbe (#RRGGBB)",
            FontFamily = MonoFont,
            FontSize = 12,
            CornerRadius = new CornerRadius(8),
        };
        ApplyInputChrome(folderColorInput);
        folderColorInput.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.FolderColorHexInput), BindingMode.TwoWay));
        folderEditorPanel.Children.Add(folderColorInput);

        var folderButtonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
        };
        folderButtonRow.Children.Add(MakeCommandButton("Ordner +", nameof(MainViewModel.CreateFolderCommand), ButtonVisualTier.Primary));
        folderButtonRow.Children.Add(MakeCommandButton("Umben.", nameof(MainViewModel.RenameSelectedFolderCommand), ButtonVisualTier.Secondary));
        folderButtonRow.Children.Add(MakeCommandButton("Loeschen", nameof(MainViewModel.DeleteSelectedFolderCommand), ButtonVisualTier.Tertiary));
        folderEditorPanel.Children.Add(folderButtonRow);

        var folderSortRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
        };
        folderSortRow.Children.Add(MakeCommandButton("Nach oben", nameof(MainViewModel.MoveFolderUpCommand), ButtonVisualTier.Tertiary));
        folderSortRow.Children.Add(MakeCommandButton("Nach unten", nameof(MainViewModel.MoveFolderDownCommand), ButtonVisualTier.Tertiary));
        folderEditorPanel.Children.Add(folderSortRow);

        folderEditor.Child = folderEditorPanel;
        panel.Children.Add(folderEditor);

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
        var panel = new Grid
        {
            Padding = new Thickness(16, 16, 16, 16),
            RowSpacing = 8,
        };
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var title = new TextBlock
        {
            Text = "Library",
            FontFamily = DisplayFont,
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(InkStrong),
        };
        Grid.SetRow(title, 0);
        panel.Children.Add(title);

        var bar = (FrameworkElement)BuildLibraryCommandBar();
        Grid.SetRow(bar, 1);
        panel.Children.Add(bar);

        var searchLabel = CreateSectionLabel("Suche");
        Grid.SetRow(searchLabel, 2);
        panel.Children.Add(searchLabel);

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
        Grid.SetRow(searchFrame, 3);
        panel.Children.Add(searchFrame);

        var replaceLabel = CreateSectionLabel("Ersetzen (Scope: aktuelle Trefferliste)");
        Grid.SetRow(replaceLabel, 4);
        panel.Children.Add(replaceLabel);

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
        Grid.SetRow(replaceFrame, 5);
        panel.Children.Add(replaceFrame);

        var scopePanel = new Grid
        {
            ColumnSpacing = 8,
            RowSpacing = 6,
            Margin = new Thickness(0, 2, 0, 0),
        };
        scopePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        scopePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        scopePanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        scopePanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var tagScopeInput = new TextBox
        {
            PlaceholderText = "Scope Tag (optional)",
            FontFamily = BodyFont,
            FontSize = 13,
            CornerRadius = new CornerRadius(10),
        };
        ApplyInputChrome(tagScopeInput);
        tagScopeInput.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.ReplaceScopeTag), BindingMode.TwoWay));
        scopePanel.Children.Add(tagScopeInput);

        var processScopeInput = new TextBox
        {
            PlaceholderText = "Scope Zielprozess (optional)",
            FontFamily = BodyFont,
            FontSize = 13,
            CornerRadius = new CornerRadius(10),
        };
        ApplyInputChrome(processScopeInput);
        processScopeInput.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.ReplaceScopeTargetProcess), BindingMode.TwoWay));
        Grid.SetColumn(processScopeInput, 1);
        scopePanel.Children.Add(processScopeInput);

        var folderScopeToggle = new ToggleSwitch
        {
            Header = "Ordner-Scope",
            FontFamily = BodyFont,
            FontSize = 12,
        };
        folderScopeToggle.SetBinding(ToggleSwitch.IsOnProperty, CreateBinding(nameof(MainViewModel.ReplaceScopeUseFolder), BindingMode.TwoWay));
        Grid.SetRow(folderScopeToggle, 1);
        scopePanel.Children.Add(folderScopeToggle);

        var selectionScopeWrap = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var selectionScopeToggle = new ToggleSwitch
        {
            Header = "Auswahl-Scope",
            FontFamily = BodyFont,
            FontSize = 12,
        };
        selectionScopeToggle.SetBinding(ToggleSwitch.IsOnProperty, CreateBinding(nameof(MainViewModel.ReplaceScopeUseSelection), BindingMode.TwoWay));
        selectionScopeWrap.Children.Add(selectionScopeToggle);
        var includeHiddenToggle = new ToggleSwitch
        {
            Header = "Hidden einschliessen",
            FontFamily = BodyFont,
            FontSize = 12,
        };
        includeHiddenToggle.SetBinding(ToggleSwitch.IsOnProperty, CreateBinding(nameof(MainViewModel.ReplaceScopeIncludeHidden), BindingMode.TwoWay));
        selectionScopeWrap.Children.Add(includeHiddenToggle);
        Grid.SetColumn(selectionScopeWrap, 1);
        Grid.SetRow(selectionScopeWrap, 1);
        scopePanel.Children.Add(selectionScopeWrap);
        Grid.SetRow(scopePanel, 6);
        panel.Children.Add(scopePanel);

        var replaceResult = new TextBlock
        {
            FontFamily = BodyFont,
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(InkMeta),
            TextWrapping = TextWrapping.Wrap,
        };
        replaceResult.SetBinding(TextBlock.TextProperty, CreateBinding(nameof(MainViewModel.ReplaceResultText), BindingMode.OneWay));
        Grid.SetRow(replaceResult, 7);
        panel.Children.Add(replaceResult);

        var integrationExpander = new Expander
        {
            IsExpanded = false,
            Header = new TextBlock
            {
                Text = "Import & Resolver",
                FontFamily = BodyFont,
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(InkLabel),
            },
            BorderBrush = new SolidColorBrush(CommandSurfaceBorder),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(205, 244, 250, 253)),
            Content = BuildImportResolverPanel(),
        };
        Grid.SetRow(integrationExpander, 8);
        panel.Children.Add(integrationExpander);

        _snippetList = new ListView
        {
            DisplayMemberPath = "Title",
            MinHeight = 180,
            MaxHeight = 1200,
            SelectionMode = ListViewSelectionMode.Single,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8),
            CornerRadius = new CornerRadius(16),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(196, 245, 252, 255)),
            Foreground = new SolidColorBrush(InkStrong),
        };
        _snippetList.Resources["ListViewItemForeground"] = new SolidColorBrush(InkStrong);
        _snippetList.Resources["ListViewItemForegroundSelected"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 12, 42, 56));
        _snippetList.Resources["ListViewItemBackgroundSelected"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 210, 236, 246));
        _snippetList.Resources["ListViewItemBackgroundSelectedPointerOver"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 201, 230, 241));
        _snippetList.Resources["ListViewItemBackgroundPointerOver"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 226, 244, 250));
        _snippetList.SetBinding(ItemsControl.ItemsSourceProperty, CreateBinding(nameof(MainViewModel.VisibleSnippets)));
        _snippetList.SetBinding(ListView.SelectedItemProperty, CreateBinding(nameof(MainViewModel.SelectedSnippet), BindingMode.TwoWay));
        Grid.SetRow(_snippetList, 9);
        panel.Children.Add(_snippetList);

        AttachHoverMotion(_centerPaneCard, -3);
        return panel;
    }

    private UIElement BuildImportResolverPanel()
    {
        var panel = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0),
        };

        var importGrid = new Grid
        {
            ColumnSpacing = 8,
            RowSpacing = 6,
        };
        importGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        importGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        importGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        importGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        importGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var importPath = new TextBox
        {
            Header = "Import-Pfad",
            FontFamily = BodyFont,
            CornerRadius = new CornerRadius(10),
        };
        ApplyInputChrome(importPath);
        importPath.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.ImportSourcePath), BindingMode.TwoWay));
        importGrid.Children.Add(importPath);

        var importFormat = new ComboBox
        {
            Header = "Format",
            FontFamily = BodyFont,
        };
        importFormat.SetBinding(ItemsControl.ItemsSourceProperty, CreateBinding(nameof(MainViewModel.ImportFormatOptions), BindingMode.OneWay));
        importFormat.SetBinding(ComboBox.SelectedItemProperty, CreateBinding(nameof(MainViewModel.ImportFormat), BindingMode.TwoWay));
        Grid.SetColumn(importFormat, 1);
        importGrid.Children.Add(importFormat);

        var importButton = MakeCommandButton("Importieren", nameof(MainViewModel.ExecuteImportCommand), ButtonVisualTier.Secondary);
        importButton.VerticalAlignment = VerticalAlignment.Bottom;
        Grid.SetColumn(importButton, 2);
        importGrid.Children.Add(importButton);

        var importResult = new TextBlock
        {
            FontFamily = BodyFont,
            FontSize = 12,
            Foreground = new SolidColorBrush(InkMeta),
            TextWrapping = TextWrapping.Wrap,
        };
        importResult.SetBinding(TextBlock.TextProperty, CreateBinding(nameof(MainViewModel.ImportResultText), BindingMode.OneWay));
        Grid.SetRow(importResult, 1);
        Grid.SetColumnSpan(importResult, 3);
        importGrid.Children.Add(importResult);

        panel.Children.Add(importGrid);

        var resolverGrid = new Grid
        {
            ColumnSpacing = 8,
            RowSpacing = 6,
        };
        resolverGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        resolverGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        resolverGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        resolverGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        resolverGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var resolverName = new ComboBox
        {
            Header = "Resolver",
            FontFamily = BodyFont,
        };
        resolverName.SetBinding(ItemsControl.ItemsSourceProperty, CreateBinding(nameof(MainViewModel.ResolverOptions), BindingMode.OneWay));
        resolverName.SetBinding(ComboBox.SelectedItemProperty, CreateBinding(nameof(MainViewModel.ResolverName), BindingMode.TwoWay));
        resolverGrid.Children.Add(resolverName);

        var resolverExpression = new TextBox
        {
            Header = "Ausdruck",
            FontFamily = BodyFont,
            CornerRadius = new CornerRadius(10),
        };
        ApplyInputChrome(resolverExpression);
        resolverExpression.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.ResolverExpression), BindingMode.TwoWay));
        Grid.SetColumn(resolverExpression, 1);
        resolverGrid.Children.Add(resolverExpression);

        var resolverButton = MakeCommandButton("Aufloesen", nameof(MainViewModel.ResolveExternalValueCommand), ButtonVisualTier.Secondary);
        resolverButton.VerticalAlignment = VerticalAlignment.Bottom;
        Grid.SetColumn(resolverButton, 2);
        resolverGrid.Children.Add(resolverButton);

        var resolverResult = new TextBox
        {
            Header = "Resolver-Ergebnis",
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 56,
            FontFamily = MonoFont,
            CornerRadius = new CornerRadius(10),
        };
        ApplyInputChrome(resolverResult);
        resolverResult.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.ResolverResultText), BindingMode.OneWay));
        Grid.SetRow(resolverResult, 1);
        Grid.SetColumnSpan(resolverResult, 3);
        resolverGrid.Children.Add(resolverResult);

        panel.Children.Add(resolverGrid);
        return panel;
    }

    private UIElement BuildTemplateDesignerPanel()
    {
        var root = new Grid
        {
            Margin = new Thickness(0, 8, 0, 0),
            ColumnSpacing = 10,
            RowSpacing = 8,
        };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var fieldList = new ListView
        {
            DisplayMemberPath = nameof(TemplateFieldDesignerItemModel.Display),
            MinHeight = 160,
            MaxHeight = 280,
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(188, 247, 253, 255)),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(108, 84, 126, 141)),
            BorderThickness = new Thickness(1),
            Foreground = new SolidColorBrush(InkStrong),
            Padding = new Thickness(4),
        };
        fieldList.SetBinding(ItemsControl.ItemsSourceProperty, CreateBinding(nameof(MainViewModel.TemplateFields), BindingMode.OneWay));
        fieldList.SetBinding(ListView.SelectedItemProperty, CreateBinding(nameof(MainViewModel.SelectedTemplateField), BindingMode.TwoWay));
        root.Children.Add(fieldList);

        var editorPanel = new StackPanel { Spacing = 6 };
        Grid.SetColumn(editorPanel, 1);

        var modeText = new TextBlock
        {
            FontFamily = BodyFont,
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(InkMeta),
        };
        modeText.SetBinding(TextBlock.TextProperty, CreateBinding(nameof(MainViewModel.TemplateDesignerMode), BindingMode.OneWay));
        editorPanel.Children.Add(modeText);

        var keyLabelGrid = new Grid { ColumnSpacing = 8 };
        keyLabelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        keyLabelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var keyInput = new TextBox { Header = "Key", FontFamily = MonoFont, CornerRadius = new CornerRadius(8) };
        ApplyInputChrome(keyInput);
        keyInput.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.TemplateFieldKeyInput), BindingMode.TwoWay));
        keyLabelGrid.Children.Add(keyInput);
        var labelInput = new TextBox { Header = "Label", FontFamily = BodyFont, CornerRadius = new CornerRadius(8) };
        ApplyInputChrome(labelInput);
        labelInput.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.TemplateFieldLabelInput), BindingMode.TwoWay));
        Grid.SetColumn(labelInput, 1);
        keyLabelGrid.Children.Add(labelInput);
        editorPanel.Children.Add(keyLabelGrid);

        var typeRequiredGrid = new Grid { ColumnSpacing = 8 };
        typeRequiredGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        typeRequiredGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var typeCombo = new ComboBox { Header = "Typ", FontFamily = BodyFont };
        typeCombo.SetBinding(ItemsControl.ItemsSourceProperty, CreateBinding(nameof(MainViewModel.TemplateFieldTypeOptions), BindingMode.OneWay));
        typeCombo.SetBinding(ComboBox.SelectedItemProperty, CreateBinding(nameof(MainViewModel.TemplateFieldTypeInput), BindingMode.TwoWay));
        typeRequiredGrid.Children.Add(typeCombo);
        var requiredToggle = new ToggleSwitch { Header = "Required", FontFamily = BodyFont, FontSize = 12 };
        requiredToggle.SetBinding(ToggleSwitch.IsOnProperty, CreateBinding(nameof(MainViewModel.TemplateFieldRequiredInput), BindingMode.TwoWay));
        Grid.SetColumn(requiredToggle, 1);
        typeRequiredGrid.Children.Add(requiredToggle);
        editorPanel.Children.Add(typeRequiredGrid);

        var placeholderInput = new TextBox { Header = "Placeholder", FontFamily = BodyFont, CornerRadius = new CornerRadius(8) };
        ApplyInputChrome(placeholderInput);
        placeholderInput.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.TemplateFieldPlaceholderInput), BindingMode.TwoWay));
        editorPanel.Children.Add(placeholderInput);

        var minMaxDefaultGrid = new Grid { ColumnSpacing = 8 };
        minMaxDefaultGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        minMaxDefaultGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        minMaxDefaultGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var minInput = new TextBox { Header = "Min", FontFamily = MonoFont, CornerRadius = new CornerRadius(8) };
        ApplyInputChrome(minInput);
        minInput.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.TemplateFieldMinInput), BindingMode.TwoWay));
        minMaxDefaultGrid.Children.Add(minInput);
        var maxInput = new TextBox { Header = "Max", FontFamily = MonoFont, CornerRadius = new CornerRadius(8) };
        ApplyInputChrome(maxInput);
        maxInput.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.TemplateFieldMaxInput), BindingMode.TwoWay));
        Grid.SetColumn(maxInput, 1);
        minMaxDefaultGrid.Children.Add(maxInput);
        var defaultInput = new TextBox { Header = "Default", FontFamily = BodyFont, CornerRadius = new CornerRadius(8) };
        ApplyInputChrome(defaultInput);
        defaultInput.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.TemplateFieldDefaultValueInput), BindingMode.TwoWay));
        Grid.SetColumn(defaultInput, 2);
        minMaxDefaultGrid.Children.Add(defaultInput);
        editorPanel.Children.Add(minMaxDefaultGrid);

        var optionsInput = new TextBox
        {
            Header = "Options (key:label; key2:label2)",
            FontFamily = MonoFont,
            FontSize = 12,
            CornerRadius = new CornerRadius(8),
        };
        ApplyInputChrome(optionsInput);
        optionsInput.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.TemplateFieldOptionsInput), BindingMode.TwoWay));
        editorPanel.Children.Add(optionsInput);

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
        };
        buttonRow.Children.Add(MakeCommandButton("Neu", nameof(MainViewModel.NewTemplateFieldCommand), ButtonVisualTier.Tertiary));
        buttonRow.Children.Add(MakeCommandButton("Feld speichern", nameof(MainViewModel.SaveTemplateFieldCommand), ButtonVisualTier.Secondary));
        buttonRow.Children.Add(MakeCommandButton("Feld loeschen", nameof(MainViewModel.DeleteSelectedTemplateFieldCommand), ButtonVisualTier.Tertiary));
        buttonRow.Children.Add(MakeCommandButton("Template speichern", nameof(MainViewModel.SaveTemplateDesignCommand), ButtonVisualTier.Primary));
        editorPanel.Children.Add(buttonRow);

        root.Children.Add(editorPanel);

        var hint = new TextBlock
        {
            Text = "Definiere Formularfelder visuell und speichere sie direkt im Snippet-Template.",
            FontFamily = BodyFont,
            FontSize = 12,
            Foreground = new SolidColorBrush(InkMeta),
            TextWrapping = TextWrapping.Wrap,
        };
        Grid.SetColumnSpan(hint, 2);
        Grid.SetRow(hint, 1);
        root.Children.Add(hint);

        return root;
    }

    private UIElement BuildAiStudioPanel()
    {
        var panel = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0),
        };

        var providerRow = new Grid { ColumnSpacing = 8 };
        providerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        providerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var providerCombo = new ComboBox { Header = "Provider", FontFamily = BodyFont };
        providerCombo.SetBinding(ItemsControl.ItemsSourceProperty, CreateBinding(nameof(MainViewModel.AiProviderOptions), BindingMode.OneWay));
        providerCombo.SetBinding(ComboBox.SelectedItemProperty, CreateBinding(nameof(MainViewModel.SelectedAiProvider), BindingMode.TwoWay));
        providerRow.Children.Add(providerCombo);

        var modelInput = new TextBox { Header = "Model (optional)", FontFamily = BodyFont, CornerRadius = new CornerRadius(8) };
        ApplyInputChrome(modelInput);
        modelInput.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.AiModelInput), BindingMode.TwoWay));
        Grid.SetColumn(modelInput, 1);
        providerRow.Children.Add(modelInput);
        panel.Children.Add(providerRow);

        var promptInput = new TextBox
        {
            Header = "Prompt / Rewrite-Instruktion",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 80,
            FontFamily = BodyFont,
            CornerRadius = new CornerRadius(8),
        };
        ScrollViewer.SetVerticalScrollBarVisibility(promptInput, ScrollBarVisibility.Auto);
        ApplyInputChrome(promptInput);
        promptInput.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.AiPromptInput), BindingMode.TwoWay));
        panel.Children.Add(promptInput);

        var translationRow = new Grid { ColumnSpacing = 8 };
        translationRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        translationRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var sourceLang = new TextBox { Header = "Quelle", FontFamily = MonoFont, CornerRadius = new CornerRadius(8) };
        ApplyInputChrome(sourceLang);
        sourceLang.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.TranslationSourceLanguageInput), BindingMode.TwoWay));
        translationRow.Children.Add(sourceLang);

        var targetLang = new TextBox { Header = "Ziel", FontFamily = MonoFont, CornerRadius = new CornerRadius(8) };
        ApplyInputChrome(targetLang);
        targetLang.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.TranslationTargetLanguageInput), BindingMode.TwoWay));
        Grid.SetColumn(targetLang, 1);
        translationRow.Children.Add(targetLang);
        panel.Children.Add(translationRow);

        var actionRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        actionRow.Children.Add(MakeCommandButton("Generieren", nameof(MainViewModel.GenerateWithAiCommand), ButtonVisualTier.Primary));
        actionRow.Children.Add(MakeCommandButton("Umformulieren", nameof(MainViewModel.RewriteWithAiCommand), ButtonVisualTier.Secondary));
        actionRow.Children.Add(MakeCommandButton("Uebersetzen", nameof(MainViewModel.TranslateWithAiCommand), ButtonVisualTier.Secondary));
        actionRow.Children.Add(MakeCommandButton("Health", nameof(MainViewModel.RefreshAiProviderHealthCommand), ButtonVisualTier.Tertiary));
        actionRow.Children.Add(MakeCommandButton("Uebernehmen", nameof(MainViewModel.ApplyAiResultToEditorCommand), ButtonVisualTier.Tertiary));
        panel.Children.Add(actionRow);

        var resultBox = new TextBox
        {
            Header = "KI-Ergebnis",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 100,
            FontFamily = BodyFont,
            CornerRadius = new CornerRadius(8),
        };
        ScrollViewer.SetVerticalScrollBarVisibility(resultBox, ScrollBarVisibility.Auto);
        ApplyInputChrome(resultBox);
        resultBox.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.AiResultText), BindingMode.TwoWay));
        panel.Children.Add(resultBox);

        var healthBox = new TextBox
        {
            Header = "Provider Health",
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 72,
            FontFamily = MonoFont,
            CornerRadius = new CornerRadius(8),
        };
        ScrollViewer.SetVerticalScrollBarVisibility(healthBox, ScrollBarVisibility.Auto);
        ApplyInputChrome(healthBox);
        healthBox.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.AiHealthReportText), BindingMode.OneWay));
        panel.Children.Add(healthBox);

        return panel;
    }

    private UIElement BuildMacroStudioPanel()
    {
        var panel = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0),
        };

        var scriptBox = new TextBox
        {
            Header = "DSL Skript",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 110,
            FontFamily = MonoFont,
            FontSize = 12,
            CornerRadius = new CornerRadius(8),
        };
        ScrollViewer.SetVerticalScrollBarVisibility(scriptBox, ScrollBarVisibility.Auto);
        ApplyInputChrome(scriptBox);
        scriptBox.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.MacroScriptInput), BindingMode.TwoWay));
        panel.Children.Add(scriptBox);

        var varsBox = new TextBox
        {
            Header = "Variablen (key=value pro Zeile oder JSON)",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 70,
            FontFamily = MonoFont,
            FontSize = 12,
            CornerRadius = new CornerRadius(8),
        };
        ScrollViewer.SetVerticalScrollBarVisibility(varsBox, ScrollBarVisibility.Auto);
        ApplyInputChrome(varsBox);
        varsBox.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.MacroVariablesInput), BindingMode.TwoWay));
        panel.Children.Add(varsBox);

        var policyRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        policyRow.Children.Add(CreateMacroPolicyToggle("Process", nameof(MainViewModel.MacroAllowProcessStart)));
        policyRow.Children.Add(CreateMacroPolicyToggle("FileWrite", nameof(MainViewModel.MacroAllowFileSystemWrite)));
        policyRow.Children.Add(CreateMacroPolicyToggle("External", nameof(MainViewModel.MacroAllowExternalOpen)));
        policyRow.Children.Add(CreateMacroPolicyToggle("Notify", nameof(MainViewModel.MacroAllowNotifications)));
        policyRow.Children.Add(CreateMacroPolicyToggle("PowerShell", nameof(MainViewModel.MacroAllowPowerShell)));
        panel.Children.Add(policyRow);

        var policySummary = new TextBlock
        {
            FontFamily = BodyFont,
            FontSize = 12,
            Foreground = new SolidColorBrush(InkMeta),
            TextWrapping = TextWrapping.Wrap,
        };
        policySummary.SetBinding(TextBlock.TextProperty, CreateBinding(nameof(MainViewModel.MacroPolicySummaryText), BindingMode.OneWay));
        panel.Children.Add(policySummary);

        var runRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        runRow.Children.Add(MakeCommandButton("Makro ausfuehren", nameof(MainViewModel.ExecuteMacroScriptCommand), ButtonVisualTier.Primary));
        panel.Children.Add(runRow);

        var outputBox = new TextBox
        {
            Header = "Output",
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 70,
            FontFamily = MonoFont,
            FontSize = 12,
            CornerRadius = new CornerRadius(8),
        };
        ScrollViewer.SetVerticalScrollBarVisibility(outputBox, ScrollBarVisibility.Auto);
        ApplyInputChrome(outputBox);
        outputBox.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.MacroOutputText), BindingMode.OneWay));
        panel.Children.Add(outputBox);

        var errorBox = new TextBox
        {
            Header = "Fehler",
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 60,
            FontFamily = MonoFont,
            FontSize = 12,
            CornerRadius = new CornerRadius(8),
        };
        ScrollViewer.SetVerticalScrollBarVisibility(errorBox, ScrollBarVisibility.Auto);
        ApplyInputChrome(errorBox);
        errorBox.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.MacroErrorsText), BindingMode.OneWay));
        panel.Children.Add(errorBox);

        var auditBox = new TextBox
        {
            Header = "Audit Trail",
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 80,
            FontFamily = MonoFont,
            FontSize = 12,
            CornerRadius = new CornerRadius(8),
        };
        ScrollViewer.SetVerticalScrollBarVisibility(auditBox, ScrollBarVisibility.Auto);
        ApplyInputChrome(auditBox);
        auditBox.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.MacroAuditText), BindingMode.OneWay));
        panel.Children.Add(auditBox);

        return panel;
    }

    private UIElement BuildEditorPane()
    {
        var panel = new StackPanel
        {
            Spacing = 6,
            Padding = new Thickness(16, 16, 16, 16),
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
            Header = new TextBlock
            {
                Text = "Einfuegen-Tools (Placeholder, Tabelle, Bild)",
                FontFamily = BodyFont,
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(InkLabel),
            },
            IsExpanded = false,
            Margin = new Thickness(0, 2, 0, 2),
            BorderBrush = new SolidColorBrush(CommandSurfaceBorder),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(208, 239, 247, 252)),
            Content = new Border
            {
                Margin = new Thickness(0, 6, 0, 0),
                Padding = new Thickness(8, 6, 8, 8),
                CornerRadius = new CornerRadius(10),
                BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(110, 89, 128, 144)),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(170, 249, 253, 255)),
                Child = BuildInsertToolsPanel(),
            },
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

        var templateDesignerExpander = new Expander
        {
            IsExpanded = false,
            Header = new TextBlock
            {
                Text = "Template-Designer",
                FontFamily = BodyFont,
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(InkLabel),
            },
            BorderBrush = new SolidColorBrush(CommandSurfaceBorder),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(205, 244, 250, 253)),
            Content = BuildTemplateDesignerPanel(),
        };
        panel.Children.Add(templateDesignerExpander);

        var aiExpander = new Expander
        {
            IsExpanded = false,
            Header = new TextBlock
            {
                Text = "KI Studio (Generate / Rewrite / Translate)",
                FontFamily = BodyFont,
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(InkLabel),
            },
            BorderBrush = new SolidColorBrush(CommandSurfaceBorder),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(208, 239, 247, 252)),
            Content = BuildAiStudioPanel(),
        };
        panel.Children.Add(aiExpander);

        var macroExpander = new Expander
        {
            IsExpanded = false,
            Header = new TextBlock
            {
                Text = "Makro / DSL Studio",
                FontFamily = BodyFont,
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(InkLabel),
            },
            BorderBrush = new SolidColorBrush(CommandSurfaceBorder),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(208, 239, 247, 252)),
            Content = BuildMacroStudioPanel(),
        };
        panel.Children.Add(macroExpander);

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
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(InkLabel),
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
            FontSize = 15,
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
            FontSize = 15,
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
            RowSpacing = 8,
        };
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

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

        _triggerRuleList = new ListView
        {
            MinHeight = 120,
            MaxHeight = 260,
            SelectionMode = ListViewSelectionMode.Single,
            DisplayMemberPath = nameof(TriggerRuleItemModel.Display),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(46, 196, 231, 238)),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(95, 96, 152, 165)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(6),
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 229, 246, 250)),
        };
        _triggerRuleList.Resources["ListViewItemForeground"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 227, 246, 250));
        _triggerRuleList.Resources["ListViewItemForegroundSelected"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 241, 252, 255));
        _triggerRuleList.Resources["ListViewItemBackgroundSelected"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 56, 98, 116));
        _triggerRuleList.SetBinding(ItemsControl.ItemsSourceProperty, CreateBinding(nameof(MainViewModel.TriggerRules), BindingMode.OneWay));
        _triggerRuleList.SetBinding(ListView.SelectedItemProperty, CreateBinding(nameof(MainViewModel.SelectedTriggerRule), BindingMode.TwoWay));
        Grid.SetRow(_triggerRuleList, 2);
        panel.Children.Add(_triggerRuleList);

        var triggerEditorBorder = new Border
        {
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(110, 88, 144, 158)),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(72, 28, 63, 83)),
            Padding = new Thickness(10, 8, 10, 10),
        };
        var triggerEditor = new Grid
        {
            ColumnSpacing = 8,
            RowSpacing = 6,
        };
        triggerEditor.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        triggerEditor.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        triggerEditor.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        triggerEditor.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        triggerEditor.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        triggerEditor.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        triggerEditor.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var modeText = new TextBlock
        {
            FontFamily = BodyFont,
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 213, 239, 244)),
        };
        modeText.SetBinding(TextBlock.TextProperty, CreateBinding(nameof(MainViewModel.TriggerEditorMode), BindingMode.OneWay));
        Grid.SetColumnSpan(modeText, 3);
        triggerEditor.Children.Add(modeText);

        var typeCombo = new ComboBox
        {
            Header = "Typ",
            FontFamily = BodyFont,
        };
        typeCombo.SetBinding(ItemsControl.ItemsSourceProperty, CreateBinding(nameof(MainViewModel.TriggerTypeOptions), BindingMode.OneWay));
        typeCombo.SetBinding(ComboBox.SelectedItemProperty, CreateBinding(nameof(MainViewModel.TriggerTypeInput), BindingMode.TwoWay));
        Grid.SetRow(typeCombo, 1);
        triggerEditor.Children.Add(typeCombo);

        var patternInput = new TextBox
        {
            Header = "Pattern",
            FontFamily = BodyFont,
            CornerRadius = new CornerRadius(8),
        };
        ApplyInputChrome(patternInput);
        patternInput.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.TriggerPatternInput), BindingMode.TwoWay));
        Grid.SetRow(patternInput, 1);
        Grid.SetColumn(patternInput, 1);
        triggerEditor.Children.Add(patternInput);

        var saveTriggerButton = MakeCommandButton("Speichern", nameof(MainViewModel.SaveTriggerRuleCommand), ButtonVisualTier.Secondary);
        saveTriggerButton.VerticalAlignment = VerticalAlignment.Bottom;
        Grid.SetRow(saveTriggerButton, 1);
        Grid.SetColumn(saveTriggerButton, 2);
        triggerEditor.Children.Add(saveTriggerButton);

        var scopeCombo = new ComboBox
        {
            Header = "Scope",
            FontFamily = BodyFont,
        };
        scopeCombo.SetBinding(ItemsControl.ItemsSourceProperty, CreateBinding(nameof(MainViewModel.TriggerScopeOptions), BindingMode.OneWay));
        scopeCombo.SetBinding(ComboBox.SelectedItemProperty, CreateBinding(nameof(MainViewModel.TriggerScopeInput), BindingMode.TwoWay));
        Grid.SetRow(scopeCombo, 2);
        triggerEditor.Children.Add(scopeCombo);

        var processInput = new TextBox
        {
            Header = "TargetProcess (optional)",
            FontFamily = BodyFont,
            CornerRadius = new CornerRadius(8),
        };
        ApplyInputChrome(processInput);
        processInput.SetBinding(TextBox.TextProperty, CreateBinding(nameof(MainViewModel.TriggerTargetProcessInput), BindingMode.TwoWay));
        Grid.SetRow(processInput, 2);
        Grid.SetColumn(processInput, 1);
        triggerEditor.Children.Add(processInput);

        var newTriggerButton = MakeCommandButton("Neu", nameof(MainViewModel.NewTriggerRuleCommand), ButtonVisualTier.Tertiary);
        newTriggerButton.VerticalAlignment = VerticalAlignment.Bottom;
        Grid.SetRow(newTriggerButton, 2);
        Grid.SetColumn(newTriggerButton, 2);
        triggerEditor.Children.Add(newTriggerButton);

        var flagsWrap = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
        };
        var caseToggle = new ToggleSwitch
        {
            Header = "Case Sensitive",
            FontFamily = BodyFont,
            FontSize = 12,
        };
        caseToggle.SetBinding(ToggleSwitch.IsOnProperty, CreateBinding(nameof(MainViewModel.TriggerCaseSensitiveInput), BindingMode.TwoWay));
        flagsWrap.Children.Add(caseToggle);
        var enabledToggle = new ToggleSwitch
        {
            Header = "Aktiv",
            FontFamily = BodyFont,
            FontSize = 12,
        };
        enabledToggle.SetBinding(ToggleSwitch.IsOnProperty, CreateBinding(nameof(MainViewModel.TriggerEnabledInput), BindingMode.TwoWay));
        flagsWrap.Children.Add(enabledToggle);
        Grid.SetRow(flagsWrap, 3);
        Grid.SetColumnSpan(flagsWrap, 2);
        triggerEditor.Children.Add(flagsWrap);

        var deleteTriggerButton = MakeCommandButton("Loeschen", nameof(MainViewModel.DeleteSelectedTriggerRuleCommand), ButtonVisualTier.Tertiary);
        deleteTriggerButton.VerticalAlignment = VerticalAlignment.Bottom;
        Grid.SetRow(deleteTriggerButton, 3);
        Grid.SetColumn(deleteTriggerButton, 2);
        triggerEditor.Children.Add(deleteTriggerButton);

        triggerEditorBorder.Child = triggerEditor;
        Grid.SetRow(triggerEditorBorder, 3);
        panel.Children.Add(triggerEditorBorder);

        var recoveryGrid = new Grid
        {
            ColumnSpacing = 8,
            RowSpacing = 6,
        };
        recoveryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        recoveryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        recoveryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        recoveryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        recoveryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var versionLabel = new TextBlock
        {
            Text = "Versionen",
            FontFamily = BodyFont,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 226, 245, 249)),
        };
        recoveryGrid.Children.Add(versionLabel);

        var trashLabel = new TextBlock
        {
            Text = "Papierkorb",
            FontFamily = BodyFont,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 226, 245, 249)),
        };
        Grid.SetColumn(trashLabel, 1);
        recoveryGrid.Children.Add(trashLabel);

        _versionList = new ListView
        {
            MinHeight = 110,
            MaxHeight = 190,
            DisplayMemberPath = nameof(VersionEntryItemModel.Display),
            SelectionMode = ListViewSelectionMode.Single,
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(44, 190, 226, 236)),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(95, 96, 152, 165)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 230, 247, 251)),
        };
        _versionList.SetBinding(ItemsControl.ItemsSourceProperty, CreateBinding(nameof(MainViewModel.VersionEntries), BindingMode.OneWay));
        _versionList.SetBinding(ListView.SelectedItemProperty, CreateBinding(nameof(MainViewModel.SelectedVersionEntry), BindingMode.TwoWay));
        Grid.SetRow(_versionList, 1);
        recoveryGrid.Children.Add(_versionList);

        _trashList = new ListView
        {
            MinHeight = 110,
            MaxHeight = 190,
            DisplayMemberPath = nameof(TrashEntryItemModel.Display),
            SelectionMode = ListViewSelectionMode.Single,
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(44, 190, 226, 236)),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(95, 96, 152, 165)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 230, 247, 251)),
        };
        _trashList.SetBinding(ItemsControl.ItemsSourceProperty, CreateBinding(nameof(MainViewModel.TrashEntries), BindingMode.OneWay));
        _trashList.SetBinding(ListView.SelectedItemProperty, CreateBinding(nameof(MainViewModel.SelectedTrashEntry), BindingMode.TwoWay));
        Grid.SetColumn(_trashList, 1);
        Grid.SetRow(_trashList, 1);
        recoveryGrid.Children.Add(_trashList);

        var versionButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
        };
        versionButtons.Children.Add(MakeCommandButton("Laden", nameof(MainViewModel.LoadVersionsCommand), ButtonVisualTier.Tertiary));
        versionButtons.Children.Add(MakeCommandButton("Rollback", nameof(MainViewModel.RollbackToSelectedVersionCommand), ButtonVisualTier.Secondary));
        Grid.SetRow(versionButtons, 2);
        recoveryGrid.Children.Add(versionButtons);

        var trashButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        trashButtons.Children.Add(MakeCommandButton("Refresh", nameof(MainViewModel.LoadTrashEntriesCommand), ButtonVisualTier.Tertiary));
        trashButtons.Children.Add(MakeCommandButton("Restore", nameof(MainViewModel.RestoreLatestTrashCommand), ButtonVisualTier.Secondary));
        trashButtons.Children.Add(MakeCommandButton("Hard Delete", nameof(MainViewModel.DeleteSelectedTrashEntryCommand), ButtonVisualTier.Tertiary));
        Grid.SetColumn(trashButtons, 1);
        Grid.SetRow(trashButtons, 2);
        recoveryGrid.Children.Add(trashButtons);

        Grid.SetRow(recoveryGrid, 4);
        panel.Children.Add(recoveryGrid);

        AttachHoverMotion(_triggerCard, -3);
        return panel;
    }

    private UIElement BuildLibraryCommandBar()
    {
        var bar = CreateCommandBar();

        bar.PrimaryCommands.Add(MakeCommandBarCommandButton("Suchen", nameof(MainViewModel.SearchCommand), Symbol.Find, ButtonVisualTier.Primary));
        bar.PrimaryCommands.Add(MakeCommandBarCommandButton("Ersetzen", nameof(MainViewModel.SearchReplaceVisibleCommand), Symbol.Switch, ButtonVisualTier.Secondary));
        bar.PrimaryCommands.Add(MakeCommandBarCommandButton("Duplikate", nameof(MainViewModel.RemoveDuplicatesCommand), Symbol.Filter, ButtonVisualTier.Secondary));

        bar.SecondaryCommands.Add(MakeCommandBarCommandButton("Duplizieren", nameof(MainViewModel.DuplicateSnippetCommand), Symbol.Copy, ButtonVisualTier.Secondary));
        bar.SecondaryCommands.Add(MakeCommandBarCommandButton("Verschieben", nameof(MainViewModel.MoveSnippetToSelectedFolderCommand), Symbol.Forward, ButtonVisualTier.Tertiary));
        bar.SecondaryCommands.Add(MakeCommandBarCommandButton("Bulk Font", nameof(MainViewModel.ApplyBulkFontCommand), Symbol.FontColor, ButtonVisualTier.Tertiary));
        bar.SecondaryCommands.Add(MakeCommandBarCommandButton("Hervorheben", nameof(MainViewModel.ToggleHighlightCommand), Symbol.Highlight, ButtonVisualTier.Tertiary));
        bar.SecondaryCommands.Add(MakeCommandBarCommandButton("Ausblenden", nameof(MainViewModel.ToggleHiddenCommand), Symbol.HideBcc, ButtonVisualTier.Tertiary));

        return bar;
    }

    private UIElement BuildEditorWorkflowCommandBar()
    {
        var bar = CreateCommandBar();

        bar.PrimaryCommands.Add(MakeCommandBarActionButton("Einfuegen", OnInsertClicked, Symbol.Paste, ButtonVisualTier.Primary));
        bar.PrimaryCommands.Add(MakeCommandBarCommandButton("Speichern", nameof(MainViewModel.SaveSnippetCommand), Symbol.Save, ButtonVisualTier.Secondary));
        bar.PrimaryCommands.Add(MakeCommandBarCommandButton("Simulieren", nameof(MainViewModel.SimulateInsertCommand), Symbol.Play, ButtonVisualTier.Secondary));

        bar.SecondaryCommands.Add(MakeCommandBarCommandButton("Versionen", nameof(MainViewModel.LoadVersionsCommand), Symbol.Clock, ButtonVisualTier.Secondary));
        bar.SecondaryCommands.Add(MakeCommandBarCommandButton("Rollback", nameof(MainViewModel.RollbackToPreviousVersionCommand), Symbol.Undo, ButtonVisualTier.Secondary));
        bar.SecondaryCommands.Add(MakeCommandBarCommandButton("Papierkorb", nameof(MainViewModel.MoveToTrashCommand), Symbol.Delete, ButtonVisualTier.Tertiary));
        bar.SecondaryCommands.Add(MakeCommandBarCommandButton("Aus Trash", nameof(MainViewModel.RestoreLatestTrashCommand), Symbol.Repair, ButtonVisualTier.Tertiary));
        bar.SecondaryCommands.Add(MakeCommandBarCommandButton("Trash leeren", nameof(MainViewModel.PurgeTrashCommand), Symbol.Clear, ButtonVisualTier.Tertiary));
        bar.SecondaryCommands.Add(MakeCommandBarCommandButton("Dokument", nameof(MainViewModel.GenerateDocumentCommand), Symbol.Document, ButtonVisualTier.Tertiary));
        bar.SecondaryCommands.Add(MakeCommandBarCommandButton("Korrektur", nameof(MainViewModel.ApplyTextCorrectionsCommand), Symbol.Edit, ButtonVisualTier.Tertiary));
        bar.SecondaryCommands.Add(MakeCommandBarCommandButton("Clip Verlauf", nameof(MainViewModel.LoadClipboardHistoryCommand), Symbol.Paste, ButtonVisualTier.Tertiary));
        bar.SecondaryCommands.Add(MakeCommandBarCommandButton("Clip laden", nameof(MainViewModel.InsertLatestClipboardHistoryCommand), Symbol.Paste, ButtonVisualTier.Tertiary));

        return bar;
    }

    private UIElement BuildFormattingCommandBar()
    {
        var bar = CreateCommandBar();

        bar.PrimaryCommands.Add(MakeCommandBarActionButton("Fett", OnFormatBoldClicked, Symbol.Bold, ButtonVisualTier.Primary));
        bar.PrimaryCommands.Add(MakeCommandBarActionButton("Kursiv", OnFormatItalicClicked, Symbol.Italic, ButtonVisualTier.Secondary));
        bar.SecondaryCommands.Add(MakeCommandBarActionButton("Unterstrichen", OnFormatUnderlineClicked, Symbol.Underline, ButtonVisualTier.Secondary));
        bar.SecondaryCommands.Add(MakeCommandBarActionButton("Liste", OnBulletListClicked, Symbol.List, ButtonVisualTier.Tertiary));
        bar.SecondaryCommands.Add(MakeCommandBarActionButton("Nummeriert", OnNumberedListClicked, Symbol.AllApps, ButtonVisualTier.Tertiary));

        return bar;
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

    private static CommandBar CreateCommandBar()
    {
        var bar = new CommandBar
        {
            IsDynamicOverflowEnabled = true,
            OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Auto,
            DefaultLabelPosition = CommandBarDefaultLabelPosition.Right,
            RequestedTheme = ElementTheme.Light,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(CommandSurfaceBorder),
            Background = new SolidColorBrush(CommandSurface),
            Foreground = new SolidColorBrush(InkStrong),
            Margin = new Thickness(0, 0, 0, 2),
            Padding = new Thickness(2),
        };

        void ApplySurface()
        {
            bar.Background = new SolidColorBrush(CommandSurface);
            bar.BorderBrush = new SolidColorBrush(CommandSurfaceBorder);
            bar.Foreground = new SolidColorBrush(InkStrong);
        }

        bar.Opened += (_, _) => ApplySurface();
        bar.Closed += (_, _) => ApplySurface();
        ApplySurface();

        bar.Resources["CommandBarBackground"] = new SolidColorBrush(CommandSurface);
        bar.Resources["CommandBarBackgroundOpen"] = new SolidColorBrush(CommandSurface);
        bar.Resources["CommandBarBorderBrush"] = new SolidColorBrush(CommandSurfaceBorder);
        bar.Resources["CommandBarBorderBrushOpen"] = new SolidColorBrush(CommandSurfaceBorder);
        bar.Resources["CommandBarForeground"] = new SolidColorBrush(InkStrong);
        bar.Resources["AppBarBackground"] = new SolidColorBrush(CommandSurface);
        bar.Resources["AppBarBackgroundThemeBrush"] = new SolidColorBrush(CommandSurface);
        bar.Resources["CommandBarOverflowPresenterForeground"] = new SolidColorBrush(InkStrong);
        bar.Resources["CommandBarOverflowPresenterBackground"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 240, 247, 252));
        bar.Resources["CommandBarFlyoutBackground"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 240, 247, 252));
        bar.Resources["CommandBarFlyoutForeground"] = new SolidColorBrush(InkStrong);
        return bar;
    }

    private static AppBarButton MakeCommandBarCommandButton(string label, string commandPath, Symbol symbol, ButtonVisualTier tier)
    {
        var button = BuildCommandBarButton(label, symbol, tier);
        button.SetBinding(AppBarButton.CommandProperty, CreateBinding(commandPath));
        return button;
    }

    private static AppBarButton MakeCommandBarActionButton(string label, RoutedEventHandler onClick, Symbol symbol, ButtonVisualTier tier)
    {
        var button = BuildCommandBarButton(label, symbol, tier);
        button.Click += onClick;
        return button;
    }

    private static AppBarButton BuildCommandBarButton(string label, Symbol symbol, ButtonVisualTier tier)
    {
        var palette = ResolveButtonPalette(tier);
        var button = new AppBarButton
        {
            Label = label,
            Icon = new SymbolIcon(symbol),
            Foreground = new SolidColorBrush(palette.Foreground),
            FontFamily = BodyFont,
            FontWeight = tier == ButtonVisualTier.Tertiary
                ? Microsoft.UI.Text.FontWeights.Normal
                : Microsoft.UI.Text.FontWeights.SemiBold,
            MinWidth = 86,
        };

        if (tier == ButtonVisualTier.Primary)
        {
            button.Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 15, 134, 128));
            button.BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 10, 97, 96));
            button.BorderThickness = new Thickness(1);
            button.CornerRadius = new CornerRadius(8);
            button.Resources["AppBarButtonBackground"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 15, 134, 128));
            button.Resources["AppBarButtonBackgroundPointerOver"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 29, 153, 146));
            button.Resources["AppBarButtonBackgroundPressed"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 12, 111, 106));
            button.Resources["AppBarButtonForeground"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 248, 255, 255));
            button.Resources["AppBarButtonForegroundPointerOver"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 248, 255, 255));
            button.Resources["AppBarButtonForegroundPressed"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 244, 253, 255));
        }
        else
        {
            var subtle = tier == ButtonVisualTier.Secondary
                ? Microsoft.UI.ColorHelper.FromArgb(178, 237, 247, 252)
                : Microsoft.UI.ColorHelper.FromArgb(120, 245, 251, 255);
            var subtlePointer = tier == ButtonVisualTier.Secondary
                ? Microsoft.UI.ColorHelper.FromArgb(210, 228, 241, 248)
                : Microsoft.UI.ColorHelper.FromArgb(190, 234, 246, 252);
            var subtlePressed = tier == ButtonVisualTier.Secondary
                ? Microsoft.UI.ColorHelper.FromArgb(220, 216, 234, 242)
                : Microsoft.UI.ColorHelper.FromArgb(200, 224, 240, 248);
            button.Background = new SolidColorBrush(subtle);
            button.BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(160, 93, 128, 142));
            button.BorderThickness = new Thickness(1);
            button.CornerRadius = new CornerRadius(8);
            button.Resources["AppBarButtonBackground"] = new SolidColorBrush(subtle);
            button.Resources["AppBarButtonBackgroundPointerOver"] = new SolidColorBrush(subtlePointer);
            button.Resources["AppBarButtonBackgroundPressed"] = new SolidColorBrush(subtlePressed);
            button.Resources["AppBarButtonForeground"] = new SolidColorBrush(palette.Foreground);
            button.Resources["AppBarButtonForegroundPointerOver"] = new SolidColorBrush(InkLabel);
            button.Resources["AppBarButtonForegroundPressed"] = new SolidColorBrush(InkStrong);
        }

        return button;
    }

    private static TextBlock CreateSectionLabel(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontFamily = BodyFont,
            FontSize = 15,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(InkStrong),
            Margin = new Thickness(0, 2, 0, 2),
        };
    }

    private static ToggleSwitch CreateMacroPolicyToggle(string header, string bindingPath)
    {
        var toggle = new ToggleSwitch
        {
            Header = header,
            FontFamily = BodyFont,
            FontSize = 11,
            MinWidth = 92,
        };
        toggle.SetBinding(ToggleSwitch.IsOnProperty, CreateBinding(bindingPath, BindingMode.TwoWay));
        return toggle;
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
        var snippet = _viewModel.SelectedSnippet?.Source;
        if (snippet is null)
        {
            _triggerSummaryText.Text = "Keine Auswahl";
            _triggerTargetText.Text = "Zielprozess: -";
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
        var background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(248, 253, 255, 255));
        var border = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(178, 74, 111, 127));
        var borderFocused = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 30, 119, 121));
        control.BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(178, 74, 111, 127));
        control.BorderThickness = new Thickness(1);
        control.Background = background;
        control.Foreground = foreground;
        var headerForeground = new SolidColorBrush(InkLabel);
        var placeholderForeground = new SolidColorBrush(InputPlaceholder);
        control.Resources["TextControlPlaceholderForeground"] = placeholderForeground;
        control.Resources["TextControlPlaceholderForegroundFocused"] = placeholderForeground;
        control.Resources["TextControlForeground"] = foreground;
        control.Resources["TextControlForegroundFocused"] = foreground;
        control.Resources["TextControlForegroundPointerOver"] = foreground;
        control.Resources["TextControlBackground"] = background;
        control.Resources["TextControlBackgroundPointerOver"] = background;
        control.Resources["TextControlBackgroundFocused"] = background;
        control.Resources["TextControlBorderBrush"] = border;
        control.Resources["TextControlBorderBrushPointerOver"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(210, 59, 109, 127));
        control.Resources["TextControlBorderBrushFocused"] = borderFocused;
        control.Resources["TextControlHeaderForeground"] = headerForeground;
        control.Resources["TextBoxBackground"] = background;
        control.Resources["TextBoxBackgroundPointerOver"] = background;
        control.Resources["TextBoxBackgroundFocused"] = background;
        control.Resources["TextBoxForeground"] = foreground;
        control.Resources["TextBoxForegroundFocused"] = foreground;
        control.Resources["TextBoxForegroundPointerOver"] = foreground;

        void ApplyFocusedState(bool focused)
        {
            control.Background = background;
            control.Foreground = foreground;
            control.BorderBrush = focused ? borderFocused : border;
        }

        control.GotFocus += (_, _) => ApplyFocusedState(true);
        control.LostFocus += (_, _) => ApplyFocusedState(false);
        ApplyFocusedState(false);
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
                Microsoft.UI.ColorHelper.FromArgb(255, 34, 65, 79)),
            _ => (
                Microsoft.UI.ColorHelper.FromArgb(244, 239, 248, 252),
                Microsoft.UI.ColorHelper.FromArgb(176, 69, 106, 122),
                Microsoft.UI.ColorHelper.FromArgb(255, 24, 60, 76)),
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
                FontSize = 12,
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
        var stackedLayout = effective < 1200;
        var compact = effective < 920;
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
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(wide ? 470 : 410) });
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(_leftPaneCard, 0);
            Grid.SetRow(_leftPaneCard, 0);

            Grid.SetColumn(_centerPaneCard, 1);
            Grid.SetRow(_centerPaneCard, 0);

            Grid.SetColumn(_rightColumnGrid, 2);
            Grid.SetRow(_rightColumnGrid, 0);

            _rightColumnGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
            _rightColumnGrid.RowDefinitions[1].Height = new GridLength(wide ? 250 : 220);
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
            _shellStatusText.FontSize = 14;
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
            _shellStatusText.FontSize = 15;
        }
    }

    private void ApplyDensityByWidth(double width)
    {
        var compact = width < 920;
        var stacked = width < 1200;
        var medium = width >= 920 && width < 1200;

        if (_folderList is not null)
        {
            _folderList.MinHeight = compact ? 120 : stacked ? 130 : 140;
            _folderList.MaxHeight = compact ? 190 : medium ? 220 : 250;
        }

        if (_snippetList is not null)
        {
            _snippetList.MinHeight = compact ? 150 : stacked ? 180 : 220;
            _snippetList.MaxHeight = compact ? 320 : medium ? 420 : 560;
        }

        if (_editorSplitGrid is not null)
        {
            _editorSplitGrid.MinHeight = compact ? 260 : stacked ? 300 : 340;
        }

        if (_documentPreview is not null)
        {
            _documentPreview.MinHeight = compact ? 92 : 132;
        }

        if (_clipboardPreview is not null)
        {
            _clipboardPreview.MinHeight = compact ? 92 : 132;
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
            @"<\s*(iframe|object|embed|link|meta)\b[^>]*\s*(?:\/?>|>[\s\S]*?<\s*/\s*\1\s*>)",
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

        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"(?i)\b(href|src)\s*=\s*javascript:[^\s>]+",
            "$1=\"#\"");

        return cleaned;
    }
}

