using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Integrations.Resolvers;

public sealed class ExcelResolver : IExternalDataResolver
{
    private readonly CsvResolver _csvFallback = new();

    public string Name => "excel";

    public Task<string?> ResolveAsync(ExternalValueRequest request, CancellationToken cancellationToken = default)
    {
        // v1 baseline maps excel resolver to csv-compatible expressions:
        // "<path.csv>|<row>|<column>".
        return _csvFallback.ResolveAsync(request, cancellationToken);
    }
}
