namespace Texty.OutlookAddin;

public sealed class OutlookAddinBridge
{
    private readonly GenderOMaticService _genderService;

    /// <summary>
    /// Initialisiert eine neue Instanz von <see cref="OutlookAddinBridge"/> mit dem angegebenen GenderOMaticService.
    /// </summary>
    /// <param name="genderService">Service zum Ermitteln der Anrede eines Empfängers.</param>
    public OutlookAddinBridge(GenderOMaticService genderService)
    {
        _genderService = genderService;
    }

    /// <summary>
    /// Erstellt eine Anredezeile für einen Empfänger basierend auf dessen Kontext und optionalem Nachnamen.
    /// </summary>
    /// <param name="recipient">Kontextinformationen des Empfängers, die zur Bestimmung der Anrede verwendet werden.</param>
    /// <param name="lastName">Der Nachname des Empfängers; wird weggelassen wenn null, leer oder nur Leerzeichen.</param>
    /// <returns>Die Anrede gefolgt von optionalem Nachnamen und einem abschließenden Komma (z. B. "Herr Müller," oder "Frau,").</returns>
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
