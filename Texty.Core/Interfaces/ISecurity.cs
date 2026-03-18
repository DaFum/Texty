namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface IAuthContextProvider
{
    Task<AuthContext> GetContextAsync(CancellationToken cancellationToken = default);
}

public interface IRolePolicyService
{
    bool CanAccess(AuthContext context, string resourceId, RoleName requiredRole);
}

public interface ILicenseService
{
    Task<LicenseState> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task<bool> ValidateAsync(CancellationToken cancellationToken = default);
}

public interface ISecretProtector
{
    string Protect(string plainText);
    string Unprotect(string protectedValue);
}
