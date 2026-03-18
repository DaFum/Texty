using System.Text.RegularExpressions;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Templating;

public sealed class TemplateRenderer : ITemplateRenderer
{
    private static readonly Regex PlaceholderRegex = new(@"\{\{\s*(?<key>[a-zA-Z0-9_.-]+)\s*\}\}", RegexOptions.Compiled);

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
