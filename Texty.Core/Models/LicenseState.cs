namespace Texty.Core.Models;

public sealed record LicenseState(
    bool IsLicensed,
    string Tier,
    DateTimeOffset? ExpiresUtc,
    int ActiveUsers);
