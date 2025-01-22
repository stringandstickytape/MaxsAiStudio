namespace AiTool3.Settings
{
    partial class ModelEditForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            lblFriendlyName = new Label();
            txtFriendlyName = new TextBox();
            txtModelName = new TextBox();
            lblModelName = new Label();
            lblAiService = new Label();
            cboAiService = new ComboBox();
            txtModelUrl = new TextBox();
            lblModelUrl = new Label();
            txtModelKey = new TextBox();
            lblModelKey = new Label();
            txtInputPrice = new TextBox();
            lblInputPrice = new Label();
            txtOutputPrice = new TextBox();
            lblOutputPrice = new Label();
            txtColor = new TextBox();
            lblColor = new Label();
            btnOK = new Button();
            btnCancel = new Button();
            lblServiceProvider = new Label();
            cboServiceProvider = new ComboBox();
            SuspendLayout();
            // 
            // lblFriendlyName
            // 
            lblFriendlyName.AutoSize = true;
            lblFriendlyName.Location = new Point(12, 9);
            lblFriendlyName.Name = "lblFriendlyName";
            lblFriendlyName.Size = new Size(87, 15);
            lblFriendlyName.TabIndex = 0;
            lblFriendlyName.Text = "Friendly Name:";
            // 
            // txtFriendlyName
            // 
            txtFriendlyName.Location = new Point(150, 6);
            txtFriendlyName.Name = "txtFriendlyName";
            txtFriendlyName.Size = new Size(200, 23);
            txtFriendlyName.TabIndex = 1;
            // 
            // txtModelName
            // 
            txtModelName.Location = new Point(150, 35);
            txtModelName.Name = "txtModelName";
            txtModelName.Size = new Size(200, 23);
            txtModelName.TabIndex = 3;
            // 
            // lblModelName
            // 
            lblModelName.AutoSize = true;
            lblModelName.Location = new Point(12, 38);
            lblModelName.Name = "lblModelName";
            lblModelName.Size = new Size(79, 15);
            lblModelName.TabIndex = 2;
            lblModelName.Text = "Model Name:";
            // 
            // lblAiService
            // 
            lblAiService.AutoSize = true;
            lblAiService.Location = new Point(12, 67);
            lblAiService.Name = "lblAiService";
            lblAiService.Size = new Size(61, 15);
            lblAiService.TabIndex = 4;
            lblAiService.Text = "AI Service:";
            // 
            // cboAiService
            // 
            cboAiService.FormattingEnabled = true;
            cboAiService.Location = new Point(150, 64);
            cboAiService.Name = "cboAiService";
            cboAiService.Size = new Size(200, 23);
            cboAiService.TabIndex = 5;
            // 
            // txtModelUrl
            // 
            txtModelUrl.Location = new Point(150, 93);
            txtModelUrl.Name = "txtModelUrl";
            txtModelUrl.Size = new Size(200, 23);
            txtModelUrl.TabIndex = 7;
            // 
            // lblModelUrl
            // 
            lblModelUrl.AutoSize = true;
            lblModelUrl.Location = new Point(12, 96);
            lblModelUrl.Name = "lblModelUrl";
            lblModelUrl.Size = new Size(62, 15);
            lblModelUrl.TabIndex = 6;
            lblModelUrl.Text = "Model Url:";
            // 
            // txtModelKey
            // 
            txtModelKey.Location = new Point(150, 122);
            txtModelKey.Name = "txtModelKey";
            txtModelKey.Size = new Size(200, 23);
            txtModelKey.TabIndex = 9;
            // 
            // lblModelKey
            // 
            lblModelKey.AutoSize = true;
            lblModelKey.Location = new Point(12, 125);
            lblModelKey.Name = "lblModelKey";
            lblModelKey.Size = new Size(66, 15);
            lblModelKey.TabIndex = 8;
            lblModelKey.Text = "Model Key:";
            // 
            // txtInputPrice
            // 
            txtInputPrice.Location = new Point(150, 151);
            txtInputPrice.Name = "txtInputPrice";
            txtInputPrice.Size = new Size(200, 23);
            txtInputPrice.TabIndex = 11;
            // 
            // lblInputPrice
            // 
            lblInputPrice.AutoSize = true;
            lblInputPrice.Location = new Point(12, 154);
            lblInputPrice.Name = "lblInputPrice";
            lblInputPrice.Size = new Size(119, 15);
            lblInputPrice.TabIndex = 10;
            lblInputPrice.Text = "Input 1MToken Price:";
            // 
            // txtOutputPrice
            // 
            txtOutputPrice.Location = new Point(150, 180);
            txtOutputPrice.Name = "txtOutputPrice";
            txtOutputPrice.Size = new Size(200, 23);
            txtOutputPrice.TabIndex = 13;
            // 
            // lblOutputPrice
            // 
            lblOutputPrice.AutoSize = true;
            lblOutputPrice.Location = new Point(12, 183);
            lblOutputPrice.Name = "lblOutputPrice";
            lblOutputPrice.Size = new Size(129, 15);
            lblOutputPrice.TabIndex = 12;
            lblOutputPrice.Text = "Output 1MToken Price:";
            // 
            // txtColor
            // 
            txtColor.Location = new Point(150, 209);
            txtColor.Name = "txtColor";
            txtColor.Size = new Size(200, 23);
            txtColor.TabIndex = 15;
            // 
            // lblColor
            // 
            lblColor.AutoSize = true;
            lblColor.Location = new Point(12, 212);
            lblColor.Name = "lblColor";
            lblColor.Size = new Size(39, 15);
            lblColor.TabIndex = 14;
            lblColor.Text = "Color:";
            // 
            // btnOK
            // 
            btnOK.Location = new Point(194, 271);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(75, 23);
            btnOK.TabIndex = 16;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(275, 271);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 23);
            btnCancel.TabIndex = 17;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // lblServiceProvider
            // 
            lblServiceProvider.AutoSize = true;
            lblServiceProvider.Location = new Point(12, 241);
            lblServiceProvider.Name = "lblServiceProvider";
            lblServiceProvider.Size = new Size(94, 15);
            lblServiceProvider.TabIndex = 18;
            lblServiceProvider.Text = "Service Provider:";
            // 
            // cboServiceProvider
            // 
            cboServiceProvider.FormattingEnabled = true;
            cboServiceProvider.Location = new Point(150, 238);
            cboServiceProvider.Name = "cboServiceProvider";
            cboServiceProvider.Size = new Size(200, 23);
            cboServiceProvider.TabIndex = 19;
            // 
            // ModelEditForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(364, 303);
            Controls.Add(cboServiceProvider);
            Controls.Add(lblServiceProvider);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(txtColor);
            Controls.Add(lblColor);
            Controls.Add(txtOutputPrice);
            Controls.Add(lblOutputPrice);
            Controls.Add(txtInputPrice);
            Controls.Add(lblInputPrice);
            Controls.Add(txtModelKey);
            Controls.Add(lblModelKey);
            Controls.Add(txtModelUrl);
            Controls.Add(lblModelUrl);
            Controls.Add(cboAiService);
            Controls.Add(lblAiService);
            Controls.Add(txtModelName);
            Controls.Add(lblModelName);
            Controls.Add(txtFriendlyName);
            Controls.Add(lblFriendlyName);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ModelEditForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Edit Model";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblFriendlyName;
        private System.Windows.Forms.TextBox txtFriendlyName;
        private System.Windows.Forms.TextBox txtModelName;
        private System.Windows.Forms.Label lblModelName;
        private System.Windows.Forms.Label lblAiService;
        private System.Windows.Forms.ComboBox cboAiService;
        private System.Windows.Forms.TextBox txtModelUrl;
        private System.Windows.Forms.Label lblModelUrl;
        private System.Windows.Forms.TextBox txtModelKey;
        private System.Windows.Forms.Label lblModelKey;
        private System.Windows.Forms.TextBox txtInputPrice;
        private System.Windows.Forms.Label lblInputPrice;
        private System.Windows.Forms.TextBox txtOutputPrice;
        private System.Windows.Forms.Label lblOutputPrice;
        private System.Windows.Forms.TextBox txtColor;
        private System.Windows.Forms.Label lblColor;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblServiceProvider;
        private System.Windows.Forms.ComboBox cboServiceProvider;
    }
}