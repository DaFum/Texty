using System.Globalization;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Texty.App.Services;
using Texty.Core.Models;

namespace Texty.App.Pages;

public sealed partial class MainPage
{
    private const long MaxInlineImageBytes = 10 * 1024 * 1024;

    private async void OnInsertClicked(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;

        var runtime = AppRuntimeService.RuntimeContext;
        if (runtime is null)
        {
            _viewModel.StatusText = "Runtime noch nicht initialisiert.";
            return;
        }

        var selected = _viewModel.SelectedSnippet?.Source;
        if (selected is null)
        {
            _viewModel.StatusText = "Bitte zuerst einen Baustein auswaehlen.";
            return;
        }

        await SyncEditorSnapshotAsync();

        var normalizedHtml = BuildEditorHtmlFragment(_viewModel.EditorPlainText, _viewModel.EditorHtmlText);
        var normalizedSnippet = selected with
        {
            PlainText = _viewModel.EditorPlainText,
            HtmlText = normalizedHtml,
            UpdatedUtc = DateTimeOffset.UtcNow,
            LastEditor = Environment.UserName,
        };

        var fields = GetEffectiveTemplateFields(normalizedSnippet);
        var validationErrors = runtime.FormSchemaValidator.Validate(new SnippetTemplate(normalizedSnippet.HtmlText, fields));
        if (validationErrors.Count > 0)
        {
            _viewModel.StatusText = $"Formularschema ungueltig: {string.Join(" | ", validationErrors)}";
            return;
        }

        IReadOnlyDictionary<string, object?> values = new Dictionary<string, object?>();
        if (fields.Count > 0)
        {
            var providedValues = await ShowTemplateFormDialogAsync(normalizedSnippet.Title, fields);
            if (providedValues is null)
            {
                _viewModel.StatusText = "Einfuegen abgebrochen.";
                return;
            }

            values = providedValues;
        }

        var renderSnippet = normalizedSnippet with
        {
            Template = fields.Count > 0
                ? new SnippetTemplate(normalizedSnippet.HtmlText, fields)
                : null,
        };

        var renderResult = fields.Count > 0
            ? await runtime.TemplateRenderer.RenderAsync(renderSnippet, new RenderContext(values))
            : new TemplateRenderResult(normalizedSnippet.PlainText, normalizedSnippet.HtmlText);

        var contextVariables = values.ToDictionary(
            x => x.Key,
            x => ToVariableString(x.Value),
            StringComparer.OrdinalIgnoreCase);

        var insertionResults = await runtime.InsertionPipeline.ExecuteAsync(
            new InsertionPayload(renderResult.PlainText, renderResult.HtmlText, []),
            new InsertionContext(null, false, false, contextVariables));

        var insertSucceeded = insertionResults.Any(r => r.Step == InsertionStep.Insert && r.Success);
        if (insertSucceeded)
        {
            runtime.ProductivityStatsService.TrackInsertion();
            runtime.ClipboardHistoryService.Add(new ClipboardItem(renderResult.PlainText, renderResult.HtmlText, null));
        }

        _viewModel.StatusText = insertSucceeded
            ? "Baustein eingefuegt."
            : $"Einfuegen fehlgeschlagen ({insertionResults.Count(r => !r.Success)} Fehler).";
    }

    private async void OnFormatBoldClicked(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        await ExecuteEditorCommandAsync("bold");
    }

    private async void OnFormatItalicClicked(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        await ExecuteEditorCommandAsync("italic");
    }

    private async void OnFormatUnderlineClicked(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        await ExecuteEditorCommandAsync("underline");
    }

    private async void OnBulletListClicked(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        await ExecuteEditorCommandAsync("insertUnorderedList");
    }

    private async void OnNumberedListClicked(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        await ExecuteEditorCommandAsync("insertOrderedList");
    }

    private async void OnInsertPlaceholderClicked(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;

        var key = NormalizePlaceholderKey(_placeholderTokenInput.Text);
        if (string.IsNullOrWhiteSpace(key))
        {
            _viewModel.StatusText = "Bitte gueltigen Placeholder-Key eingeben.";
            return;
        }

        await ExecuteEditorScriptAsync($"window.textyEditor?.insertPlaceholder({JsonSerializer.Serialize(key)});");
        await SyncEditorSnapshotAsync();
        _viewModel.StatusText = $"Placeholder '{{{{{key}}}}}' eingefuegt.";
    }

    private async void OnInsertTableClicked(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;

        var rows = NormalizeTableDimension(_tableRowsInput.Value);
        var columns = NormalizeTableDimension(_tableColumnsInput.Value);
        await ExecuteEditorScriptAsync($"window.textyEditor?.insertTable({rows}, {columns});");
        await SyncEditorSnapshotAsync();
    }

    private async void OnInsertImageClicked(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;

        var imagePath = _imagePathInput.Text.Trim();
        if (!File.Exists(imagePath))
        {
            _viewModel.StatusText = "Bildpfad nicht gefunden.";
            return;
        }

        var fileInfo = new FileInfo(imagePath);
        if (fileInfo.Length > MaxInlineImageBytes)
        {
            _viewModel.StatusText = $"Bild zu gross (max. {MaxInlineImageBytes / (1024 * 1024)} MB).";
            return;
        }

        try
        {
            var bytes = await File.ReadAllBytesAsync(imagePath);
            var extension = Path.GetExtension(imagePath).ToLowerInvariant();
            var mime = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "image/png",
            };
            var dataUrl = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";

            await ExecuteEditorScriptAsync(
                $"window.textyEditor?.insertImage({JsonSerializer.Serialize(dataUrl)}, {JsonSerializer.Serialize(Path.GetFileName(imagePath))});");
            await SyncEditorSnapshotAsync();
            _viewModel.StatusText = $"Bild eingefuegt: {Path.GetFileName(imagePath)}";
        }
        catch (Exception ex)
        {
            _viewModel.StatusText = $"Bild konnte nicht eingefuegt werden: {ex.Message}";
        }
    }

    private static int NormalizeTableDimension(double raw)
    {
        if (double.IsNaN(raw) || double.IsInfinity(raw))
        {
            return 2;
        }

        return Math.Clamp(Convert.ToInt32(Math.Round(raw, MidpointRounding.AwayFromZero)), 1, 12);
    }

    private async Task ExecuteEditorCommandAsync(string command)
    {
        await ExecuteEditorScriptAsync($"window.textyEditor?.exec({JsonSerializer.Serialize(command)});");
        await SyncEditorSnapshotAsync();
    }

    private async Task ExecuteEditorScriptAsync(string script)
    {
        if (_htmlPreview?.CoreWebView2 is null || !_isWebViewInitialized || !_isEditorDomReady)
        {
            return;
        }

        try
        {
            await _htmlPreview.CoreWebView2.ExecuteScriptAsync(script);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"WebView2 script execution failed: {ex}");
            ActivateFallbackPreview(
                "WebView2 Editor nicht verfuegbar, Fallback aktiv.",
                BuildEditorHtmlFragment(_viewModel.EditorPlainText, _viewModel.EditorHtmlText));
        }
    }

    private async Task SyncEditorSnapshotAsync()
    {
        if (_htmlPreview?.CoreWebView2 is null || !_isWebViewInitialized || !_isEditorDomReady)
        {
            return;
        }

        try
        {
            var result = await _htmlPreview.CoreWebView2.ExecuteScriptAsync(
                "(() => { const c = window.textyEditor?.getContent(); return c ? JSON.stringify(c) : null; })();");

            var payloadJson = JsonSerializer.Deserialize<string>(result);
            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                return;
            }

            using var doc = JsonDocument.Parse(payloadJson);
            var html = doc.RootElement.TryGetProperty("html", out var htmlElement)
                ? htmlElement.GetString() ?? string.Empty
                : string.Empty;
            var plain = doc.RootElement.TryGetProperty("plain", out var plainElement)
                ? plainElement.GetString() ?? string.Empty
                : string.Empty;

            ApplyEditorContent(html, plain);
        }
        catch
        {
            ActivateFallbackPreview(
                "Editor-Synchronisierung fehlgeschlagen, Fallback aktiv.",
                BuildEditorHtmlFragment(_viewModel.EditorPlainText, _viewModel.EditorHtmlText));
        }
    }

    private async Task<IReadOnlyDictionary<string, object?>?> ShowTemplateFormDialogAsync(
        string snippetTitle,
        IReadOnlyList<TemplateField> fields)
    {
        var bindings = new List<FormFieldBinding>();
        var formPanel = new StackPanel { Spacing = 10 };
        var validationText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.IndianRed),
            Visibility = Visibility.Collapsed,
        };

        foreach (var field in fields)
        {
            formPanel.Children.Add(BuildFieldControl(field, bindings));
        }

        formPanel.Children.Add(validationText);

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = $"Eingaben fuer '{snippetTitle}'",
            PrimaryButtonText = "Einsetzen",
            CloseButtonText = "Abbrechen",
            DefaultButton = ContentDialogButton.Primary,
            Content = new ScrollViewer
            {
                Content = formPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MaxHeight = 560,
                MinWidth = 560,
            },
        };

        Dictionary<string, object?>? submittedValues = null;
        dialog.PrimaryButtonClick += (_, args) =>
        {
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            var errors = new List<string>();

            foreach (var binding in bindings)
            {
                var value = binding.Getter();
                if (binding.Field.Required && IsEmptyFieldValue(value))
                {
                    errors.Add($"'{binding.Field.Label}' ist erforderlich.");
                    continue;
                }

                values[binding.Field.Key] = value;
            }

            if (errors.Count > 0)
            {
                args.Cancel = true;
                validationText.Text = string.Join(Environment.NewLine, errors);
                validationText.Visibility = Visibility.Visible;
                return;
            }

            validationText.Visibility = Visibility.Collapsed;
            submittedValues = values;
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return null;
        }

        return submittedValues ?? new Dictionary<string, object?>();
    }

    private FrameworkElement BuildFieldControl(TemplateField field, ICollection<FormFieldBinding> bindings)
    {
        var container = new StackPanel { Spacing = 6 };
        container.Children.Add(new TextBlock
        {
            Text = field.Label,
            FontFamily = BodyFont,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 25, 64, 77)),
        });

        switch (field.FieldType)
        {
            case FormFieldType.Dropdown:
            {
                var options = field.Options?.ToList() ?? [];
                var combo = new ComboBox
                {
                    ItemsSource = options,
                    DisplayMemberPath = nameof(TemplateFieldOption.Label),
                    SelectedIndex = options.Count > 0 ? 0 : -1,
                    PlaceholderText = field.Placeholder,
                };
                if (!string.IsNullOrWhiteSpace(field.DefaultValue))
                {
                    var defaultIndex = options.FindIndex(o => string.Equals(o.Key, field.DefaultValue, StringComparison.OrdinalIgnoreCase));
                    if (defaultIndex >= 0)
                    {
                        combo.SelectedIndex = defaultIndex;
                    }
                }

                container.Children.Add(combo);
                bindings.Add(new FormFieldBinding(field, () =>
                {
                    return combo.SelectedItem is TemplateFieldOption selected
                        ? selected.Key
                        : null;
                }));
                break;
            }
            case FormFieldType.Radio:
            {
                var options = field.Options?.ToList() ?? [];
                var radioGroup = $"group_{Guid.NewGuid():N}";
                var panel = new StackPanel { Spacing = 4 };
                var buttons = new List<(TemplateFieldOption Option, RadioButton Button)>(options.Count);

                foreach (var option in options)
                {
                    var radio = new RadioButton
                    {
                        GroupName = radioGroup,
                        Content = option.Label,
                    };
                    panel.Children.Add(radio);
                    buttons.Add((option, radio));
                }

                if (buttons.Count > 0)
                {
                    var defaultPair = buttons.FirstOrDefault(x =>
                        string.Equals(x.Option.Key, field.DefaultValue, StringComparison.OrdinalIgnoreCase));

                    (defaultPair.Button ?? buttons[0].Button).IsChecked = true;
                }

                container.Children.Add(panel);
                bindings.Add(new FormFieldBinding(field, () =>
                {
                    var selected = buttons.FirstOrDefault(x => x.Button.IsChecked == true);
                    return selected.Option?.Key;
                }));
                break;
            }
            case FormFieldType.Checkbox:
            {
                var check = new CheckBox
                {
                    Content = field.Placeholder ?? field.Label,
                    IsChecked = bool.TryParse(field.DefaultValue, out var initial) && initial,
                };
                container.Children.Add(check);
                bindings.Add(new FormFieldBinding(field, () => check.IsChecked == true));
                break;
            }
            case FormFieldType.Slider:
            {
                var minimum = field.Min ?? 0;
                var maximum = field.Max ?? 100;
                var sliderValue = minimum;
                if (double.TryParse(field.DefaultValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedDefault))
                {
                    sliderValue = Math.Clamp(parsedDefault, minimum, maximum);
                }

                var valueLabel = new TextBlock
                {
                    FontFamily = MonoFont,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 76, 109, 122)),
                    Text = sliderValue.ToString("0.##", CultureInfo.InvariantCulture),
                };

                var slider = new Slider
                {
                    Minimum = minimum,
                    Maximum = maximum,
                    Value = sliderValue,
                    StepFrequency = 1,
                };
                slider.ValueChanged += (_, args) =>
                {
                    valueLabel.Text = args.NewValue.ToString("0.##", CultureInfo.InvariantCulture);
                };

                container.Children.Add(slider);
                container.Children.Add(valueLabel);
                bindings.Add(new FormFieldBinding(field, () => slider.Value));
                break;
            }
            case FormFieldType.DatePicker:
            {
                var picker = new DatePicker();
                if (DateTimeOffset.TryParse(field.DefaultValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedDate))
                {
                    picker.Date = parsedDate;
                }

                container.Children.Add(picker);
                bindings.Add(new FormFieldBinding(field, () => picker.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                break;
            }
            case FormFieldType.Table:
            {
                var input = new TextBox
                {
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    MinHeight = 120,
                    PlaceholderText = "Zeilen mit Enter, Spalten mit ; trennen",
                    Text = field.DefaultValue ?? string.Empty,
                };
                ScrollViewer.SetVerticalScrollBarVisibility(input, ScrollBarVisibility.Auto);
                container.Children.Add(input);
                bindings.Add(new FormFieldBinding(field, () => ParseTableValue(input.Text)));
                break;
            }
            default:
            {
                var text = new TextBox
                {
                    PlaceholderText = field.Placeholder,
                    Text = field.DefaultValue ?? string.Empty,
                };
                container.Children.Add(text);
                bindings.Add(new FormFieldBinding(field, () => text.Text.Trim()));
                break;
            }
        }

        return container;
    }

    private static IReadOnlyList<TemplateField> GetEffectiveTemplateFields(Snippet snippet)
    {
        if (snippet.Template?.Fields is { Count: > 0 })
        {
            return snippet.Template.Fields;
        }

        var keys = ExtractPlaceholderKeys(snippet.PlainText, snippet.HtmlText);
        return keys
            .Select(k => new TemplateField(k, k, FormFieldType.Text, true, $"Wert fuer {k}", null, null, null, null))
            .ToList();
    }

    private static IReadOnlyList<string> ExtractPlaceholderKeys(string plainText, string? htmlText)
    {
        var source = $"{plainText}\n{htmlText}";
        return PlaceholderRegex
            .Matches(source)
            .Select(match => match.Groups["key"].Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizePlaceholderKey(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        return System.Text.RegularExpressions.Regex.Replace(raw.Trim(), @"[^a-zA-Z0-9_.-]", string.Empty);
    }

    private static bool IsEmptyFieldValue(object? value)
    {
        return value switch
        {
            null => true,
            string text => string.IsNullOrWhiteSpace(text),
            bool b => !b,
            TemplateValue tv => string.IsNullOrWhiteSpace(tv.PlainText) && string.IsNullOrWhiteSpace(tv.HtmlText),
            _ => false,
        };
    }

    private static TemplateValue ParseTableValue(string rawText)
    {
        var rows = rawText
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(row => row.Split(';', StringSplitOptions.TrimEntries))
            .Where(columns => columns.Length > 0)
            .ToList();

        if (rows.Count == 0)
        {
            return new TemplateValue(string.Empty, string.Empty);
        }

        var plainLines = rows.Select(columns => string.Join(" | ", columns));
        var plain = string.Join(Environment.NewLine, plainLines);

        var htmlRows = rows.Select(columns =>
            "<tr>" + string.Join(string.Empty, columns.Select(col => $"<td>{System.Net.WebUtility.HtmlEncode(col)}</td>")) + "</tr>");
        var html = "<table style=\"border-collapse:collapse;border:1px solid #8aa0ad;\">" +
            string.Join(string.Empty, htmlRows) +
            "</table>";

        return new TemplateValue(plain, html);
    }

    private static string ToVariableString(object? value)
    {
        return value switch
        {
            null => string.Empty,
            TemplateValue tv => tv.PlainText,
            bool b => b ? "true" : "false",
            double d => d.ToString("0.##", CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
        };
    }

    private static string BuildEditorHtmlFragment(string plainText, string? htmlText)
    {
        if (string.IsNullOrWhiteSpace(htmlText))
        {
            var encoded = System.Net.WebUtility.HtmlEncode(plainText ?? string.Empty)
                .Replace("\r\n", "<br/>", StringComparison.Ordinal)
                .Replace("\n", "<br/>", StringComparison.Ordinal);
            return string.IsNullOrWhiteSpace(encoded) ? "<p><br/></p>" : $"<p>{encoded}</p>";
        }

        var sanitized = SanitizeHtmlFragment(htmlText);
        return string.IsNullOrWhiteSpace(sanitized) ? "<p><br/></p>" : sanitized;
    }

    private static string BuildEditorShellHtml()
    {
        var nonce = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var html = """
            <!doctype html>
            <html>
            <head>
            <meta charset="utf-8" />
            <meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src data: blob:; style-src 'unsafe-inline'; script-src 'nonce-__TEXTY_NONCE__';" />
            <style>
                html, body {
                    margin: 0;
                    padding: 0;
                    height: 100%;
                    background: #ecf4f8;
                    color: #16313f;
                    font-family: "Candara", "Segoe UI Variable", sans-serif;
                }
                #editor {
                    min-height: 100%;
                    box-sizing: border-box;
                    padding: 12px;
                    outline: none;
                    line-height: 1.5;
                    white-space: pre-wrap;
                    word-break: break-word;
                }
                table {
                    border-collapse: collapse;
                    margin: 4px 0;
                }
                td, th {
                    border: 1px solid #7e98a6;
                    min-width: 40px;
                    padding: 4px 8px;
                }
                .texty-placeholder {
                    display: inline-block;
                    border: 1px dashed #0a8a86;
                    background: #d8f4f2;
                    color: #0d5250;
                    border-radius: 4px;
                    padding: 0 4px;
                    font-family: "Cascadia Code", "Courier New", monospace;
                    font-size: 0.9em;
                }
                img {
                    max-width: 100%;
                    height: auto;
                    border-radius: 6px;
                }
            </style>
            </head>
            <body>
                <div id="editor" contenteditable="true"><p><br /></p></div>
                <script nonce="__TEXTY_NONCE__">
                    const editor = document.getElementById('editor');
                    let suppressChange = false;

                    const emitContent = () => {
                        if (suppressChange) {
                            return;
                        }
                        window.chrome.webview.postMessage({
                            type: 'content',
                            html: editor.innerHTML,
                            plain: editor.innerText || ''
                        });
                    };

                    const normalize = (value) => (value && value.trim().length > 0) ? value : '<p><br /></p>';

                    const ensureSelection = () => {
                        const selection = window.getSelection();
                        if (!selection || selection.rangeCount > 0) {
                            return selection;
                        }
                        const range = document.createRange();
                        range.selectNodeContents(editor);
                        range.collapse(false);
                        selection.addRange(range);
                        return selection;
                    };

                    window.textyEditor = {
                        setHtml(value) {
                            suppressChange = true;
                            editor.innerHTML = normalize(value);
                            suppressChange = false;
                        },
                        getContent() {
                            return {
                                html: editor.innerHTML,
                                plain: editor.innerText || ''
                            };
                        },
                        exec(command) {
                            editor.focus();
                            document.execCommand(command, false, null);
                            emitContent();
                        },
                        insertPlaceholder(key) {
                            if (!key) {
                                return;
                            }
                            editor.focus();
                            const selection = ensureSelection();
                            const token = document.createElement('span');
                            token.className = 'texty-placeholder';
                            token.setAttribute('data-key', key);
                            token.textContent = `{{${key}}}`;
                            if (!selection || selection.rangeCount === 0) {
                                editor.appendChild(token);
                            } else {
                                const range = selection.getRangeAt(0);
                                range.deleteContents();
                                range.insertNode(token);
                                range.setStartAfter(token);
                                range.setEndAfter(token);
                                selection.removeAllRanges();
                                selection.addRange(range);
                            }
                            emitContent();
                        },
                        insertTable(rows, columns) {
                            const rowCount = Math.max(1, Number(rows || 1));
                            const columnCount = Math.max(1, Number(columns || 1));
                            const table = document.createElement('table');
                            const tbody = document.createElement('tbody');
                            for (let r = 0; r < rowCount; r++) {
                                const tr = document.createElement('tr');
                                for (let c = 0; c < columnCount; c++) {
                                    const td = document.createElement('td');
                                    td.innerHTML = '&nbsp;';
                                    tr.appendChild(td);
                                }
                                tbody.appendChild(tr);
                            }
                            table.appendChild(tbody);
                            editor.focus();
                            document.execCommand('insertHTML', false, table.outerHTML + '<p><br /></p>');
                            emitContent();
                        },
                        insertImage(src, alt) {
                            if (!src) {
                                return;
                            }
                            const img = document.createElement('img');
                            img.src = src;
                            img.alt = alt || '';
                            editor.focus();
                            document.execCommand('insertHTML', false, img.outerHTML + '<p><br /></p>');
                            emitContent();
                        }
                    };

                    editor.addEventListener('input', emitContent);
                    editor.addEventListener('keyup', emitContent);
                    editor.addEventListener('paste', () => setTimeout(emitContent, 0));
                    editor.addEventListener('blur', emitContent);
                    window.chrome.webview.postMessage({ type: 'ready' });
                </script>
            </body>
            </html>
            """;
        return html.Replace("__TEXTY_NONCE__", nonce, StringComparison.Ordinal);
    }

    private void OnHtmlPreviewNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        _ = sender;
        if (args.IsSuccess)
        {
            return;
        }

        ActivateFallbackPreview(
            $"WebView2 Navigation fehlgeschlagen ({args.WebErrorStatus}).",
            BuildEditorHtmlFragment(_viewModel.EditorPlainText, _viewModel.EditorHtmlText));
    }

    private void OnHtmlPreviewWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        _ = sender;

        try
        {
            using var doc = JsonDocument.Parse(args.WebMessageAsJson);
            if (!doc.RootElement.TryGetProperty("type", out var typeElement))
            {
                return;
            }

            var type = typeElement.GetString();
            if (string.Equals(type, "ready", StringComparison.OrdinalIgnoreCase))
            {
                _isEditorDomReady = true;
                _ = PushViewModelContentToEditorAsync();
                return;
            }

            if (!string.Equals(type, "content", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var html = doc.RootElement.TryGetProperty("html", out var htmlElement)
                ? htmlElement.GetString() ?? string.Empty
                : string.Empty;
            var plain = doc.RootElement.TryGetProperty("plain", out var plainElement)
                ? plainElement.GetString() ?? string.Empty
                : string.Empty;
            ApplyEditorContent(html, plain);
        }
        catch
        {
            ActivateFallbackPreview(
                "WebView2 Nachrichten konnten nicht verarbeitet werden.",
                BuildEditorHtmlFragment(_viewModel.EditorPlainText, _viewModel.EditorHtmlText));
        }
    }

    private void ApplyEditorContent(string html, string plain)
    {
        if (_isUpdatingFromEditor)
        {
            return;
        }

        _isUpdatingFromEditor = true;
        _viewModel.EditorHtmlText = BuildEditorHtmlFragment(plain, html);
        _viewModel.EditorPlainText = plain;
        _isUpdatingFromEditor = false;
    }

    private static Button MakeActionButton(string text, RoutedEventHandler onClick, ButtonVisualTier tier)
    {
        var button = BuildStyledButton(text, tier);

        AttachHoverMotion(button, -2);
        button.Click += onClick;
        return button;
    }

    private sealed record FormFieldBinding(TemplateField Field, Func<object?> Getter);
}

