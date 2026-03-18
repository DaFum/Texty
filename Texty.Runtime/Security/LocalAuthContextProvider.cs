using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Security;

public sealed class LocalAuthContextProvider : IAuthContextProvider
{
    public Task<AuthContext> GetContextAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var user = Environment.UserName;
        var domain = Environment.UserDomainName;
        return Task.FromResult(new AuthContext($"{domain}\\{user}", user, [], false));
    }
}
