using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Integrations.Resolvers;

public sealed class EnvResolver : IExternalDataResolver
{
    public string Name => "env";

    public Task<string?> ResolveAsync(ExternalValueRequest request, CancellationToken cancellationToken = default)
    {
        var variable = request.Expression.Trim();
        if (string.IsNullOrWhiteSpace(variable))
        {
            return Task.FromResult<string?>(null);
        }

        return Task.FromResult(Environment.GetEnvironmentVariable(variable));
    }
}
