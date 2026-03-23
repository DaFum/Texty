using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Security;

public sealed class RolePolicyService : IRolePolicyService
{
    private readonly IReadOnlyDictionary<string, RoleName> _roleAssignments;

    public RolePolicyService(IReadOnlyDictionary<string, RoleName> roleAssignments)
    {
        _roleAssignments = roleAssignments;
    }

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
