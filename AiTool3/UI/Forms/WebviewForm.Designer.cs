namespace AiTool3
{
    partial class WebviewForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            inlineWebView = new Microsoft.Web.WebView2.WinForms.WebView2();
            ((System.ComponentModel.ISupportInitialize)inlineWebView).BeginInit();
            SuspendLayout();
            // 
            // inlineWebView
            // 
            inlineWebView.AllowExternalDrop = true;
            inlineWebView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            inlineWebView.CreationProperties = null;
            inlineWebView.DefaultBackgroundColor = Color.White;
            inlineWebView.Location = new Point(0, 0);
            inlineWebView.Name = "inlineWebView";
            inlineWebView.Size = new Size(800, 449);
            inlineWebView.TabIndex = 0;
            inlineWebView.ZoomFactor = 1D;
            // 
            // WebviewForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(inlineWebView);
            Name = "WebviewForm";
            Text = "WebviewForm";
            ((System.ComponentModel.ISupportInitialize)inlineWebView).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 inlineWebView;
    }
}