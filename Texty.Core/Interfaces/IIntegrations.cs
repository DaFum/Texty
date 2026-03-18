namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface IExternalDataResolver
{
    string Name { get; }
    Task<string?> ResolveAsync(ExternalValueRequest request, CancellationToken cancellationToken = default);
}

public interface IExternalDataResolverFactory
{
    IExternalDataResolver? GetResolver(string resolverName);
}

public interface IImportService
{
    Task<ImportResult> ImportAsync(string sourcePath, string format, CancellationToken cancellationToken = default);
}
