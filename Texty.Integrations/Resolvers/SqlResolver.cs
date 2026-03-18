using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Integrations.Resolvers;

public sealed class SqlResolver : IExternalDataResolver
{
    public string Name => "sql";

    public Task<string?> ResolveAsync(ExternalValueRequest request, CancellationToken cancellationToken = default)
    {
        // Placeholder for parameterized SQL query execution.
        // Expression format target: "<connectionName>|<sql>|<columnName>".
        _ = request;
        return Task.FromResult<string?>(null);
    }
}
