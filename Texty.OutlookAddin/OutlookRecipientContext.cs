namespace Texty.OutlookAddin;

public sealed record OutlookRecipientContext(
    string? DisplayName,
    string? EmailAddress,
    string? ExplicitGender,
    string? FirstName = null,
    string? LastName = null,
    string? Company = null);
