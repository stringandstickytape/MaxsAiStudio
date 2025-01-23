namespace AiTool3.Settings
{
    partial class SettingsForm
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
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            dgvModels = new AlternatingRowsDataGridView();
            btnSettingsOK = new Button();
            btnSettingsCancel = new Button();
            panelToggles = new Panel();
            btnEditServiceProviders = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvModels).BeginInit();
            SuspendLayout();
            // 
            // dgvModels
            // 
            dataGridViewCellStyle1.BackColor = Color.FromArgb(220, 230, 241);
            dataGridViewCellStyle1.ForeColor = Color.Black;
            dataGridViewCellStyle1.SelectionBackColor = Color.Transparent;
            dataGridViewCellStyle1.SelectionForeColor = Color.Black;
            dgvModels.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            dgvModels.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvModels.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = Color.FromArgb(255, 248, 220);
            dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = Color.Black;
            dataGridViewCellStyle2.SelectionBackColor = Color.Transparent;
            dataGridViewCellStyle2.SelectionForeColor = Color.Black;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            dgvModels.DefaultCellStyle = dataGridViewCellStyle2;
            dgvModels.EvenRowColor = Color.FromArgb(220, 230, 241);
            dgvModels.Location = new Point(8, 34);
            dgvModels.Margin = new Padding(2);
            dgvModels.Name = "dgvModels";
            dgvModels.OddRowColor = Color.FromArgb(255, 248, 220);
            dgvModels.RowHeadersWidth = 62;
            dataGridViewCellStyle3.BackColor = Color.FromArgb(255, 248, 220);
            dgvModels.RowsDefaultCellStyle = dataGridViewCellStyle3;
            dgvModels.Size = new Size(790, 190);
            dgvModels.TabIndex = 0;
            // 
            // btnSettingsOK
            // 
            btnSettingsOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSettingsOK.Location = new Point(637, 464);
            btnSettingsOK.Margin = new Padding(2);
            btnSettingsOK.Name = "btnSettingsOK";
            btnSettingsOK.Size = new Size(78, 20);
            btnSettingsOK.TabIndex = 1;
            btnSettingsOK.Text = "OK";
            btnSettingsOK.UseVisualStyleBackColor = true;
            btnSettingsOK.Click += btnSettingsOK_Click;
            // 
            // btnSettingsCancel
            // 
            btnSettingsCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSettingsCancel.Location = new Point(720, 464);
            btnSettingsCancel.Margin = new Padding(2);
            btnSettingsCancel.Name = "btnSettingsCancel";
            btnSettingsCancel.Size = new Size(78, 20);
            btnSettingsCancel.TabIndex = 2;
            btnSettingsCancel.Text = "Cancel";
            btnSettingsCancel.UseVisualStyleBackColor = true;
            btnSettingsCancel.Click += btnSettingsCancel_Click;
            // 
            // panelToggles
            // 
            panelToggles.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panelToggles.AutoScroll = true;
            panelToggles.BorderStyle = BorderStyle.FixedSingle;
            panelToggles.Location = new Point(8, 236);
            panelToggles.Margin = new Padding(2);
            panelToggles.Name = "panelToggles";
            panelToggles.Size = new Size(791, 218);
            panelToggles.TabIndex = 3;
            // 
            // btnEditServiceProviders
            // 
            btnEditServiceProviders.Location = new Point(8, 3);
            btnEditServiceProviders.Name = "btnEditServiceProviders";
            btnEditServiceProviders.Size = new Size(144, 26);
            btnEditServiceProviders.TabIndex = 4;
            btnEditServiceProviders.Text = "Edit Service Providers";
            btnEditServiceProviders.UseVisualStyleBackColor = true;
            btnEditServiceProviders.Click += btnEditServiceProviders_Click;
            // 
            // SettingsForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(806, 489);
            ControlBox = false;
            Controls.Add(btnEditServiceProviders);
            Controls.Add(panelToggles);
            Controls.Add(btnSettingsCancel);
            Controls.Add(btnSettingsOK);
            Controls.Add(dgvModels);
            Margin = new Padding(2);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            Text = "SettingsForm";
            ((System.ComponentModel.ISupportInitialize)dgvModels).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private AlternatingRowsDataGridView dgvModels;
        private Button btnSettingsOK;
        private Button btnSettingsCancel;
        private Panel panelToggles;
        private Button btnEditServiceProviders;
    }
}