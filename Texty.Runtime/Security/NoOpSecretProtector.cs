using Texty.Core.Interfaces;

namespace Texty.Runtime.Security;

public sealed class NoOpSecretProtector : ISecretProtector
{
    public string Protect(string plainText) => plainText;

    public string Unprotect(string protectedValue) => protectedValue;
}
