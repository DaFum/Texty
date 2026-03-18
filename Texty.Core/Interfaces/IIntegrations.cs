namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface IExternalDataResolver
{
    string Name { get; }
    /// <summary>
/// Löst einen externen Wert basierend auf der angegebenen ExternalValueRequest.
/// </summary>
/// <param name="request">Die Anfrage, die Angaben zum zu lösenden externen Wert enthält.</param>
/// <param name="cancellationToken">Token zum Abbrechen der Operation.</param>
/// <returns>Die aufgelöste Zeichenfolge, oder <c>null</c>, wenn kein Wert ermittelt werden konnte.</returns>
Task<string?> ResolveAsync(ExternalValueRequest request, CancellationToken cancellationToken = default);
}

public interface IExternalDataResolverFactory
{
    /// <summary>
/// Gibt den External-Datenresolver mit dem angegebenen Namen zurück.
/// </summary>
/// <param name="resolverName">Name des gesuchten Resolvers.</param>
/// <returns>Der Resolver mit dem angegebenen Namen, oder <c>null</c>, wenn kein passender Resolver vorhanden ist.</returns>
IExternalDataResolver? GetResolver(string resolverName);
}

public interface IImportService
{
    /// <summary>
/// Importiert Daten von einem Quellpfad im angegebenen Format.
/// </summary>
/// <param name="sourcePath">Pfad oder URI zur Datenquelle, von der importiert werden soll.</param>
/// <param name="format">Formatkennung oder Dateierweiterung, die das Eingabeformat beschreibt.</param>
/// <param name="cancellationToken">Token zum Abbrechen des Vorgangs.</param>
/// <returns>Ein ImportResult mit Details zum Import, inklusive Status, gefundener Fehler und gegebenenfalls importierter Entitäten.</returns>
Task<ImportResult> ImportAsync(string sourcePath, string format, CancellationToken cancellationToken = default);
}
