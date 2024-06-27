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
            ndcConversation = new NetworkDiagramControl();
            cbEngine = new ComboBox();
            btnGo = new Button();
            rtbSystemPrompt = new ButtonedRichTextBox();
            rtbOutput = new ButtonedRichTextBox();
            rtbInput = new ButtonedRichTextBox();
            splitContainer1 = new SplitContainer();
            splitContainer5 = new SplitContainer();
            dgvConversations = new DataGridView();
            splitContainer2 = new SplitContainer();
            button2 = new Button();
            button1 = new Button();
            cbTemplates = new ComboBox();
            cbCategories = new ComboBox();
            splitContainer3 = new SplitContainer();
            panelSnippets = new Panel();
            button3 = new Button();
            btnRestart = new Button();
            btnClear = new Button();
            menuBar = new MenuStrip();
            toolStripMenuItem1 = new ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer5).BeginInit();
            splitContainer5.Panel1.SuspendLayout();
            splitContainer5.Panel2.SuspendLayout();
            splitContainer5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvConversations).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer3).BeginInit();
            splitContainer3.Panel1.SuspendLayout();
            splitContainer3.Panel2.SuspendLayout();
            splitContainer3.SuspendLayout();
            menuBar.SuspendLayout();
            SuspendLayout();
            // 
            // ndcConversation
            // 
            ndcConversation.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            ndcConversation.BackColor = Color.DimGray;
            ndcConversation.HighlightedNode = null;
            ndcConversation.HighlightedNodeBorderColor = Color.Red;
            ndcConversation.Location = new Point(3, 3);
            ndcConversation.Name = "ndcConversation";
            ndcConversation.NodeBackgroundColor = Color.LightBlue;
            ndcConversation.NodeBorderColor = Color.Blue;
            ndcConversation.NodeCornerRadius = 10;
            ndcConversation.NodeForegroundColor = Color.Black;
            ndcConversation.NodeGradientEnd = Color.LightSkyBlue;
            ndcConversation.NodeGradientStart = Color.White;
            ndcConversation.Size = new Size(335, 602);
            ndcConversation.TabIndex = 0;
            ndcConversation.Text = "networkDiagramControl1";
            ndcConversation.UseDropShadow = true;
            // 
            // cbEngine
            // 
            cbEngine.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cbEngine.BackColor = Color.Black;
            cbEngine.Font = new Font("Segoe UI", 12F);
            cbEngine.ForeColor = Color.White;
            cbEngine.FormattingEnabled = true;
            cbEngine.Location = new Point(994, 3);
            cbEngine.Name = "cbEngine";
            cbEngine.Size = new Size(315, 40);
            cbEngine.TabIndex = 3;
            // 
            // btnGo
            // 
            btnGo.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnGo.BackColor = Color.Black;
            btnGo.ForeColor = Color.White;
            btnGo.Location = new Point(1164, 232);
            btnGo.Name = "btnGo";
            btnGo.Size = new Size(145, 246);
            btnGo.TabIndex = 4;
            btnGo.Text = "Go";
            btnGo.UseVisualStyleBackColor = false;
            btnGo.Click += btnGo_Click;
            // 
            // rtbSystemPrompt
            // 
            rtbSystemPrompt.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            rtbSystemPrompt.BackColor = Color.Black;
            rtbSystemPrompt.ForeColor = Color.FromArgb(224, 224, 224);
            rtbSystemPrompt.Location = new Point(3, 49);
            rtbSystemPrompt.Name = "rtbSystemPrompt";
            rtbSystemPrompt.Size = new Size(1306, 193);
            rtbSystemPrompt.TabIndex = 5;
            // 
            // rtbOutput
            // 
            rtbOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            rtbOutput.BackColor = Color.Black;
            rtbOutput.ForeColor = Color.FromArgb(224, 224, 224);
            rtbOutput.Location = new Point(3, 3);
            rtbOutput.Name = "rtbOutput";
            rtbOutput.Size = new Size(1155, 271);
            rtbOutput.TabIndex = 6;
            // 
            // rtbInput
            // 
            rtbInput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            rtbInput.BackColor = Color.Black;
            rtbInput.ForeColor = Color.FromArgb(224, 224, 224);
            rtbInput.Location = new Point(3, 3);
            rtbInput.Name = "rtbInput";
            rtbInput.Size = new Size(1155, 475);
            rtbInput.TabIndex = 7;
            // 
            // splitContainer1
            // 
            splitContainer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer1.Location = new Point(12, 42);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(splitContainer5);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(splitContainer2);
            splitContainer1.Size = new Size(1669, 1015);
            splitContainer1.SplitterDistance = 347;
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
            splitContainer5.Panel1.Controls.Add(dgvConversations);
            // 
            // splitContainer5.Panel2
            // 
            splitContainer5.Panel2.Controls.Add(ndcConversation);
            splitContainer5.Size = new Size(341, 1006);
            splitContainer5.SplitterDistance = 394;
            splitContainer5.TabIndex = 1;
            // 
            // dgvConversations
            // 
            dgvConversations.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvConversations.BackgroundColor = Color.Black;
            dgvConversations.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvConversations.Location = new Point(3, 3);
            dgvConversations.Name = "dgvConversations";
            dgvConversations.RowHeadersWidth = 62;
            dgvConversations.Size = new Size(335, 388);
            dgvConversations.TabIndex = 0;
            dgvConversations.CellClick += dgvConversations_CellClick;
            // 
            // splitContainer2
            // 
            splitContainer2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer2.Location = new Point(3, 3);
            splitContainer2.Name = "splitContainer2";
            splitContainer2.Orientation = Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(button2);
            splitContainer2.Panel1.Controls.Add(button1);
            splitContainer2.Panel1.Controls.Add(cbTemplates);
            splitContainer2.Panel1.Controls.Add(cbCategories);
            splitContainer2.Panel1.Controls.Add(cbEngine);
            splitContainer2.Panel1.Controls.Add(rtbSystemPrompt);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(splitContainer3);
            splitContainer2.Size = new Size(1312, 1012);
            splitContainer2.SplitterDistance = 245;
            splitContainer2.TabIndex = 8;
            // 
            // button2
            // 
            button2.BackColor = Color.Black;
            button2.ForeColor = Color.White;
            button2.Location = new Point(913, 7);
            button2.Name = "button2";
            button2.Size = new Size(75, 34);
            button2.TabIndex = 9;
            button2.Text = "Add";
            button2.UseVisualStyleBackColor = false;
            button2.Click += button2_Click;
            // 
            // button1
            // 
            button1.BackColor = Color.Black;
            button1.ForeColor = Color.White;
            button1.Location = new Point(838, 6);
            button1.Name = "button1";
            button1.Size = new Size(69, 34);
            button1.TabIndex = 8;
            button1.Text = "Edit";
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // cbTemplates
            // 
            cbTemplates.BackColor = Color.Black;
            cbTemplates.Font = new Font("Segoe UI", 12F);
            cbTemplates.ForeColor = Color.White;
            cbTemplates.FormattingEnabled = true;
            cbTemplates.Location = new Point(389, 3);
            cbTemplates.Name = "cbTemplates";
            cbTemplates.Size = new Size(443, 40);
            cbTemplates.TabIndex = 7;
            cbTemplates.SelectedIndexChanged += cbTemplates_SelectedIndexChanged;
            // 
            // cbCategories
            // 
            cbCategories.BackColor = Color.Black;
            cbCategories.Font = new Font("Segoe UI", 12F);
            cbCategories.ForeColor = Color.White;
            cbCategories.FormattingEnabled = true;
            cbCategories.Location = new Point(3, 3);
            cbCategories.Name = "cbCategories";
            cbCategories.Size = new Size(380, 40);
            cbCategories.TabIndex = 6;
            cbCategories.SelectedIndexChanged += cbCategories_SelectedIndexChanged;
            // 
            // splitContainer3
            // 
            splitContainer3.Dock = DockStyle.Fill;
            splitContainer3.Location = new Point(0, 0);
            splitContainer3.Name = "splitContainer3";
            splitContainer3.Orientation = Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            splitContainer3.Panel1.Controls.Add(panelSnippets);
            splitContainer3.Panel1.Controls.Add(rtbOutput);
            // 
            // splitContainer3.Panel2
            // 
            splitContainer3.Panel2.Controls.Add(button3);
            splitContainer3.Panel2.Controls.Add(btnRestart);
            splitContainer3.Panel2.Controls.Add(btnClear);
            splitContainer3.Panel2.Controls.Add(rtbInput);
            splitContainer3.Panel2.Controls.Add(btnGo);
            splitContainer3.Size = new Size(1312, 763);
            splitContainer3.SplitterDistance = 278;
            splitContainer3.TabIndex = 0;
            // 
            // panelSnippets
            // 
            panelSnippets.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            panelSnippets.Location = new Point(1168, 2);
            panelSnippets.Name = "panelSnippets";
            panelSnippets.Size = new Size(144, 274);
            panelSnippets.TabIndex = 7;
            // 
            // button3
            // 
            button3.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            button3.BackColor = Color.Black;
            button3.ForeColor = Color.White;
            button3.Location = new Point(1216, 21);
            button3.Name = "button3";
            button3.Size = new Size(35, 34);
            button3.TabIndex = 10;
            button3.Text = "Restart";
            button3.UseVisualStyleBackColor = false;
            button3.Click += button3_Click;
            // 
            // btnRestart
            // 
            btnRestart.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnRestart.BackColor = Color.Black;
            btnRestart.ForeColor = Color.White;
            btnRestart.Location = new Point(1164, 61);
            btnRestart.Name = "btnRestart";
            btnRestart.Size = new Size(145, 77);
            btnRestart.TabIndex = 9;
            btnRestart.Text = "Restart";
            btnRestart.UseVisualStyleBackColor = false;
            btnRestart.Click += btnRestart_Click;
            // 
            // btnClear
            // 
            btnClear.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClear.BackColor = Color.Black;
            btnClear.ForeColor = Color.White;
            btnClear.Location = new Point(1164, 144);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(145, 77);
            btnClear.TabIndex = 8;
            btnClear.Text = "Clear";
            btnClear.UseVisualStyleBackColor = false;
            btnClear.Click += btnClear_Click;
            // 
            // menuBar
            // 
            menuBar.BackColor = Color.Black;
            menuBar.ImageScalingSize = new Size(24, 24);
            menuBar.Items.AddRange(new ToolStripItem[] { toolStripMenuItem1 });
            menuBar.Location = new Point(0, 0);
            menuBar.Name = "menuBar";
            menuBar.Size = new Size(1693, 24);
            menuBar.TabIndex = 10;
            menuBar.Text = "menuStrip1";
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(16, 20);
            // 
            // Form2
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(1693, 1069);
            Controls.Add(splitContainer1);
            Controls.Add(menuBar);
            MainMenuStrip = menuBar;
            Name = "Form2";
            Text = "Form2";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitContainer5.Panel1.ResumeLayout(false);
            splitContainer5.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer5).EndInit();
            splitContainer5.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvConversations).EndInit();
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            splitContainer3.Panel1.ResumeLayout(false);
            splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer3).EndInit();
            splitContainer3.ResumeLayout(false);
            menuBar.ResumeLayout(false);
            menuBar.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private NetworkDiagramControl ndcConversation;
        private Button btnGo;
        private ComboBox cbEngine;
        private ButtonedRichTextBox rtbSystemPrompt;
        private ButtonedRichTextBox rtbOutput;
        private ButtonedRichTextBox rtbInput;
        private SplitContainer splitContainer1;
        private SplitContainer splitContainer2;
        private SplitContainer splitContainer3;
        private Button btnClear;
        private DataGridView dgvConversations;
        private SplitContainer splitContainer5;
        private MenuStrip menuBar;
        private ToolStripMenuItem toolStripMenuItem1;
        private ComboBox cbTemplates;
        private ComboBox cbCategories;
        private Panel panelSnippets;
        private Button button1;
        private Button button2;
        private Button btnRestart;
        private Button button3;
    }
}