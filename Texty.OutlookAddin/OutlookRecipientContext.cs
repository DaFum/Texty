namespace Texty.OutlookAddin;

public sealed record OutlookRecipientContext(
    string DisplayName,
    string EmailAddress,
    string? ExplicitGender);
