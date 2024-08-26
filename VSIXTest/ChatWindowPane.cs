using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace VSIXTest
{
    [Guid("743967b7-4ad8-4103-8a28-bf2933a5bdf4")]
    public class ChatWindowPane : ToolWindowPane
    {
        public ChatWindowPane() : base(null)
        {
            this.Caption = "AI Chat";
            this.Content = new ChatWindowControl();
        }
    }
}