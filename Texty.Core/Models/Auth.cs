namespace Texty.Core.Models;

public sealed record AuthContext
{
    public AuthContext(
        string userId,
        string displayName,
        IEnumerable<string>? groupIds,
        bool isEntraBacked)
    {
        UserId = userId;
        DisplayName = displayName;
        GroupIds = groupIds?.ToArray() ?? Array.Empty<string>();
        IsEntraBacked = isEntraBacked;
    }

    public string UserId { get; }

    public string DisplayName { get; }

    public IReadOnlyList<string> GroupIds { get; }

    public bool IsEntraBacked { get; }
}
