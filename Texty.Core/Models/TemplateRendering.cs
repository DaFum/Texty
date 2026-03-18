namespace Texty.Core.Models;

public sealed record RenderContext(IReadOnlyDictionary<string, object?> Values);

public sealed record TemplateRenderResult(string PlainText, string HtmlText);
