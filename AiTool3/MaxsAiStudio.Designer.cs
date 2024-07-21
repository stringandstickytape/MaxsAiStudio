using AiTool3.UI;

namespace AiTool3
{
    partial class MaxsAiStudio
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
            cbEngine = new ComboBox();
            splitContainer1 = new SplitContainer();
            splitContainer5 = new SplitContainer();
            btnClearSearch = new Button();
            tbSearch = new TextBox();
            dgvConversations = new DataGridView();
            ndcWeb = new Microsoft.Web.WebView2.WinForms.WebView2();
            cbUseEmbeddings = new CheckBox();
            panel1 = new Panel();
            chatWebView = new ChatWebView();
            btnProjectHelper = new Button();
            label2 = new Label();
            label1 = new Label();
            cbSummaryEngine = new ComboBox();
            button1 = new Button();
            buttonStartRecording = new Button();
            buttonAttachImage = new Button();
            menuBar = new MenuStrip();
            toolStripMenuItem1 = new ToolStripMenuItem();
            statusStrip1 = new StatusStrip();
            tokenUsageLabel = new ToolStripStatusLabel();
            button2 = new Button();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer5).BeginInit();
            splitContainer5.Panel1.SuspendLayout();
            splitContainer5.Panel2.SuspendLayout();
            splitContainer5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvConversations).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ndcWeb).BeginInit();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)chatWebView).BeginInit();
            menuBar.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // cbEngine
            // 
            cbEngine.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cbEngine.BackColor = Color.Black;
            cbEngine.Font = new Font("Segoe UI", 12F);
            cbEngine.ForeColor = Color.White;
            cbEngine.FormattingEnabled = true;
            cbEngine.Location = new Point(732, 4);
            cbEngine.Margin = new Padding(2);
            cbEngine.Name = "cbEngine";
            cbEngine.Size = new Size(291, 29);
            cbEngine.TabIndex = 3;
            cbEngine.SelectedIndexChanged += cbEngine_SelectedIndexChanged;
            // 
            // splitContainer1
            // 
            splitContainer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer1.Location = new Point(10, 24);
            splitContainer1.Margin = new Padding(2);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(splitContainer5);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(button2);
            splitContainer1.Panel2.Controls.Add(cbUseEmbeddings);
            splitContainer1.Panel2.Controls.Add(panel1);
            splitContainer1.Panel2.Controls.Add(btnProjectHelper);
            splitContainer1.Panel2.Controls.Add(label2);
            splitContainer1.Panel2.Controls.Add(label1);
            splitContainer1.Panel2.Controls.Add(cbSummaryEngine);
            splitContainer1.Panel2.Controls.Add(button1);
            splitContainer1.Panel2.Controls.Add(cbEngine);
            splitContainer1.Panel2.Controls.Add(buttonStartRecording);
            splitContainer1.Panel2.Controls.Add(buttonAttachImage);
            splitContainer1.Panel2MinSize = 1000;
            splitContainer1.Size = new Size(1363, 672);
            splitContainer1.SplitterDistance = 336;
            splitContainer1.SplitterWidth = 3;
            splitContainer1.TabIndex = 9;
            // 
            // splitContainer5
            // 
            splitContainer5.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer5.Location = new Point(2, 4);
            splitContainer5.Margin = new Padding(2);
            splitContainer5.Name = "splitContainer5";
            splitContainer5.Orientation = Orientation.Horizontal;
            // 
            // splitContainer5.Panel1
            // 
            splitContainer5.Panel1.Controls.Add(btnClearSearch);
            splitContainer5.Panel1.Controls.Add(tbSearch);
            splitContainer5.Panel1.Controls.Add(dgvConversations);
            // 
            // splitContainer5.Panel2
            // 
            splitContainer5.Panel2.Controls.Add(ndcWeb);
            splitContainer5.Size = new Size(332, 667);
            splitContainer5.SplitterDistance = 316;
            splitContainer5.SplitterWidth = 2;
            splitContainer5.TabIndex = 1;
            // 
            // btnClearSearch
            // 
            btnClearSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClearSearch.BackColor = Color.Black;
            btnClearSearch.ForeColor = Color.White;
            btnClearSearch.Location = new Point(253, 7);
            btnClearSearch.Margin = new Padding(2);
            btnClearSearch.Name = "btnClearSearch";
            btnClearSearch.Size = new Size(79, 19);
            btnClearSearch.TabIndex = 2;
            btnClearSearch.Text = "Clear";
            btnClearSearch.UseVisualStyleBackColor = false;
            btnClearSearch.Click += btnClearSearch_Click;
            // 
            // tbSearch
            // 
            tbSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbSearch.BackColor = Color.Black;
            tbSearch.ForeColor = Color.White;
            tbSearch.Location = new Point(6, 7);
            tbSearch.Margin = new Padding(2);
            tbSearch.Name = "tbSearch";
            tbSearch.PlaceholderText = "Search...";
            tbSearch.Size = new Size(244, 23);
            tbSearch.TabIndex = 1;
            tbSearch.TextChanged += tbSearch_TextChanged;
            // 
            // dgvConversations
            // 
            dgvConversations.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvConversations.BackgroundColor = Color.FromArgb(64, 64, 64);
            dgvConversations.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvConversations.Location = new Point(2, 29);
            dgvConversations.Margin = new Padding(2);
            dgvConversations.MultiSelect = false;
            dgvConversations.Name = "dgvConversations";
            dgvConversations.RowHeadersWidth = 62;
            dgvConversations.Size = new Size(330, 283);
            dgvConversations.TabIndex = 0;
            dgvConversations.CellClick += dgvConversations_CellClick;
            // 
            // ndcWeb
            // 
            ndcWeb.AllowExternalDrop = true;
            ndcWeb.BackColor = Color.DarkGray;
            ndcWeb.CreationProperties = null;
            ndcWeb.DefaultBackgroundColor = Color.White;
            ndcWeb.Location = new Point(2, -3);
            ndcWeb.Margin = new Padding(2);
            ndcWeb.Name = "ndcWeb";
            ndcWeb.Size = new Size(325, 349);
            ndcWeb.TabIndex = 1;
            ndcWeb.ZoomFactor = 1D;
            // 
            // cbUseEmbeddings
            // 
            cbUseEmbeddings.BackColor = Color.Transparent;
            cbUseEmbeddings.ForeColor = Color.White;
            cbUseEmbeddings.Location = new Point(229, 20);
            cbUseEmbeddings.Margin = new Padding(2);
            cbUseEmbeddings.Name = "cbUseEmbeddings";
            cbUseEmbeddings.Size = new Size(45, 19);
            cbUseEmbeddings.TabIndex = 15;
            cbUseEmbeddings.Text = "Use";
            cbUseEmbeddings.UseVisualStyleBackColor = false;
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel1.BackColor = Color.Black;
            panel1.Controls.Add(chatWebView);
            panel1.Location = new Point(2, 63);
            panel1.Name = "panel1";
            panel1.Size = new Size(1021, 609);
            panel1.TabIndex = 21;
            // 
            // chatWebView
            // 
            chatWebView.AllowExternalDrop = true;
            chatWebView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            chatWebView.BackColor = Color.RosyBrown;
            chatWebView.CreationProperties = null;
            chatWebView.DefaultBackgroundColor = Color.White;
            chatWebView.Location = new Point(0, 0);
            chatWebView.Margin = new Padding(0);
            chatWebView.Name = "chatWebView";
            chatWebView.Size = new Size(1021, 609);
            chatWebView.TabIndex = 0;
            chatWebView.ZoomFactor = 1D;
            chatWebView.DragDrop += chatWebView_DragDrop;
            // 
            // btnProjectHelper
            // 
            btnProjectHelper.BackColor = Color.Black;
            btnProjectHelper.ForeColor = Color.White;
            btnProjectHelper.Location = new Point(157, 2);
            btnProjectHelper.Margin = new Padding(2);
            btnProjectHelper.Name = "btnProjectHelper";
            btnProjectHelper.Size = new Size(56, 51);
            btnProjectHelper.TabIndex = 19;
            btnProjectHelper.Text = "Project\r\nHelper";
            btnProjectHelper.UseVisualStyleBackColor = false;
            btnProjectHelper.Click += btnProjectHelper_Click;
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label2.AutoSize = true;
            label2.ForeColor = Color.White;
            label2.Location = new Point(621, 37);
            label2.Margin = new Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new Size(105, 15);
            label2.TabIndex = 18;
            label2.Text = "Summary/Suggest";
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label1.AutoSize = true;
            label1.ForeColor = Color.White;
            label1.Location = new Point(641, 10);
            label1.Margin = new Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new Size(87, 15);
            label1.TabIndex = 17;
            label1.Text = "Main AI Engine";
            // 
            // cbSummaryEngine
            // 
            cbSummaryEngine.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cbSummaryEngine.BackColor = Color.Black;
            cbSummaryEngine.Font = new Font("Segoe UI", 12F);
            cbSummaryEngine.ForeColor = Color.White;
            cbSummaryEngine.FormattingEnabled = true;
            cbSummaryEngine.Location = new Point(732, 31);
            cbSummaryEngine.Margin = new Padding(2);
            cbSummaryEngine.Name = "cbSummaryEngine";
            cbSummaryEngine.Size = new Size(291, 29);
            cbSummaryEngine.TabIndex = 16;
            cbSummaryEngine.SelectedIndexChanged += cbSummaryEngine_SelectedIndexChanged;
            // 
            // button1
            // 
            button1.BackColor = Color.FromArgb(64, 64, 64);
            button1.FlatStyle = FlatStyle.Popup;
            button1.ForeColor = Color.White;
            button1.Location = new Point(6, 3);
            button1.Margin = new Padding(2);
            button1.Name = "button1";
            button1.Size = new Size(22, 50);
            button1.TabIndex = 13;
            button1.Text = ">\r\n>\r\n>";
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // buttonStartRecording
            // 
            buttonStartRecording.BackColor = Color.Black;
            buttonStartRecording.ForeColor = Color.White;
            buttonStartRecording.Location = new Point(92, 2);
            buttonStartRecording.Margin = new Padding(2);
            buttonStartRecording.Name = "buttonStartRecording";
            buttonStartRecording.Size = new Size(60, 51);
            buttonStartRecording.TabIndex = 10;
            buttonStartRecording.Text = "Voice\r\nPrompt";
            buttonStartRecording.UseVisualStyleBackColor = false;
            buttonStartRecording.Click += buttonStartRecording_Click;
            // 
            // buttonAttachImage
            // 
            buttonAttachImage.BackColor = Color.Black;
            buttonAttachImage.ForeColor = Color.White;
            buttonAttachImage.Location = new Point(36, 2);
            buttonAttachImage.Margin = new Padding(2);
            buttonAttachImage.Name = "buttonAttachImage";
            buttonAttachImage.Size = new Size(52, 51);
            buttonAttachImage.TabIndex = 12;
            buttonAttachImage.Text = "Attach";
            buttonAttachImage.UseVisualStyleBackColor = false;
            buttonAttachImage.Click += buttonAttachImage_Click;
            // 
            // menuBar
            // 
            menuBar.BackColor = Color.Black;
            menuBar.ImageScalingSize = new Size(24, 24);
            menuBar.Items.AddRange(new ToolStripItem[] { toolStripMenuItem1 });
            menuBar.Location = new Point(0, 0);
            menuBar.Name = "menuBar";
            menuBar.Padding = new Padding(4, 1, 0, 1);
            menuBar.Size = new Size(1373, 24);
            menuBar.TabIndex = 10;
            menuBar.Text = "menuStrip1";
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(12, 22);
            // 
            // statusStrip1
            // 
            statusStrip1.BackColor = Color.Black;
            statusStrip1.ImageScalingSize = new Size(24, 24);
            statusStrip1.Items.AddRange(new ToolStripItem[] { tokenUsageLabel });
            statusStrip1.Location = new Point(0, 693);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 10, 0);
            statusStrip1.Size = new Size(1373, 22);
            statusStrip1.TabIndex = 11;
            statusStrip1.Text = "statusStrip1";
            // 
            // tokenUsageLabel
            // 
            tokenUsageLabel.ForeColor = Color.White;
            tokenUsageLabel.Name = "tokenUsageLabel";
            tokenUsageLabel.Size = new Size(73, 17);
            tokenUsageLabel.Text = "Token Usage";
            // 
            // button2
            // 
            button2.Location = new Point(312, 5);
            button2.Name = "button2";
            button2.Size = new Size(52, 29);
            button2.TabIndex = 22;
            button2.Text = "button2";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // MaxsAiStudio
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.Black;
            ClientSize = new Size(1373, 715);
            Controls.Add(splitContainer1);
            Controls.Add(statusStrip1);
            Controls.Add(menuBar);
            MainMenuStrip = menuBar;
            Margin = new Padding(2);
            Name = "MaxsAiStudio";
            Text = "MaxsAiStudio";
            FormClosing += MaxsAiStudio_FormClosing;
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitContainer5.Panel1.ResumeLayout(false);
            splitContainer5.Panel1.PerformLayout();
            splitContainer5.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer5).EndInit();
            splitContainer5.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvConversations).EndInit();
            ((System.ComponentModel.ISupportInitialize)ndcWeb).EndInit();
            panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)chatWebView).EndInit();
            menuBar.ResumeLayout(false);
            menuBar.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ComboBox cbEngine;
        private SplitContainer splitContainer1;
        private DataGridView dgvConversations;
        private SplitContainer splitContainer5;
        private MenuStrip menuBar;
        private ToolStripMenuItem toolStripMenuItem1;
        private Button buttonStartRecording;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel tokenUsageLabel;
        private Button buttonAttachImage;
        private TextBox tbSearch;
        private Button btnClearSearch;
        private Microsoft.Web.WebView2.WinForms.WebView2 ndcWeb;
        private ChatWebView chatWebView;
        private Button button1;
        private CheckBox cbUseEmbeddings;
        private ComboBox cbSummaryEngine;
        private Label label1;
        private Label label2;
        private Button btnProjectHelper;
        private Panel panel1;
        private Button button2;
    }
}