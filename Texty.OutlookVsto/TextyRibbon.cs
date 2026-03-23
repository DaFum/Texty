using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Office.Core;

namespace Texty.OutlookVsto;

[ComVisible(true)]
public sealed class TextyRibbon : IRibbonExtensibility
{
    private readonly ThisAddIn _addin;

    public TextyRibbon(ThisAddIn addin)
    {
        _addin = addin ?? throw new ArgumentNullException(nameof(addin));
    }

    public string GetCustomUI(string ribbonId)
    {
        _ = ribbonId;
        var sb = new StringBuilder();
        sb.AppendLine("<customUI xmlns='http://schemas.microsoft.com/office/2009/07/customui'>");
        sb.AppendLine("  <ribbon>");
        sb.AppendLine("    <tabs>");
        sb.AppendLine("      <tab idMso='TabNewMailMessage'>");
        sb.AppendLine("        <group id='TextyGroupCompose' label='Texty'>");
        sb.AppendLine("          <button id='TextyInsertGreeting' label='Texty Anrede einfügen' size='large' imageMso='ContactInsert' onAction='OnInsertGreeting' />");
        sb.AppendLine("        </group>");
        sb.AppendLine("      </tab>");
        sb.AppendLine("      <tab idMso='TabMail'>");
        sb.AppendLine("        <group id='TextyGroupRead' label='Texty'>");
        sb.AppendLine("          <button id='TextyInsertGreetingRead' label='Texty Anrede einfügen' size='normal' imageMso='ContactInsert' onAction='OnInsertGreeting' />");
        sb.AppendLine("        </group>");
        sb.AppendLine("      </tab>");
        sb.AppendLine("    </tabs>");
        sb.AppendLine("  </ribbon>");
        sb.AppendLine("</customUI>");
        return sb.ToString();
    }

    // Called by Office Ribbon XML.
    public void OnInsertGreeting(IRibbonControl control)
    {
        _ = control;
        _addin.InsertGreetingIntoActiveInspector();
    }
}
