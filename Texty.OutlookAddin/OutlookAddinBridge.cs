namespace Texty.OutlookAddin;

public sealed class OutlookAddinBridge
{
    private readonly GenderOMaticService _genderService;

    public OutlookAddinBridge(GenderOMaticService genderService)
    {
        _genderService = genderService ?? throw new ArgumentNullException(nameof(genderService));
    }

    public string BuildGreeting(OutlookRecipientContext? recipient, string? lastName)
    {
        var salutation = _genderService.DetermineSalutation(recipient);
        var resolvedLastName = ResolveLastName(lastName, recipient);
        if (string.IsNullOrWhiteSpace(resolvedLastName))
        {
            return $"{salutation},";
        }

        return $"{salutation} {resolvedLastName},";
    }

    public OutlookRecipientContext BuildRecipientContext(
        string? displayName,
        string? emailAddress,
        string? explicitGender,
        string? firstName,
        string? lastName,
        string? company)
    {
        return new OutlookRecipientContext(
            displayName?.Trim(),
            emailAddress?.Trim(),
            explicitGender?.Trim(),
            firstName?.Trim(),
            lastName?.Trim(),
            company?.Trim());
    }

    private static string? ResolveLastName(string? lastName, OutlookRecipientContext? recipient)
    {
        if (!string.IsNullOrWhiteSpace(lastName))
        {
            return lastName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(recipient?.LastName))
        {
            return recipient.LastName.Trim();
        }

        return null;
    }
}
