namespace Texty.OutlookAddin;

public sealed class OutlookAddinBridge
{
    private readonly GenderOMaticService _genderService;

    public OutlookAddinBridge(GenderOMaticService genderService)
    {
        _genderService = genderService;
    }

    public string BuildGreeting(OutlookRecipientContext recipient, string lastName)
    {
        var salutation = _genderService.DetermineSalutation(recipient);
        if (string.IsNullOrWhiteSpace(lastName))
        {
            return $"{salutation},";
        }

        return $"{salutation} {lastName},";
    }
}
