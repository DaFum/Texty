namespace Texty.OutlookAddin;

/// <summary>
/// Helper for VSTO hosts that map Outlook interop recipient values
/// into the runtime-agnostic Texty recipient context.
/// </summary>
public static class OutlookVstoRecipientMapper
{
    public static OutlookRecipientContext FromInteropValues(
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

    public static OutlookRecipientContext FromDynamicRecipient(object? recipient)
    {
        if (recipient is null)
        {
            return new OutlookRecipientContext(null, null, null);
        }

        string? displayName = null;
        string? emailAddress = null;

        try
        {
            displayName = Convert.ToString(recipient.GetType().GetProperty("Name")?.GetValue(recipient));
        }
        catch
        {
            // ignored by design
        }

        try
        {
            emailAddress = Convert.ToString(recipient.GetType().GetProperty("Address")?.GetValue(recipient));
        }
        catch
        {
            // ignored by design
        }

        return new OutlookRecipientContext(displayName?.Trim(), emailAddress?.Trim(), null);
    }
}
