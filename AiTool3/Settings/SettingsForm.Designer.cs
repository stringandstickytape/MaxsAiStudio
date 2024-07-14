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
            dgvModels = new DataGridView();
            btnSettingsOK = new Button();
            btnSettingsCancel = new Button();
            panelToggles = new Panel();
            ((System.ComponentModel.ISupportInitialize)dgvModels).BeginInit();
            SuspendLayout();
            // 
            // dgvModels
            // 
            dgvModels.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvModels.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvModels.Location = new Point(12, 12);
            dgvModels.Name = "dgvModels";
            dgvModels.RowHeadersWidth = 62;
            dgvModels.Size = new Size(1128, 362);
            dgvModels.TabIndex = 0;
            // 
            // btnSettingsOK
            // 
            btnSettingsOK.Location = new Point(910, 774);
            btnSettingsOK.Name = "btnSettingsOK";
            btnSettingsOK.Size = new Size(112, 34);
            btnSettingsOK.TabIndex = 1;
            btnSettingsOK.Text = "OK";
            btnSettingsOK.UseVisualStyleBackColor = true;
            btnSettingsOK.Click += btnSettingsOK_Click;
            // 
            // btnSettingsCancel
            // 
            btnSettingsCancel.Location = new Point(1028, 774);
            btnSettingsCancel.Name = "btnSettingsCancel";
            btnSettingsCancel.Size = new Size(112, 34);
            btnSettingsCancel.TabIndex = 2;
            btnSettingsCancel.Text = "Cancel";
            btnSettingsCancel.UseVisualStyleBackColor = true;
            btnSettingsCancel.Click += btnSettingsCancel_Click;
            // 
            // panelToggles
            // 
            panelToggles.Location = new Point(11, 393);
            panelToggles.Name = "panelToggles";
            panelToggles.Size = new Size(1129, 375);
            panelToggles.TabIndex = 3;
            // 
            // SettingsForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1152, 815);
            ControlBox = false;
            Controls.Add(panelToggles);
            Controls.Add(btnSettingsCancel);
            Controls.Add(btnSettingsOK);
            Controls.Add(dgvModels);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            Text = "SettingsForm";
            ((System.ComponentModel.ISupportInitialize)dgvModels).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DataGridView dgvModels;
        private Button btnSettingsOK;
        private Button btnSettingsCancel;
        private Panel panelToggles;
    }
}