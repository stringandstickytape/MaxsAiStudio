using System.Windows.Controls;

namespace VSIXTest
{
    public partial class DebugWindowControl : UserControl
    {
        public DebugWindowControl()
        {
            InitializeComponent();
            DataContext = VsixDebugLog.Instance;

            VsixDebugLog.Instance.LogMessageAdded += (sender, message) =>
            {
                LogListBox.Items.MoveCurrentToLast();
                LogListBox.ScrollIntoView(LogListBox.Items.CurrentItem);
            };
        }
    }
}