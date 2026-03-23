using System;
using Microsoft.Office.Tools.Outlook;

namespace Texty.OutlookVsto;

public partial class ThisAddIn
{
    private void InternalStartup()
    {
        Startup += ThisAddIn_Startup;
        Shutdown += ThisAddIn_Shutdown;
    }
}
