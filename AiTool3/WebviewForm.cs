using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AiTool3.UI.NetworkDiagramControl;

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
                if (inlineWebView.InvokeRequired)
                {
                    inlineWebView.Invoke(new Action(() => inlineWebView.NavigateToString(SnipperHelper.StripFirstAndLastLine(_code))));
                }
                else
                {
                    inlineWebView.NavigateToString(SnipperHelper.StripFirstAndLastLine(_code));
                };
            }
            
            );

            
            


        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }
    }
}
