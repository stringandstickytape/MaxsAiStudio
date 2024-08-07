using AiTool3.ExtensionMethods;
using AiTool3.Helpers;

namespace AiTool3
{
    public partial class WebviewForm : Form
    {
        private string _code;
        public WebviewForm(string code)
        {
            _code = code;

            InitializeComponent();
            inlineWebView.EnsureCoreWebView2Async(null, null);

            // do this with invoke if necc:
            Task.Delay(1000).ContinueWith(t =>
            {
                this.InvokeIfNeeded(() =>
                {
                    inlineWebView.NavigateToString(SnippetHelper.StripFirstAndLastLine(_code));
                });
            }
            );
        }
    }
}
