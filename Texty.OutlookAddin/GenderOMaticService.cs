namespace Texty.OutlookAddin;

public sealed class GenderOMaticService
{
    public string DetermineSalutation(OutlookRecipientContext? recipient)
    {
        if (recipient is null)
        {
            return "Guten Tag";
        }

        var explicitGender = recipient.ExplicitGender?.Trim();
        if (!string.IsNullOrWhiteSpace(explicitGender))
        {
            return explicitGender.Equals("female", StringComparison.OrdinalIgnoreCase)
                ? "Sehr geehrte Frau"
                : explicitGender.Equals("male", StringComparison.OrdinalIgnoreCase)
                    ? "Sehr geehrter Herr"
                    : "Guten Tag";
        }

        // Do not infer gender from DisplayName-only heuristics.
        return "Guten Tag";
    }
}
