using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Integrations.Resolvers;

public sealed class AdResolver : IExternalDataResolver
{
    public string Name => "ad";

    public Task<string?> ResolveAsync(ExternalValueRequest request, CancellationToken cancellationToken = default)
    {
        // Placeholder for Active Directory lookup integration.
        _ = request;
        return Task.FromResult<string?>(null);
    }
}
