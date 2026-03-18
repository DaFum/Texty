using Texty.Core.Interfaces;

namespace Texty.AI;

public sealed class AiProviderRegistry
{
    private readonly Dictionary<string, IAiProvider> _providers;

    /// <summary>
    /// Erstellt ein Register, das die übergebenen IAiProvider-Instanzen unter ihrem <c>Name</c>-Wert speichert (Schlüsselvergleich ist case-insensitive).
    /// </summary>
    /// <param name="providers">Auflistung von IAiProvider-Instanzen, die in das Register aufgenommen werden sollen.</param>
    /// <exception cref="ArgumentNullException">Wenn <paramref name="providers"/> null ist.</exception>
    /// <exception cref="ArgumentException">Wenn mehrere Provider denselben Namen (unabhängig von Groß-/Kleinschreibung) besitzen.</exception>
    public AiProviderRegistry(IEnumerable<IAiProvider> providers)
    {
        _providers = providers.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Findet den registrierten AI-Provider für den angegebenen Namen.
    /// </summary>
    /// <param name="providerName">Der Name des Providers. Die Suche ignoriert Groß-/Kleinschreibung.</param>
    /// <returns>Die gefundene <see cref="IAiProvider"/>-Instanz, oder <c>null</c>, wenn kein Provider mit diesem Namen existiert.</returns>
    public IAiProvider? Get(string providerName)
    {
        _providers.TryGetValue(providerName, out var provider);
        return provider;
    }

    public IReadOnlyList<string> Names => _providers.Keys.Order().ToList();
}
