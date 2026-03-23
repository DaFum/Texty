namespace Texty.Core.Models;

public sealed record RoleAssignment(
    string PrincipalId,
    RoleName Role,
    bool IsEntraGroup);
