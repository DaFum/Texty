using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Integrations.Resolvers;

public sealed class SqlResolver : IExternalDataResolver
{
    public string Name => "sql";

    /// <summary>
    /// Löst einen Wert aus einer SQL-Ausdruckszeichenfolge und liefert den entsprechenden Spaltenwert als Text.
    /// </summary>
    /// <param name="request">Enthält die Ausdruckszeichenfolge im Format "&lt;connectionName&gt;|&lt;sql&gt;|&lt;columnName&gt;".</param>
    /// <returns>Der gefundene Spaltenwert als String, oder <c>null</c> wenn kein Wert ermittelt wurde oder die Funktion aktuell nicht implementiert ist.</returns>
    public Task<string?> ResolveAsync(ExternalValueRequest request, CancellationToken cancellationToken = default)
    {
        // Placeholder for parameterized SQL query execution.
        // Expression format target: "<connectionName>|<sql>|<columnName>".
        _ = request;
        return Task.FromResult<string?>(null);
    }
}
