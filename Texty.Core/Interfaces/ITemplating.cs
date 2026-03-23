namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface ITemplateRenderer
{
    Task<TemplateRenderResult> RenderAsync(
        Snippet snippet,
        RenderContext context,
        CancellationToken cancellationToken = default);
}

public interface IFormSchemaValidator
{
    IReadOnlyList<string> Validate(SnippetTemplate template);
}
