using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Integrations.Resolvers;

public sealed class AdResolver : IExternalDataResolver
{
    public string Name => "ad";

    /// <summary>
    /// Versucht, aus Active Directory einen String-Wert für die angegebene Anfrage zu ermitteln.
    /// </summary>
    /// <param name="request">Anfrage-Objekt mit Informationen, welche externen Wert aufgelöst werden soll.</param>
    /// <param name="cancellationToken">Token zum Abbrechen der Operation.</param>
    /// <returns>`null` (derzeit keine Auflösung; Platzhalterimplementierung liefert immer `null`).</returns>
    public Task<string?> ResolveAsync(ExternalValueRequest request, CancellationToken cancellationToken = default)
    {
        // Placeholder for Active Directory lookup integration.
        _ = request;
        return Task.FromResult<string?>(null);
    }
}
