using AiTool3.UI;

namespace AiTool3
{
    partial class Form2
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
            label2 = new Label();
            label1 = new Label();
            cbSummaryEngine = new ComboBox();
            cbUseEmbeddings = new CheckBox();
            btnGenerateEmbeddings = new Button();
            button1 = new Button();
            chatWebView = new ChatWebView();
            buttonStartRecording = new Button();
            buttonAttachImage = new Button();
            menuBar = new MenuStrip();
            toolStripMenuItem1 = new ToolStripMenuItem();
            statusStrip1 = new StatusStrip();
            tokenUsageLabel = new ToolStripStatusLabel();
            splitContainer4 = new SplitContainer();
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
            ((System.ComponentModel.ISupportInitialize)chatWebView).BeginInit();
            menuBar.SuspendLayout();
            statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer4).BeginInit();
            splitContainer4.Panel1.SuspendLayout();
            splitContainer4.Panel2.SuspendLayout();
            splitContainer4.SuspendLayout();
            SuspendLayout();
            // 
            // cbEngine
            // 
            cbEngine.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cbEngine.BackColor = Color.Black;
            cbEngine.Font = new Font("Segoe UI", 12F);
            cbEngine.ForeColor = Color.White;
            cbEngine.FormattingEnabled = true;
            cbEngine.Location = new Point(947, 6);
            cbEngine.Name = "cbEngine";
            cbEngine.Size = new Size(499, 40);
            cbEngine.TabIndex = 3;
            cbEngine.SelectedIndexChanged += cbEngine_SelectedIndexChanged;
            // 
            // splitContainer1
            // 
            splitContainer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer1.Location = new Point(3, 3);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(splitContainer5);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(label2);
            splitContainer1.Panel2.Controls.Add(label1);
            splitContainer1.Panel2.Controls.Add(cbSummaryEngine);
            splitContainer1.Panel2.Controls.Add(cbUseEmbeddings);
            splitContainer1.Panel2.Controls.Add(btnGenerateEmbeddings);
            splitContainer1.Panel2.Controls.Add(button1);
            splitContainer1.Panel2.Controls.Add(chatWebView);
            splitContainer1.Panel2.Controls.Add(cbEngine);
            splitContainer1.Panel2.Controls.Add(buttonStartRecording);
            splitContainer1.Panel2.Controls.Add(buttonAttachImage);
            splitContainer1.Size = new Size(1929, 1120);
            splitContainer1.SplitterDistance = 476;
            splitContainer1.TabIndex = 9;
            // 
            // splitContainer5
            // 
            splitContainer5.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer5.Location = new Point(3, 6);
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
            splitContainer5.Size = new Size(470, 1111);
            splitContainer5.SplitterDistance = 528;
            splitContainer5.TabIndex = 1;
            // 
            // btnClearSearch
            // 
            btnClearSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClearSearch.BackColor = Color.Black;
            btnClearSearch.ForeColor = Color.White;
            btnClearSearch.Location = new Point(357, 11);
            btnClearSearch.Name = "btnClearSearch";
            btnClearSearch.Size = new Size(113, 32);
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
            tbSearch.Location = new Point(9, 11);
            tbSearch.Name = "tbSearch";
            tbSearch.PlaceholderText = "Search...";
            tbSearch.Size = new Size(342, 31);
            tbSearch.TabIndex = 1;
            tbSearch.TextChanged += tbSearch_TextChanged;
            // 
            // dgvConversations
            // 
            dgvConversations.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvConversations.BackgroundColor = Color.FromArgb(64, 64, 64);
            dgvConversations.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvConversations.Location = new Point(3, 48);
            dgvConversations.MultiSelect = false;
            dgvConversations.Name = "dgvConversations";
            dgvConversations.RowHeadersWidth = 62;
            dgvConversations.Size = new Size(467, 473);
            dgvConversations.TabIndex = 0;
            dgvConversations.CellClick += dgvConversations_CellClick;
            // 
            // ndcWeb
            // 
            ndcWeb.AllowExternalDrop = true;
            ndcWeb.BackColor = Color.DarkGray;
            ndcWeb.CreationProperties = null;
            ndcWeb.DefaultBackgroundColor = Color.White;
            ndcWeb.Location = new Point(3, -5);
            ndcWeb.Name = "ndcWeb";
            ndcWeb.Size = new Size(464, 581);
            ndcWeb.TabIndex = 1;
            ndcWeb.ZoomFactor = 1D;
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label2.AutoSize = true;
            label2.ForeColor = Color.White;
            label2.Location = new Point(701, 57);
            label2.Name = "label2";
            label2.Size = new Size(240, 25);
            label2.TabIndex = 18;
            label2.Text = "Summary/Suggest AI Engine";
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label1.AutoSize = true;
            label1.ForeColor = Color.White;
            label1.Location = new Point(810, 13);
            label1.Name = "label1";
            label1.Size = new Size(131, 25);
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
            cbSummaryEngine.Location = new Point(947, 50);
            cbSummaryEngine.Name = "cbSummaryEngine";
            cbSummaryEngine.Size = new Size(499, 40);
            cbSummaryEngine.TabIndex = 16;
            cbSummaryEngine.SelectedIndexChanged += cbSummaryEngine_SelectedIndexChanged;
            // 
            // cbUseEmbeddings
            // 
            cbUseEmbeddings.AutoSize = true;
            cbUseEmbeddings.ForeColor = Color.White;
            cbUseEmbeddings.Location = new Point(331, 5);
            cbUseEmbeddings.Name = "cbUseEmbeddings";
            cbUseEmbeddings.Size = new Size(265, 29);
            cbUseEmbeddings.TabIndex = 15;
            cbUseEmbeddings.Text = "Use embeddings in prompts";
            cbUseEmbeddings.UseVisualStyleBackColor = true;
            // 
            // btnGenerateEmbeddings
            // 
            btnGenerateEmbeddings.BackColor = Color.Black;
            btnGenerateEmbeddings.ForeColor = Color.White;
            btnGenerateEmbeddings.Location = new Point(339, 48);
            btnGenerateEmbeddings.Name = "btnGenerateEmbeddings";
            btnGenerateEmbeddings.Size = new Size(257, 36);
            btnGenerateEmbeddings.TabIndex = 14;
            btnGenerateEmbeddings.Text = "Generate Embeddings";
            btnGenerateEmbeddings.UseVisualStyleBackColor = false;
            btnGenerateEmbeddings.Click += btnGenerateEmbeddings_Click;
            // 
            // button1
            // 
            button1.BackColor = Color.FromArgb(64, 64, 64);
            button1.FlatStyle = FlatStyle.Popup;
            button1.ForeColor = Color.White;
            button1.Location = new Point(9, 5);
            button1.Name = "button1";
            button1.Size = new Size(31, 84);
            button1.TabIndex = 13;
            button1.Text = ">\r\n>\r\n>";
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // chatWebView
            // 
            chatWebView.AllowExternalDrop = true;
            chatWebView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            chatWebView.BackColor = Color.RosyBrown;
            chatWebView.CreationProperties = null;
            chatWebView.DefaultBackgroundColor = Color.White;
            chatWebView.Location = new Point(9, 96);
            chatWebView.Name = "chatWebView";
            chatWebView.Size = new Size(1437, 1021);
            chatWebView.TabIndex = 0;
            chatWebView.ZoomFactor = 1D;
            chatWebView.DragDrop += chatWebView_DragDrop;
            // 
            // buttonStartRecording
            // 
            buttonStartRecording.BackColor = Color.Black;
            buttonStartRecording.ForeColor = Color.White;
            buttonStartRecording.Location = new Point(146, 3);
            buttonStartRecording.Name = "buttonStartRecording";
            buttonStartRecording.Size = new Size(148, 85);
            buttonStartRecording.TabIndex = 10;
            buttonStartRecording.Text = "Voice Prompt";
            buttonStartRecording.UseVisualStyleBackColor = false;
            buttonStartRecording.Click += buttonStartRecording_Click;
            // 
            // buttonAttachImage
            // 
            buttonAttachImage.BackColor = Color.Black;
            buttonAttachImage.ForeColor = Color.White;
            buttonAttachImage.Location = new Point(51, 3);
            buttonAttachImage.Name = "buttonAttachImage";
            buttonAttachImage.Size = new Size(89, 85);
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
            menuBar.Size = new Size(1962, 24);
            menuBar.TabIndex = 10;
            menuBar.Text = "menuStrip1";
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(16, 20);
            // 
            // statusStrip1
            // 
            statusStrip1.BackColor = Color.Black;
            statusStrip1.ImageScalingSize = new Size(24, 24);
            statusStrip1.Items.AddRange(new ToolStripItem[] { tokenUsageLabel });
            statusStrip1.Location = new Point(0, 1160);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1962, 32);
            statusStrip1.TabIndex = 11;
            statusStrip1.Text = "statusStrip1";
            // 
            // tokenUsageLabel
            // 
            tokenUsageLabel.ForeColor = Color.White;
            tokenUsageLabel.Name = "tokenUsageLabel";
            tokenUsageLabel.Size = new Size(112, 25);
            tokenUsageLabel.Text = "Token Usage";
            // 
            // splitContainer4
            // 
            splitContainer4.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer4.IsSplitterFixed = true;
            splitContainer4.Location = new Point(15, 31);
            splitContainer4.Name = "splitContainer4";
            // 
            // splitContainer4.Panel1
            // 
            splitContainer4.Panel1Collapsed = true;
            // 
            // splitContainer4.Panel2
            // 
            splitContainer4.Panel2.Controls.Add(splitContainer1);
            splitContainer4.Size = new Size(1935, 1126);
            splitContainer4.SplitterDistance = 644;
            splitContainer4.TabIndex = 12;
            // 
            // Form2
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(1962, 1192);
            Controls.Add(statusStrip1);
            Controls.Add(menuBar);
            Controls.Add(splitContainer4);
            MainMenuStrip = menuBar;
            Name = "Form2";
            Text = "Form2";
            FormClosing += Form2_FormClosing;
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
            ((System.ComponentModel.ISupportInitialize)chatWebView).EndInit();
            menuBar.ResumeLayout(false);
            menuBar.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            splitContainer4.Panel1.ResumeLayout(false);
            splitContainer4.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer4).EndInit();
            splitContainer4.ResumeLayout(false);
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
        private SplitContainer splitContainer4;
        private ChatWebView chatWebView;
        private Button button1;
        private Button btnGenerateEmbeddings;
        private CheckBox cbUseEmbeddings;
        private ComboBox cbSummaryEngine;
        private Label label1;
        private Label label2;
    }
}