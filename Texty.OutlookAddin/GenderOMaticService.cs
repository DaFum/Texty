namespace Texty.OutlookAddin;

public sealed class GenderOMaticService
{
    public string DetermineSalutation(OutlookRecipientContext recipient)
    {
        if (!string.IsNullOrWhiteSpace(recipient.ExplicitGender))
        {
            return recipient.ExplicitGender.Equals("female", StringComparison.OrdinalIgnoreCase)
                ? "Sehr geehrte Frau"
                : recipient.ExplicitGender.Equals("male", StringComparison.OrdinalIgnoreCase)
                    ? "Sehr geehrter Herr"
                    : "Guten Tag";
        }
        // Do not infer gender from DisplayName-only heuristics.
        return "Guten Tag";
    }
}
