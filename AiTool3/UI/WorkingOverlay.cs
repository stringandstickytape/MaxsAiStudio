using System;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace AiTool3.UI
{
    public class WorkingOverlay : Control
    {
        private bool _isWorking = false;
        private WebView2 _webView;

        public WorkingOverlay(string message, bool softwareToysMode)
        {
            Message = message;
            SoftwareToysMode = softwareToysMode;
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = System.Drawing.Color.Transparent;

            _webView = new WebView2();
            _webView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
            Controls.Add(_webView);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                if (IsOnForm)
                {
                    cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT
                }
                return cp;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Do nothing to prevent painting the background if not on a form
            if (IsOnForm)
            {
                base.OnPaintBackground(e);
            }
        }
        public bool IsOnForm { get; set; }
        public string Message { get; set; }
        public bool SoftwareToysMode { get; set; }

        public bool IsWorking
        {
            get => _isWorking;
            set
            {
                if (_isWorking != value)
                {
                    _isWorking = value;
                    UpdateWebView(Message, SoftwareToysMode);
                    UpdateOverlay();
                }
            }
        }

        private async void UpdateWebView(string msg, bool softwareToysMode)
        {
            if (_isWorking)
            {
                await _webView.EnsureCoreWebView2Async();
                ResizeWebView();
                InjectHtmlAndJs(msg, softwareToysMode);
                _webView.Visible = true;
            }
            else
            {
                _webView.Visible = false;
            }
        }

        private void ResizeWebView()
        {
            _webView.Width = Width;
            _webView.Height = Height;
            _webView.Left = 0;
            _webView.Top = 0;
        }

        private void InjectHtmlAndJs(string msg, bool softwareToysMode)
        {
            msg = msg.ToUpper();
            var myRes = AssemblyHelper.GetEmbeddedAssembly("AiTool3.JavaScript.WorkingOverlay2.html");
            myRes = myRes.Replace("ABCDEFGHIJKLMNOPQRSTUVWXYZ", msg);
            if (softwareToysMode)
            {
                myRes = myRes.Replace("let dullMode = true;", "let dullMode = false;");
            }

            _webView.NavigateToString(myRes);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ResizeWebView();
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            if (!_isWorking) return;

            if (IsOnForm)
            {
                // For forms, use the existing painting logic
                base.OnPaint(e);

            }
            else
            {
                // For controls, paint on the parent's graphics

            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _webView?.Dispose();
            }
            base.Dispose(disposing);
        }
        public void UpdateOverlay()
        {
            if (IsOnForm)
            {
                Invalidate();
            }
            else
            {
                Parent?.Invalidate(new Rectangle(Left, Top, Width, Height), true);
            }
        }
    }
}