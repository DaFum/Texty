using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Security;

public sealed class LocalAuthContextProvider : IAuthContextProvider
{
    /// <summary>
    /// Erstellt ein AuthContext basierend auf dem aktuell angemeldeten Windows-Benutzer und dessen Domäne.
    /// </summary>
    /// <param name="cancellationToken">Wird akzeptiert, in dieser Implementierung jedoch nicht verwendet.</param>
    /// <returns>Ein AuthContext mit dem Principal im Format "Domäne\Benutzer", dem Benutzernamen, einer leeren Gruppensammlung und dem Flag `false`.</returns>
    public Task<AuthContext> GetContextAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var user = Environment.UserName;
        var domain = Environment.UserDomainName;
        return Task.FromResult(new AuthContext($"{domain}\\{user}", user, [], false));
    }
}
