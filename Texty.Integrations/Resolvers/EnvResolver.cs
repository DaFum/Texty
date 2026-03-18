using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Integrations.Resolvers;

public sealed class EnvResolver : IExternalDataResolver
{
    public string Name => "env";

    /// <summary>
    /// Löst den Wert einer Umgebungsvariable anhand des im Request angegebenen Ausdrucks.
    /// </summary>
    /// <param name="request">Anfrageobjekt; dessen Expression wird als Name der Umgebungsvariable verwendet.</param>
    /// <returns>Der Wert der Umgebungsvariable als Zeichenfolge, oder <c>null</c> wenn der Ausdruck leer ist oder die Variable nicht existiert.</returns>
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
