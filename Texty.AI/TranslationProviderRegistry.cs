using Texty.Core.Interfaces;

namespace Texty.AI;

public sealed class TranslationProviderRegistry
{
    private readonly Dictionary<string, ITranslationProvider> _providers;

    /// <summary>
    /// Initialisiert eine Registry von Übersetzungsanbietern, indiziert nach dem Namen jedes Anbieters.
    /// </summary>
    /// <param name="providers">Eine Auflistung von ITranslationProvider; jeder Eintrag wird unter seinem <c>Name</c> abgelegt. Die Schlüsselerstellung verwendet einen fallunabhängigen Vergleich (OrdinalIgnoreCase), daher müssen die Namen eindeutig sein.</param>
    /// <exception cref="ArgumentNullException">Wenn <paramref name="providers"/> null ist.</exception>
    /// <exception cref="ArgumentException">Wenn mehrere Anbieter denselben Namen (ignoriere Groß-/Kleinschreibung) besitzen.</exception>
    public TranslationProviderRegistry(IEnumerable<ITranslationProvider> providers)
    {
        _providers = providers.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ermittelt den Übersetzungsanbieter mit dem angegebenen Namen.
    /// </summary>
    /// <param name="providerName">Name des Anbieters; die Suche erfolgt ohne Berücksichtigung der Groß-/Kleinschreibung.</param>
    /// <returns>Das gefundene <see cref="ITranslationProvider"/>, oder <c>null</c>, wenn kein Anbieter mit diesem Namen vorhanden ist.</returns>
    public ITranslationProvider? Get(string providerName)
    {
        _providers.TryGetValue(providerName, out var provider);
        return provider;
    }
}
