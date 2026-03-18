using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Security;

public sealed class RolePolicyService : IRolePolicyService
{
    private readonly IReadOnlyDictionary<string, RoleName> _roleAssignments;

    /// <summary>
    /// Initialisiert eine neue Instanz von <see cref="RolePolicyService"/> mit den angegebenen Rollen-Zuordnungen.
    /// </summary>
    /// <param name="roleAssignments">Ein Wörterbuch, das Benutzer-IDs (string) auf die zugewiesenen <see cref="RoleName"/> abbildet und zur Zugriffskontrolle verwendet wird.</param>
    public RolePolicyService(IReadOnlyDictionary<string, RoleName> roleAssignments)
    {
        _roleAssignments = roleAssignments;
    }

    /// <summary>
    /// Ermittelt, ob der angegebene Benutzer für die spezifizierte Ressource die erforderliche Rolle besitzt.
    /// </summary>
    /// <param name="context">Authentifizierungskontext des Benutzers (enthält die UserId).</param>
    /// <param name="resourceId">Die Kennung der Ressource, auf die zugegriffen werden soll.</param>
    /// <param name="requiredRole">Die Rolle, die mindestens vorhanden sein muss, um Zugriff zu erhalten.</param>
    /// <returns>`true`, wenn dem Benutzer eine Rolle zugewiesen ist, die mindestens die Rechte der geforderten Rolle umfasst, oder wenn keine Rolle zugewiesen ist und `requiredRole` `RoleName.Reader` ist; `false` sonst.</returns>
    public bool CanAccess(AuthContext context, string resourceId, RoleName requiredRole)
    {
        _ = resourceId;
        if (_roleAssignments.TryGetValue(context.UserId, out var assigned))
        {
            return assigned <= requiredRole;
        }

        return requiredRole == RoleName.Reader;
    }
}
