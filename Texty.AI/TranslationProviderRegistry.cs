using Texty.Core.Interfaces;

namespace Texty.AI;

public sealed class TranslationProviderRegistry
{
    private readonly Dictionary<string, ITranslationProvider> _providers;

    public TranslationProviderRegistry(IEnumerable<ITranslationProvider> providers)
    {
        _providers = providers.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
    }

    public ITranslationProvider? Get(string providerName)
    {
        _providers.TryGetValue(providerName, out var provider);
        return provider;
    }
}
