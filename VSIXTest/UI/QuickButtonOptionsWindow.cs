using Microsoft.VisualStudio.Shell;
using SharedClasses;
using System.Runtime.InteropServices;

namespace VSIXTest
{
    [Guid("743967b7-4ad8-4103-8a28-bf2933a5bdf5")]
    public class QuickButtonOptionsWindow : ToolWindowPane
    {
        public bool EventsAttached { get; set; }
        public QuickButtonOptionsControl OptionsControl { get; private set; }

        public QuickButtonOptionsWindow() : base(null)
        {
            this.Caption = "Quick Button Options";
            this.OptionsControl = new QuickButtonOptionsControl();
            this.Content = this.OptionsControl;
        }

        public void SetMessage(VsixUiMessage message)
        {
            this.OptionsControl.OriginalMessage = message;
        }
    }
}