using Texty.Core.Interfaces;

namespace Texty.Integrations;

public sealed class CompositeExternalDataResolverFactory : IExternalDataResolverFactory
{
    private readonly Dictionary<string, IExternalDataResolver> _resolvers;

    /// <summary>
    /// Initialisiert eine CompositeExternalDataResolverFactory mit einer Sammlung von External-Data-Resolvern.
    /// </summary>
    /// <param name="resolvers">Die zu registrierenden Resolver; sie werden in ein Wörterbuch übernommen und nach ihrer `Name`-Eigenschaft (case-insensitive) indiziert.</param>
    public CompositeExternalDataResolverFactory(IEnumerable<IExternalDataResolver> resolvers)
    {
        _resolvers = resolvers.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gibt den registrierten externen Datenauflöser mit dem angegebenen Namen zurück.
    /// </summary>
    /// <param name="resolverName">Der Name des gesuchten Resolvers, wie er bei der Registrierung verwendet wurde; die Suche ist groß-/kleinschreibungsunabhängig.</param>
    /// <returns>Das passende <see cref="IExternalDataResolver"/>, oder <c>null</c>, wenn kein Resolver mit diesem Namen gefunden wurde.</returns>
    public IExternalDataResolver? GetResolver(string resolverName)
    {
        _resolvers.TryGetValue(resolverName, out var resolver);
        return resolver;
    }
}
