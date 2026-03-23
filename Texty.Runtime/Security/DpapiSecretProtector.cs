using System.Security.Cryptography;
using System.Text;
using Texty.Core.Interfaces;

namespace Texty.Runtime.Security;

public sealed class DpapiSecretProtector : ISecretProtector
{
    public string Protect(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    public string Unprotect(string protectedValue)
    {
        var protectedBytes = Convert.FromBase64String(protectedValue);
        var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }
}
