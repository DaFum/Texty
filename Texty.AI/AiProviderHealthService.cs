using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.AI;

public sealed class AiProviderHealthService
{
    private readonly AiProviderRegistry _registry;

    public AiProviderHealthService(AiProviderRegistry registry)
    {
        _registry = registry;
    }

    public async Task<IReadOnlyList<AiProviderHealthResult>> CheckAllAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<AiProviderHealthResult>();
        foreach (var name in _registry.Names)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var provider = _registry.Get(name);
            if (provider is null)
            {
                continue;
            }

            if (provider is IAiHealthCheckProvider healthCheckProvider)
            {
                try
                {
                    results.Add(await healthCheckProvider.CheckHealthAsync(cancellationToken));
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    results.Add(new AiProviderHealthResult(provider.Name, false, ex.ToString()));
                }
            }
            else
            {
                results.Add(new AiProviderHealthResult(provider.Name, false, "No health check implementation."));
            }
        }

        return results;
    }
}
