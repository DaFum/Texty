using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Ganss.Xss;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Templating;

public sealed class TemplateRenderer : ITemplateRenderer
{
    private static readonly Regex PlaceholderRegex = new(@"\{\{\s*(?<key>[a-zA-Z0-9_.-]+)\s*\}\}", RegexOptions.Compiled);

    public Task<TemplateRenderResult> RenderAsync(Snippet snippet, RenderContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var htmlTemplate = snippet.Template?.BodyTemplate ?? snippet.HtmlText;
        var plainTemplate = snippet.PlainText;

        var plain = ReplacePlaceholders(plainTemplate, context, htmlMode: false, cancellationToken);
        var html = ReplacePlaceholders(htmlTemplate, context, htmlMode: true, cancellationToken);

        return Task.FromResult(new TemplateRenderResult(plain, html));
    }

    private static string ReplacePlaceholders(
        string template,
        RenderContext context,
        bool htmlMode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        var result = new StringBuilder(template.Length + 64);
        var cursor = 0;

        foreach (Match match in PlaceholderRegex.Matches(template))
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.Append(template, cursor, match.Index - cursor);

            var key = match.Groups["key"].Value;
            if (context.Values.TryGetValue(key, out var value))
            {
                result.Append(htmlMode ? FormatHtmlValue(value) : FormatPlainValue(value));
            }
            else
            {
                result.Append(match.Value);
            }

            cursor = match.Index + match.Length;
        }

        result.Append(template, cursor, template.Length - cursor);
        return result.ToString();
    }

    private static string FormatPlainValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            TemplateValue templateValue => templateValue.PlainText,
            DateTimeOffset dto => dto.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
        };
    }

    private static string FormatHtmlValue(object? value)
    {
        if (value is TemplateValue templateValue)
        {
            return SanitizeHtml(templateValue.HtmlText);
        }

        var plain = FormatPlainValue(value);
        return System.Net.WebUtility.HtmlEncode(plain)
            .Replace("\r\n", "<br/>", StringComparison.Ordinal)
            .Replace("\n", "<br/>", StringComparison.Ordinal);
    }

    private static string SanitizeHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var sanitizer = CreateSanitizer();
        return sanitizer.Sanitize(html);
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("http");
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedSchemes.Add("mailto");
        sanitizer.AllowedSchemes.Add("data");

        sanitizer.AllowedAttributes.Add("class");
        sanitizer.AllowedAttributes.Add("data-key");
        sanitizer.AllowedAttributes.Add("src");
        sanitizer.AllowedAttributes.Add("alt");
        sanitizer.AllowedAttributes.Add("title");

        return sanitizer;
    }
}
