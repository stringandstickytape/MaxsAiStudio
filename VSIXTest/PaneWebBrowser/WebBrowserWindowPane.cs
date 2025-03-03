using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;

namespace VSIXTest.PaneWebBrowser
{
    [Guid("743967b7-4ad8-4103-8a28-bf2933a5bdf8")] // Unique GUID for WebBrowserWindowPane
    public class WebBrowserWindowPane : ToolWindowPane
    {
        public WebBrowserWindowPane() : base(null)
        {
            this.Caption = "Max's AI Studio Web Browser";
            this.Content = new WebBrowserWindowControl();
        }
    }
}