namespace Texty.Core.Models;

public sealed record AuthContext(
    string UserId,
    string DisplayName,
    IReadOnlyList<string> GroupIds,
    bool IsEntraBacked);
