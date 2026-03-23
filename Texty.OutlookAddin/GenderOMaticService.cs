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
        if (string.IsNullOrWhiteSpace(explicitGender))
        {
            // Do not infer gender from DisplayName-only heuristics.
            return "Guten Tag";
        }

        return explicitGender.ToLowerInvariant() switch
        {
            "female" => "Sehr geehrte Frau",
            "male" => "Sehr geehrter Herr",
            _ => "Guten Tag",
        };
    }
}
