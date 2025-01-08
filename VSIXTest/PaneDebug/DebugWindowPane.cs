using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;

namespace VSIXTest
{
    [Guid("743967b7-4ad8-4103-8a28-bf2933a5bdf7")] // Unique GUID for DebugWindowPane
    public class DebugWindowPane : ToolWindowPane
    {
        public DebugWindowPane() : base(null)
        {
            this.Caption = "Max's AI Studio Debug";
            this.Content = new DebugWindowControl();
        }
    }
}