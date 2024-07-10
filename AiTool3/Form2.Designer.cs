﻿using AiTool3.UI;

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
            btnGo = new Button();
            rtbSystemPrompt = new ButtonedRichTextBox();
            rtbOutput = new ButtonedRichTextBox();
            rtbInput = new ButtonedRichTextBox();
            splitContainer1 = new SplitContainer();
            splitContainer5 = new SplitContainer();
            btnClearSearch = new Button();
            tbSearch = new TextBox();
            dgvConversations = new DataGridView();
            ndcWeb = new Microsoft.Web.WebView2.WinForms.WebView2();
            splitContainer2 = new SplitContainer();
            groupBox1 = new GroupBox();
            buttonAddTemplate = new Button();
            cbCategories = new ComboBox();
            cbTemplates = new ComboBox();
            buttonEditTemplate = new Button();
            splitContainer3 = new SplitContainer();
            btnCancel = new Button();
            buttonAttachImage = new Button();
            buttonNewKeepAll = new Button();
            buttonStartRecording = new Button();
            btnRestart = new Button();
            btnClear = new Button();
            menuBar = new MenuStrip();
            toolStripMenuItem1 = new ToolStripMenuItem();
            statusStrip1 = new StatusStrip();
            tokenUsageLabel = new ToolStripStatusLabel();
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
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer3).BeginInit();
            splitContainer3.Panel1.SuspendLayout();
            splitContainer3.Panel2.SuspendLayout();
            splitContainer3.SuspendLayout();
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
            cbEngine.Location = new Point(623, 3);
            cbEngine.Name = "cbEngine";
            cbEngine.Size = new Size(405, 40);
            cbEngine.TabIndex = 3;
            // 
            // btnGo
            // 
            btnGo.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnGo.BackColor = Color.Black;
            btnGo.ForeColor = Color.White;
            btnGo.Location = new Point(868, 397);
            btnGo.Name = "btnGo";
            btnGo.Size = new Size(160, 85);
            btnGo.TabIndex = 4;
            btnGo.Text = "Go (CTRL-Return)";
            btnGo.UseVisualStyleBackColor = false;
            btnGo.Click += btnGo_Click;
            // 
            // rtbSystemPrompt
            // 
            rtbSystemPrompt.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            rtbSystemPrompt.BackColor = Color.Black;
            rtbSystemPrompt.EmptyLabel = "System Prompt";
            rtbSystemPrompt.FlashOnUpdate = false;
            rtbSystemPrompt.ForeColor = Color.FromArgb(224, 224, 224);
            rtbSystemPrompt.Location = new Point(3, 3);
            rtbSystemPrompt.Name = "rtbSystemPrompt";
            rtbSystemPrompt.Size = new Size(601, 218);
            rtbSystemPrompt.TabIndex = 5;
            // 
            // rtbOutput
            // 
            rtbOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            rtbOutput.BackColor = Color.Black;
            rtbOutput.EmptyLabel = "AI Output";
            rtbOutput.FlashOnUpdate = true;
            rtbOutput.ForeColor = Color.FromArgb(224, 224, 224);
            rtbOutput.Location = new Point(3, 3);
            rtbOutput.Name = "rtbOutput";
            rtbOutput.Size = new Size(1025, 268);
            rtbOutput.TabIndex = 6;
            // 
            // rtbInput
            // 
            rtbInput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            rtbInput.BackColor = Color.Black;
            rtbInput.EmptyLabel = "User Input";
            rtbInput.FlashOnUpdate = false;
            rtbInput.ForeColor = Color.FromArgb(224, 224, 224);
            rtbInput.Location = new Point(3, 3);
            rtbInput.Name = "rtbInput";
            rtbInput.Size = new Size(1025, 379);
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
            splitContainer1.Size = new Size(1669, 992);
            splitContainer1.SplitterDistance = 628;
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
            splitContainer5.Size = new Size(622, 983);
            splitContainer5.SplitterDistance = 384;
            splitContainer5.TabIndex = 1;
            // 
            // btnClearSearch
            // 
            btnClearSearch.BackColor = Color.Black;
            btnClearSearch.ForeColor = Color.White;
            btnClearSearch.Location = new Point(526, 10);
            btnClearSearch.Name = "btnClearSearch";
            btnClearSearch.Size = new Size(93, 32);
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
            tbSearch.Size = new Size(511, 31);
            tbSearch.TabIndex = 1;
            tbSearch.TextChanged += tbSearch_TextChanged;
            // 
            // dgvConversations
            // 
            dgvConversations.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvConversations.BackgroundColor = Color.Black;
            dgvConversations.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvConversations.Location = new Point(3, 46);
            dgvConversations.Name = "dgvConversations";
            dgvConversations.RowHeadersWidth = 62;
            dgvConversations.Size = new Size(616, 335);
            dgvConversations.TabIndex = 0;
            dgvConversations.CellClick += dgvConversations_CellClick;
            // 
            // ndcWeb
            // 
            ndcWeb.AllowExternalDrop = true;
            ndcWeb.BackColor = Color.DarkGray;
            ndcWeb.CreationProperties = null;
            ndcWeb.DefaultBackgroundColor = Color.White;
            ndcWeb.Location = new Point(90, 140);
            ndcWeb.Name = "ndcWeb";
            ndcWeb.Size = new Size(446, 329);
            ndcWeb.TabIndex = 1;
            ndcWeb.ZoomFactor = 1D;
            // 
            // splitContainer2
            // 
            splitContainer2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer2.IsSplitterFixed = true;
            splitContainer2.Location = new Point(3, 3);
            splitContainer2.Name = "splitContainer2";
            splitContainer2.Orientation = Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(groupBox1);
            splitContainer2.Panel1.Controls.Add(cbEngine);
            splitContainer2.Panel1.Controls.Add(rtbSystemPrompt);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(splitContainer3);
            splitContainer2.Size = new Size(1031, 989);
            splitContainer2.SplitterDistance = 224;
            splitContainer2.TabIndex = 8;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            groupBox1.Controls.Add(buttonAddTemplate);
            groupBox1.Controls.Add(cbCategories);
            groupBox1.Controls.Add(cbTemplates);
            groupBox1.Controls.Add(buttonEditTemplate);
            groupBox1.ForeColor = Color.White;
            groupBox1.Location = new Point(623, 61);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(398, 170);
            groupBox1.TabIndex = 10;
            groupBox1.TabStop = false;
            groupBox1.Text = "Templates";
            // 
            // buttonAddTemplate
            // 
            buttonAddTemplate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonAddTemplate.BackColor = Color.Black;
            buttonAddTemplate.ForeColor = Color.White;
            buttonAddTemplate.Location = new Point(317, 130);
            buttonAddTemplate.Name = "buttonAddTemplate";
            buttonAddTemplate.Size = new Size(75, 34);
            buttonAddTemplate.TabIndex = 9;
            buttonAddTemplate.Text = "Add";
            buttonAddTemplate.UseVisualStyleBackColor = false;
            buttonAddTemplate.Click += buttonAdd_Click;
            // 
            // cbCategories
            // 
            cbCategories.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cbCategories.BackColor = Color.Black;
            cbCategories.Font = new Font("Segoe UI", 12F);
            cbCategories.ForeColor = Color.White;
            cbCategories.FormattingEnabled = true;
            cbCategories.Location = new Point(12, 38);
            cbCategories.Name = "cbCategories";
            cbCategories.Size = new Size(380, 40);
            cbCategories.TabIndex = 6;
            cbCategories.SelectedIndexChanged += cbCategories_SelectedIndexChanged;
            // 
            // cbTemplates
            // 
            cbTemplates.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cbTemplates.BackColor = Color.Black;
            cbTemplates.Font = new Font("Segoe UI", 12F);
            cbTemplates.ForeColor = Color.White;
            cbTemplates.FormattingEnabled = true;
            cbTemplates.Location = new Point(12, 84);
            cbTemplates.Name = "cbTemplates";
            cbTemplates.Size = new Size(380, 40);
            cbTemplates.TabIndex = 7;
            cbTemplates.SelectedIndexChanged += cbTemplates_SelectedIndexChanged;
            // 
            // buttonEditTemplate
            // 
            buttonEditTemplate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonEditTemplate.BackColor = Color.Black;
            buttonEditTemplate.ForeColor = Color.White;
            buttonEditTemplate.Location = new Point(242, 130);
            buttonEditTemplate.Name = "buttonEditTemplate";
            buttonEditTemplate.Size = new Size(69, 34);
            buttonEditTemplate.TabIndex = 8;
            buttonEditTemplate.Text = "Edit";
            buttonEditTemplate.UseVisualStyleBackColor = false;
            buttonEditTemplate.Click += buttonEditTemplate_Click;
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
            splitContainer3.Panel1.Controls.Add(rtbOutput);
            // 
            // splitContainer3.Panel2
            // 
            splitContainer3.Panel2.Controls.Add(btnCancel);
            splitContainer3.Panel2.Controls.Add(buttonAttachImage);
            splitContainer3.Panel2.Controls.Add(buttonNewKeepAll);
            splitContainer3.Panel2.Controls.Add(buttonStartRecording);
            splitContainer3.Panel2.Controls.Add(btnRestart);
            splitContainer3.Panel2.Controls.Add(btnClear);
            splitContainer3.Panel2.Controls.Add(rtbInput);
            splitContainer3.Panel2.Controls.Add(btnGo);
            splitContainer3.Size = new Size(1031, 761);
            splitContainer3.SplitterDistance = 275;
            splitContainer3.TabIndex = 0;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.BackColor = Color.Black;
            btnCancel.ForeColor = Color.White;
            btnCancel.Location = new Point(788, 396);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(74, 85);
            btnCancel.TabIndex = 13;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Visible = false;
            btnCancel.Click += btnCancel_Click;
            // 
            // buttonAttachImage
            // 
            buttonAttachImage.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonAttachImage.BackColor = Color.Black;
            buttonAttachImage.ForeColor = Color.White;
            buttonAttachImage.Location = new Point(439, 397);
            buttonAttachImage.Name = "buttonAttachImage";
            buttonAttachImage.Size = new Size(89, 85);
            buttonAttachImage.TabIndex = 12;
            buttonAttachImage.Text = "Attach";
            buttonAttachImage.UseVisualStyleBackColor = false;
            buttonAttachImage.Click += buttonAttachImage_Click;
            // 
            // buttonNewKeepAll
            // 
            buttonNewKeepAll.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonNewKeepAll.BackColor = Color.Black;
            buttonNewKeepAll.ForeColor = Color.White;
            buttonNewKeepAll.Location = new Point(289, 397);
            buttonNewKeepAll.Name = "buttonNewKeepAll";
            buttonNewKeepAll.Size = new Size(145, 85);
            buttonNewKeepAll.TabIndex = 11;
            buttonNewKeepAll.Text = "New w/ context";
            buttonNewKeepAll.UseVisualStyleBackColor = false;
            buttonNewKeepAll.Click += buttonNewKeepAll_Click;
            // 
            // buttonStartRecording
            // 
            buttonStartRecording.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonStartRecording.BackColor = Color.Black;
            buttonStartRecording.ForeColor = Color.White;
            buttonStartRecording.Location = new Point(534, 397);
            buttonStartRecording.Name = "buttonStartRecording";
            buttonStartRecording.Size = new Size(148, 85);
            buttonStartRecording.TabIndex = 10;
            buttonStartRecording.Text = "Voice Prompt";
            buttonStartRecording.UseVisualStyleBackColor = false;
            buttonStartRecording.Click += buttonStartRecording_Click;
            // 
            // btnRestart
            // 
            btnRestart.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnRestart.BackColor = Color.Black;
            btnRestart.ForeColor = Color.White;
            btnRestart.Location = new Point(137, 397);
            btnRestart.Name = "btnRestart";
            btnRestart.Size = new Size(145, 85);
            btnRestart.TabIndex = 9;
            btnRestart.Text = "New w/prompt";
            btnRestart.UseVisualStyleBackColor = false;
            btnRestart.Click += btnRestart_Click;
            // 
            // btnClear
            // 
            btnClear.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnClear.BackColor = Color.Black;
            btnClear.ForeColor = Color.White;
            btnClear.Location = new Point(3, 397);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(128, 82);
            btnClear.TabIndex = 8;
            btnClear.Text = "New";
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
            // statusStrip1
            // 
            statusStrip1.BackColor = Color.Black;
            statusStrip1.ImageScalingSize = new Size(24, 24);
            statusStrip1.Items.AddRange(new ToolStripItem[] { tokenUsageLabel });
            statusStrip1.Location = new Point(0, 1037);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1693, 32);
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
            // Form2
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(1693, 1069);
            Controls.Add(statusStrip1);
            Controls.Add(splitContainer1);
            Controls.Add(menuBar);
            MainMenuStrip = menuBar;
            Name = "Form2";
            Text = "Form2";
            FormClosing += Form2_FormClosing;
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
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            splitContainer3.Panel1.ResumeLayout(false);
            splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer3).EndInit();
            splitContainer3.ResumeLayout(false);
            menuBar.ResumeLayout(false);
            menuBar.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
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
        private Button buttonEditTemplate;
        private Button buttonAddTemplate;
        private Button btnRestart;
        private Button buttonStartRecording;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel tokenUsageLabel;
        private Button buttonNewKeepAll;
        private Button buttonAttachImage;
        private TextBox tbSearch;
        private GroupBox groupBox1;
        private Button btnClearSearch;
        private Button btnCancel;
        private Microsoft.Web.WebView2.WinForms.WebView2 ndcWeb;
    }
}