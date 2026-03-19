using System.Diagnostics;
using Microsoft.Office.Core;
using Outlook = Microsoft.Office.Interop.Outlook;
using Texty.OutlookAddin;

namespace Texty.OutlookVsto;

public partial class ThisAddIn
{
    private readonly OutlookAddinBridge _bridge = new(new GenderOMaticService());
    private Outlook.Inspectors? _inspectors;
    private Outlook.Explorers? _explorers;

    internal static ThisAddIn? Current { get; private set; }

    protected override IRibbonExtensibility CreateRibbonExtensibilityObject()
    {
        return new TextyRibbon(this);
    }

    private void ThisAddIn_Startup(object sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        Current = this;

        _inspectors = Application.Inspectors;
        _explorers = Application.Explorers;

        _inspectors.NewInspector += OnNewInspector;
        _explorers.NewExplorer += OnNewExplorer;
        Application.ItemSend += OnItemSend;
    }

    private void ThisAddIn_Shutdown(object sender, EventArgs e)
    {
        _ = sender;
        _ = e;

        try
        {
            Application.ItemSend -= OnItemSend;
            if (_inspectors is not null)
            {
                _inspectors.NewInspector -= OnNewInspector;
            }

            if (_explorers is not null)
            {
                _explorers.NewExplorer -= OnNewExplorer;
            }
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Outlook add-in shutdown unsubscribing failed: {ex.Message}");
        }
        finally
        {
            Current = null;
        }
    }

    internal void InsertGreetingIntoActiveInspector()
    {
        try
        {
            var inspector = Application.ActiveInspector();
            if (inspector?.CurrentItem is not Outlook.MailItem mailItem)
            {
                return;
            }

            var greeting = BuildGreeting(mailItem);
            if (string.IsNullOrWhiteSpace(greeting))
            {
                return;
            }

            var existingBody = mailItem.Body ?? string.Empty;
            if (!existingBody.StartsWith(greeting, StringComparison.OrdinalIgnoreCase))
            {
                mailItem.Body = greeting + Environment.NewLine + Environment.NewLine + existingBody;
            }
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"InsertGreetingIntoActiveInspector failed: {ex.Message}");
        }
    }

    private void OnNewInspector(Outlook.Inspector inspector)
    {
        _ = inspector;
        // Hook reserved for future context-aware command updates.
    }

    private void OnNewExplorer(Outlook.Explorer explorer)
    {
        _ = explorer;
        // Hook reserved for explorer-context commands.
    }

    private void OnItemSend(object item, ref bool cancel)
    {
        if (item is not Outlook.MailItem mailItem)
        {
            return;
        }

        try
        {
            var greeting = BuildGreeting(mailItem);
            if (string.IsNullOrWhiteSpace(greeting))
            {
                return;
            }

            var body = mailItem.Body ?? string.Empty;
            if (!body.StartsWith(greeting, StringComparison.OrdinalIgnoreCase))
            {
                mailItem.Body = greeting + Environment.NewLine + Environment.NewLine + body;
            }
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"OnItemSend greeting injection failed: {ex.Message}");
            cancel = false;
        }
    }

    private string BuildGreeting(Outlook.MailItem mailItem)
    {
        Outlook.Recipient? recipient = null;
        try
        {
            if (mailItem.Recipients?.Count > 0)
            {
                recipient = mailItem.Recipients[1];
            }
        }
        catch
        {
            // ignored
        }

        var displayName = SafeGet(() => recipient?.Name);
        var email = SafeGet(() => recipient?.Address);
        var firstName = ExtractFirstName(displayName);
        var lastName = ExtractLastName(displayName);
        var context = _bridge.BuildRecipientContext(displayName, email, explicitGender: null, firstName, lastName, company: null);
        return _bridge.BuildGreeting(context, lastName);
    }

    private static string? ExtractFirstName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        var parts = displayName.Split([' ', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? null : parts[0];
    }

    private static string? ExtractLastName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        var parts = displayName.Split([' ', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length < 2 ? null : parts[^1];
    }

    private static string? SafeGet(Func<string?> accessor)
    {
        try
        {
            return accessor();
        }
        catch
        {
            return null;
        }
    }
}
