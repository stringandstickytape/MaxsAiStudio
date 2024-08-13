using AiTool3.UI;
using AiTool3.UI.Forms;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MaxsAiStudio));
            splitContainer1 = new SplitContainer();
            splitContainer5 = new SplitContainer();
            btnClearSearch = new Button();
            tbSearch = new TextBox();
            dgvConversations = new ConversationDataGridView();
            ndcWeb = new Microsoft.Web.WebView2.WinForms.WebView2();
            panel1 = new Panel();
            button1 = new Button();
            chatWebView = new ChatWebView();
            menuBar = new MenuStrip();
            toolStripMenuItem1 = new ToolStripMenuItem();
            statusStrip1 = new StatusStrip();
            tokenUsageLabel = new ToolStripStatusLabel();
            tipLabel = new ToolStripStatusLabel();
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
            splitContainer1.Panel2.Controls.Add(panel1);
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
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel1.BackColor = Color.Black;
            panel1.Controls.Add(button1);
            panel1.Controls.Add(chatWebView);
            panel1.Location = new Point(0, 0);
            panel1.Margin = new Padding(0);
            panel1.Name = "panel1";
            panel1.Size = new Size(1023, 666);
            panel1.TabIndex = 21;
            // 
            // button1
            // 
            button1.BackColor = Color.FromArgb(64, 64, 64);
            button1.FlatStyle = FlatStyle.Popup;
            button1.ForeColor = Color.White;
            button1.Location = new Point(0, 68);
            button1.Margin = new Padding(2);
            button1.Name = "button1";
            button1.Size = new Size(19, 59);
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
            chatWebView.Location = new Point(0, 0);
            chatWebView.Margin = new Padding(0);
            chatWebView.Name = "chatWebView";
            chatWebView.Size = new Size(1023, 666);
            chatWebView.TabIndex = 0;
            chatWebView.ZoomFactor = 1D;
            chatWebView.DragDrop += chatWebView_DragDrop;
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
            statusStrip1.Items.AddRange(new ToolStripItem[] { tokenUsageLabel, tipLabel });
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
            // tipLabel
            // 
            tipLabel.ForeColor = Color.White;
            tipLabel.Name = "tipLabel";
            tipLabel.Size = new Size(54, 17);
            tipLabel.Text = "";
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
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuBar;
            Margin = new Padding(2);
            Name = "MaxsAiStudio";
            Text = "MaxsAiStudio";
            FormClosing += MaxsAiStudio_FormClosing;
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
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
        private SplitContainer splitContainer1;
        private ConversationDataGridView dgvConversations;
        private SplitContainer splitContainer5;
        private MenuStrip menuBar;
        private ToolStripMenuItem toolStripMenuItem1;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel tokenUsageLabel;
        private TextBox tbSearch;
        private Button btnClearSearch;
        private Microsoft.Web.WebView2.WinForms.WebView2 ndcWeb;
        private ChatWebView chatWebView;
        private Button button1;
        private Panel panel1;
        private ToolStripStatusLabel tipLabel;
    }
}