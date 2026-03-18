using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Integrations.Resolvers;

public sealed class ExcelResolver : IExternalDataResolver
{
    private readonly CsvResolver _csvFallback = new();

    public string Name => "excel";

    /// <summary>
    /// Löst einen externen Wert für Excel-kompatible Ausdrücke und delegiert die Auflösung an den CSV-Fallback.
    /// </summary>
    /// <param name="request">Die Anforderung, die den zu resolvierenden Ausdruck enthält. Erwartetes v1-Format: "&lt;path.csv&gt;|&lt;row&gt;|&lt;column&gt;".</param>
    /// <param name="cancellationToken">Token zum Abbrechen der asynchronen Operation.</param>
    /// <returns>Der aufgelöste Wert als Zeichenkette, oder `null`, wenn kein Wert gefunden wurde.</returns>
    public Task<string?> ResolveAsync(ExternalValueRequest request, CancellationToken cancellationToken = default)
    {
        // v1 baseline maps excel resolver to csv-compatible expressions:
        // "<path.csv>|<row>|<column>".
        return _csvFallback.ResolveAsync(request, cancellationToken);
    }
}
