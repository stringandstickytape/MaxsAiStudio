using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using VSIXTest;
using System;

[Guid("743967b7-4ad8-4103-8a28-bf2933a5bdf4")]
public class ChatWindowPane : ToolWindowPane, IVsWindowFrameNotify3
{
    private VsixChat _webView;
    private System.Windows.Controls.TextBox _textBox;

    public ChatWindowPane() : base(null)
    {
        this.Caption = "Max's AI Studio";
        _webView = VsixChat.Instance; // or pass this in via constructor
        this.Content = new ChatWindowControl(_webView);

        // Create a textbox
        _textBox = new System.Windows.Controls.TextBox
        {
            Height = 80,
            AcceptsReturn = true,
            TextWrapping = System.Windows.TextWrapping.Wrap,
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            Visibility = System.Windows.Visibility.Collapsed
        };

        System.Windows.Controls.DockPanel dockPanel = new System.Windows.Controls.DockPanel();

        System.Windows.Controls.DockPanel.SetDock(_textBox, System.Windows.Controls.Dock.Bottom);
        dockPanel.Children.Add(_textBox);

        // Add the main content (webview) at the top
        dockPanel.Children.Add((System.Windows.FrameworkElement)this.Content);

        this.Content = dockPanel;
    }

    public int OnShow(int fShow)
    {
        _webView.Visibility = System.Windows.Visibility.Visible;
        return Microsoft.VisualStudio.VSConstants.S_OK;
    }

    public int OnMove(int x, int y, int w, int h) => Microsoft.VisualStudio.VSConstants.S_OK;

    public int OnSize(int x, int y, int w, int h) => Microsoft.VisualStudio.VSConstants.S_OK;

    public int OnDockableChange(int fDockable, int x, int y, int w, int h) => Microsoft.VisualStudio.VSConstants.S_OK;

    public int OnClose(ref uint pgrfSaveOptions) => Microsoft.VisualStudio.VSConstants.S_OK;

    public void UpdateTextBox(string text)
    {
        if (_textBox != null)
        {
            _textBox.Dispatcher.Invoke(() => _textBox.Text = text);
        }
    }
}
