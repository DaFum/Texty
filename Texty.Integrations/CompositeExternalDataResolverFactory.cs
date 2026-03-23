using Texty.Core.Interfaces;

namespace Texty.Integrations;

public sealed class CompositeExternalDataResolverFactory : IExternalDataResolverFactory
{
    private readonly Dictionary<string, IExternalDataResolver> _resolvers;

    public CompositeExternalDataResolverFactory(IEnumerable<IExternalDataResolver> resolvers)
    {
        _resolvers = resolvers.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IExternalDataResolver? GetResolver(string resolverName)
    {
        _resolvers.TryGetValue(resolverName, out var resolver);
        return resolver;
    }
}
