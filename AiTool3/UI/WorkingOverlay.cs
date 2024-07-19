using System;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace AiTool3.UI
{
    public class WorkingOverlay : Control
    {
        private WebView2 _webView;
        private bool _isWorking = false;

        public WorkingOverlay()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.FromArgb(128, Color.White);
        }

        public bool IsWorking
        {
            get => _isWorking;
            set
            {
                if (_isWorking != value)
                {
                    _isWorking = value;
                    UpdateWebView();
                    Invalidate();
                }
            }
        }

        private async void UpdateWebView()
        {
            if (_isWorking)
            {
                if (_webView == null)
                {
                    _webView = new WebView2();
                    await _webView.EnsureCoreWebView2Async();
                    Controls.Add(_webView);
                }
                ResizeWebView();
                InjectHtmlAndJs();
            }
            else
            {
                if (_webView != null)
                {
                    Controls.Remove(_webView);
                    _webView.Dispose();
                    _webView = null;
                }
            }
        }

        private void ResizeWebView()
        {
            if (_webView != null)
            {
                _webView.Width = Width;
                _webView.Height = Height;
                _webView.Left = 0;
                _webView.Top = 0;
            }
        }

        private void InjectHtmlAndJs()
        {
            string html = @"
                <html>
                <head>
                    <style>
                        body { 
                            display: flex; 
                            justify-content: center; 
                            align-items: center; 
                            height: 100vh; 
                            margin: 0; 
                            background-color: rgba(255, 255, 255, 0.5);
                        }
                        .loader {
                            border: 16px solid #f3f3f3;
                            border-top: 16px solid #3498db;
                            border-radius: 50%;
                            width: 120px;
                            height: 120px;
                            animation: spin 2s linear infinite;
                        }
                        @keyframes spin {
                            0% { transform: rotate(0deg); }
                            100% { transform: rotate(360deg); }
                        }
                    </style>
                </head>
                <body>
                    <div class='loader'></div>
                    <script>
                        // You can add any JavaScript here if needed
                    </script>
                </body>
                </html>";

            _webView.NavigateToString(html);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ResizeWebView();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_isWorking)
            {
                using (var brush = new SolidBrush(ForeColor))
                {
                    var stringFormat = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    e.Graphics.DrawString("Working...", Font, brush, ClientRectangle, stringFormat);
                }
            }
        }
    }
}