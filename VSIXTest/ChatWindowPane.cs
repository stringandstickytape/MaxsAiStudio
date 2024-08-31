using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using VSIXTest;

[Guid("743967b7-4ad8-4103-8a28-bf2933a5bdf4")]
public class ChatWindowPane : ToolWindowPane, IVsWindowFrameNotify3
{
    private VsixChat _webView;

    public ChatWindowPane() : base(null)
    {
        this.Caption = "AI Chat";
        _webView = VsixChat.Instance;
        this.Content = new ChatWindowControl(_webView);
    }

    public int OnShow(int fShow)
    {
        _webView.Visibility = System.Windows.Visibility.Visible;
        return Microsoft.VisualStudio.VSConstants.S_OK;
    }

    public int OnMove(int x, int y, int w, int h)
    {
        return Microsoft.VisualStudio.VSConstants.S_OK;
    }

    public int OnSize(int x, int y, int w, int h)
    {
        return Microsoft.VisualStudio.VSConstants.S_OK;
    }

    public int OnDockableChange(int fDockable, int x, int y, int w, int h)
    {
        return Microsoft.VisualStudio.VSConstants.S_OK;
    }

    public int OnClose(ref uint pgrfSaveOptions)
    {
        return Microsoft.VisualStudio.VSConstants.S_OK;
    }
}