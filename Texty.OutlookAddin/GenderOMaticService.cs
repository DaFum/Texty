namespace Texty.OutlookAddin;

public sealed class GenderOMaticService
{
    private static readonly HashSet<string> FemaleNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "anna",
        "maria",
        "julia",
        "sophia",
        "emilia",
    };

    private static readonly HashSet<string> MaleNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "thomas",
        "michael",
        "andreas",
        "jan",
        "lukas",
    };

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

        var firstName = recipient.DisplayName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? string.Empty;

        if (FemaleNames.Contains(firstName))
        {
            return "Sehr geehrte Frau";
        }

        if (MaleNames.Contains(firstName))
        {
            return "Sehr geehrter Herr";
        }

        return "Guten Tag";
    }
}
