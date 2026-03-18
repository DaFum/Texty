using System.Text.RegularExpressions;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Templating;

public sealed class TemplateRenderer : ITemplateRenderer
{
    private static readonly Regex PlaceholderRegex = new(@"\{\{\s*(?<key>[a-zA-Z0-9_.-]+)\s*\}\}", RegexOptions.Compiled);

    /// <summary>
    /// Ersetzt Platzhalter in den HTML- und Plaintext-Vorlagen eines Snippets anhand der Werte aus dem RenderContext.
    /// </summary>
    /// <param name="snippet">Das zu rendernde Snippet; dessen Template.BodyTemplate wird bevorzugt, sonst wird HtmlText verwendet. PlainText wird für die reine Textausgabe genutzt.</param>
    /// <param name="context">Enthält die Schlüssel-Wert-Paare, die für die Platzhalterersetzung herangezogen werden.</param>
    /// <returns>Ein TemplateRenderResult mit den gerenderten Plaintext- und HTML-Inhalten.</returns>
    public Task<TemplateRenderResult> RenderAsync(Snippet snippet, RenderContext context, CancellationToken cancellationToken = default)
    {
        var html = snippet.Template?.BodyTemplate ?? snippet.HtmlText;
        var plain = snippet.PlainText;

        foreach (Match match in PlaceholderRegex.Matches(html))
        {
            var key = match.Groups["key"].Value;
            if (context.Values.TryGetValue(key, out var value))
            {
                html = html.Replace(match.Value, Convert.ToString(value) ?? string.Empty, StringComparison.Ordinal);
            }
        }

        foreach (Match match in PlaceholderRegex.Matches(plain))
        {
            var key = match.Groups["key"].Value;
            if (context.Values.TryGetValue(key, out var value))
            {
                plain = plain.Replace(match.Value, Convert.ToString(value) ?? string.Empty, StringComparison.Ordinal);
            }
        }

        return Task.FromResult(new TemplateRenderResult(plain, html));
    }
}
