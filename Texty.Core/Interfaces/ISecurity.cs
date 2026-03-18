namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface IAuthContextProvider
{
    /// <summary>
/// Ermittelt den AuthContext für den aktuellen Aufrufer.
/// </summary>
/// <param name="cancellationToken">Token zum Abbrechen des Vorgangs.</param>
/// <returns>Der ermittelte AuthContext.</returns>
Task<AuthContext> GetContextAsync(CancellationToken cancellationToken = default);
}

public interface IRolePolicyService
{
    /// <summary>
/// Ermittelt, ob der angegebene AuthContext Zugriff auf die benannte Ressource mit der geforderten Rolle besitzt.
/// </summary>
/// <param name="context">Authentifizierungs- und Autorisierungsinformationen des anfragenden Benutzers.</param>
/// <param name="resourceId">Bezeichner der Zielressource (z. B. Objekt‑ oder Mandanten‑ID).</param>
/// <param name="requiredRole">Die für den Zugriff notwendige Rolle.</param>
/// <returns>`true` wenn der Kontext die erforderliche Rolle für die Ressource besitzt, `false` sonst.</returns>
bool CanAccess(AuthContext context, string resourceId, RoleName requiredRole);
}

public interface ILicenseService
{
    /// <summary>
/// Ruft asynchron den aktuellen Lizenzstatus ab.
/// </summary>
/// <returns>Ein <see cref="LicenseState"/>-Objekt, das den aktuellen Lizenzstatus beschreibt.</returns>
Task<LicenseState> GetCurrentAsync(CancellationToken cancellationToken = default);
    /// <summary>
/// Validiert die aktuelle Lizenz.
— </summary>
/// <returns>`true` wenn die Lizenz gültig ist, `false` sonst.</returns>
Task<bool> ValidateAsync(CancellationToken cancellationToken = default);
}

public interface ISecretProtector
{
    /// <summary>
/// Schützt einen Klartextstring für sichere Speicherung oder Übertragung.
/// </summary>
/// <param name="plainText">Der zu schützende Klartext.</param>
/// <returns>Der geschützte String (verschlüsselter/transformierter Wert).</returns>
string Protect(string plainText);
    /// <summary>
/// Stellt den ursprünglichen Klartext aus einem zuvor geschützten Wert wieder her.
/// </summary>
/// <param name="protectedValue">Der geschützte Wert, der zurück in Klartext konvertiert werden soll.</param>
/// <returns>Der ungeschützte Klartext.</returns>
string Unprotect(string protectedValue);
}
