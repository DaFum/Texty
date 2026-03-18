using Texty.Core.Interfaces;

namespace Texty.AI;

public sealed class AiProviderRegistry
{
    private readonly Dictionary<string, IAiProvider> _providers;

    public AiProviderRegistry(IEnumerable<IAiProvider> providers)
    {
        _providers = providers.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IAiProvider? Get(string providerName)
    {
        _providers.TryGetValue(providerName, out var provider);
        return provider;
    }

    public IReadOnlyList<string> Names => _providers.Keys.Order().ToList();
}
