namespace Texty.Core.Models;

public sealed record RenderContext(IReadOnlyDictionary<string, object?> Values);

public sealed record TemplateValue(string PlainText, string HtmlText);

public sealed record TemplateRenderResult(string PlainText, string HtmlText);
