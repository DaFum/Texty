namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface ITemplateRenderer
{
    /// <summary>
        /// Rendert ein Snippet anhand des übergebenen RenderContext.
        /// </summary>
        /// <param name="snippet">Das zu rendernde Snippet.</param>
        /// <param name="context">Kontext und Einstellungen, die während des Renderings verwendet werden.</param>
        /// <param name="cancellationToken">Token zum Abbrechen des asynchronen Renderings.</param>
        /// <returns>Ein TemplateRenderResult mit dem gerenderten Inhalt und zugehörigen Metadaten.</returns>
        Task<TemplateRenderResult> RenderAsync(
        Snippet snippet,
        RenderContext context,
        CancellationToken cancellationToken = default);
}

public interface IFormSchemaValidator
{
    /// <summary>
/// Validiert ein SnippetTemplate und liefert alle gefundenen Validierungsfehler.
/// </summary>
/// <param name="template">Die zu prüfende SnippetTemplate-Instanz.</param>
/// <returns>Eine Auflistung von Fehlermeldungen; leer, wenn keine Validierungsfehler vorliegen.</returns>
IReadOnlyList<string> Validate(SnippetTemplate template);
}
