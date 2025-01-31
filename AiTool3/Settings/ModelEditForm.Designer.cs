using System;
using System.Drawing;
using System.Windows.Forms;

namespace AiTool3.Settings
{
    partial class ModelEditForm
    {
        private System.ComponentModel.IContainer components = null;
        private Label lblFriendlyName;
        private TextBox txtFriendlyName;
        private Label lblAdditionalParams;
        private TextBox txtAdditionalParams;
        private TextBox txtModelName;
        private Label lblModelName;
        private TextBox txtInputPrice;
        private Label lblInputPrice;
        private TextBox txtOutputPrice;
        private Label lblOutputPrice;
        private Button btnColorPicker;
        private ColorDialog colorDialog;
        private Label lblColor;
        private Button btnOK;
        private Button btnCancel;
        private Label lblServiceProvider;
        private ComboBox cboServiceProvider;

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
            this.lblFriendlyName = new Label();
            this.txtFriendlyName = new TextBox();
            this.lblAdditionalParams = new Label();
            this.txtAdditionalParams = new TextBox();
            this.txtModelName = new TextBox();
            this.lblModelName = new Label();
            this.txtInputPrice = new TextBox();
            this.lblInputPrice = new Label();
            this.txtOutputPrice = new TextBox();
            this.lblOutputPrice = new Label();
            this.btnColorPicker = new Button();
            this.colorDialog = new ColorDialog();
            this.lblColor = new Label();
            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.lblServiceProvider = new Label();
            this.cboServiceProvider = new ComboBox();
            this.SuspendLayout();
            // 
            // lblFriendlyName
            // 
            this.lblFriendlyName.AutoSize = true;
            this.lblFriendlyName.Location = new Point(12, 9);
            this.lblFriendlyName.Name = "lblFriendlyName";
            this.lblFriendlyName.Size = new Size(87, 15);
            this.lblFriendlyName.TabIndex = 0;
            this.lblFriendlyName.Text = "Friendly Name:";
            // 
            // txtFriendlyName
            // 
            this.txtFriendlyName.Location = new Point(150, 6);
            this.txtFriendlyName.Name = "txtFriendlyName";
            this.txtFriendlyName.Size = new Size(200, 23);
            this.txtFriendlyName.TabIndex = 1;
            // 
            // lblAdditionalParams
            // 
            this.lblAdditionalParams.AutoSize = true;
            this.lblAdditionalParams.Location = new Point(12, 67);
            this.lblAdditionalParams.Name = "lblAdditionalParams";
            this.lblAdditionalParams.Size = new Size(110, 15);
            this.lblAdditionalParams.TabIndex = 2;
            this.lblAdditionalParams.Text = "Additional Params:";
            // 
            // txtAdditionalParams
            // 
            this.txtAdditionalParams.Location = new Point(150, 64);
            this.txtAdditionalParams.Name = "txtAdditionalParams";
            this.txtAdditionalParams.Size = new Size(200, 23);
            this.txtAdditionalParams.TabIndex = 3;
            // 
            // txtModelName
            // 
            this.txtModelName.Location = new Point(150, 35);
            this.txtModelName.Name = "txtModelName";
            this.txtModelName.Size = new Size(200, 23);
            this.txtModelName.TabIndex = 4;
            // 
            // lblModelName
            // 
            this.lblModelName.AutoSize = true;
            this.lblModelName.Location = new Point(12, 38);
            this.lblModelName.Name = "lblModelName";
            this.lblModelName.Size = new Size(79, 15);
            this.lblModelName.TabIndex = 5;
            this.lblModelName.Text = "Model Name:";
            // 
            // txtInputPrice
            // 
            this.txtInputPrice.Location = new Point(150, 181);
            this.txtInputPrice.Name = "txtInputPrice";
            this.txtInputPrice.Size = new Size(200, 23);
            this.txtInputPrice.TabIndex = 6;
            // 
            // lblInputPrice
            // 
            this.lblInputPrice.AutoSize = true;
            this.lblInputPrice.Location = new Point(12, 184);
            this.lblInputPrice.Name = "lblInputPrice";
            this.lblInputPrice.Size = new Size(119, 15);
            this.lblInputPrice.TabIndex = 7;
            this.lblInputPrice.Text = "Input 1MToken Price:";
            // 
            // txtOutputPrice
            // 
            this.txtOutputPrice.Location = new Point(150, 210);
            this.txtOutputPrice.Name = "txtOutputPrice";
            this.txtOutputPrice.Size = new Size(200, 23);
            this.txtOutputPrice.TabIndex = 8;
            // 
            // lblOutputPrice
            // 
            this.lblOutputPrice.AutoSize = true;
            this.lblOutputPrice.Location = new Point(12, 213);
            this.lblOutputPrice.Name = "lblOutputPrice";
            this.lblOutputPrice.Size = new Size(129, 15);
            this.lblOutputPrice.TabIndex = 9;
            this.lblOutputPrice.Text = "Output 1MToken Price:";
            // 
            // btnColorPicker
            // 
            this.btnColorPicker.Location = new Point(150, 239);
            this.btnColorPicker.Name = "btnColorPicker";
            this.btnColorPicker.Size = new Size(200, 23);
            this.btnColorPicker.TabIndex = 10;
            this.btnColorPicker.UseVisualStyleBackColor = true;
            this.btnColorPicker.Click += new EventHandler(this.btnColorPicker_Click);
            // 
            // lblColor
            // 
            this.lblColor.AutoSize = true;
            this.lblColor.Location = new Point(12, 242);
            this.lblColor.Name = "lblColor";
            this.lblColor.Size = new Size(39, 15);
            this.lblColor.TabIndex = 11;
            this.lblColor.Text = "Color:";
            // 
            // btnOK
            // 
            this.btnOK.Location = new Point(194, 301);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new Size(75, 23);
            this.btnOK.TabIndex = 12;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new Point(275, 301);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new Size(75, 23);
            this.btnCancel.TabIndex = 13;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new EventHandler(this.btnCancel_Click);
            // 
            // lblServiceProvider
            // 
            this.lblServiceProvider.AutoSize = true;
            this.lblServiceProvider.Location = new Point(12, 271);
            this.lblServiceProvider.Name = "lblServiceProvider";
            this.lblServiceProvider.Size = new Size(94, 15);
            this.lblServiceProvider.TabIndex = 14;
            this.lblServiceProvider.Text = "Service Provider:";
            // 
            // cboServiceProvider
            // 
            this.cboServiceProvider.FormattingEnabled = true;
            this.cboServiceProvider.Location = new Point(150, 268);
            this.cboServiceProvider.Name = "cboServiceProvider";
            this.cboServiceProvider.Size = new Size(200, 23);
            this.cboServiceProvider.TabIndex = 15;
            // 
            // ModelEditForm
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(364, 333);
            this.Controls.Add(this.cboServiceProvider);
            this.Controls.Add(this.lblServiceProvider);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnColorPicker);
            this.Controls.Add(this.lblColor);
            this.Controls.Add(this.txtOutputPrice);
            this.Controls.Add(this.lblOutputPrice);
            this.Controls.Add(this.txtInputPrice);
            this.Controls.Add(this.lblInputPrice);
            this.Controls.Add(this.txtModelName);
            this.Controls.Add(this.lblModelName);
            this.Controls.Add(this.txtAdditionalParams);
            this.Controls.Add(this.lblAdditionalParams);
            this.Controls.Add(this.txtFriendlyName);
            this.Controls.Add(this.lblFriendlyName);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ModelEditForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Edit Model";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}