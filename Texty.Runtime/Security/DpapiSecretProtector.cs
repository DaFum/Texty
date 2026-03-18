using System.Security.Cryptography;
using System.Text;
using Texty.Core.Interfaces;

namespace Texty.Runtime.Security;

public sealed class DpapiSecretProtector : ISecretProtector
{
    /// <summary>
    /// Schützt einen Klartextstring mithilfe von Windows DPAPI für den aktuellen Benutzer.
    /// </summary>
    /// <param name="plainText">Der zu schützende Klartext.</param>
    /// <returns>Base64-kodierte Darstellung der mit DPAPI geschützten Bytes.</returns>
    public string Protect(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    /// <summary>
    /// Entschlüsselt einen Base64-kodierten Wert, der mit DPAPI (DataProtectionScope.CurrentUser) geschützt wurde, und liefert den Klartext.
    /// </summary>
    /// <param name="protectedValue">Der Base64-kodierte, DPAPI-geschützte Wert.</param>
    /// <returns>Der entschlüsselte Klartext.</returns>
    public string Unprotect(string protectedValue)
    {
        var protectedBytes = Convert.FromBase64String(protectedValue);
        var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }
}
