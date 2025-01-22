using AiTool3.Settings;

namespace AiTool3.UI.Forms
{
    partial class ServiceProviderForm
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

        private void InitializeComponent()
        {
            dgvServiceProviders = new AlternatingRowsDataGridView();
            btnOK = new Button();
            btnCancel = new Button();

            ((System.ComponentModel.ISupportInitialize)dgvServiceProviders).BeginInit();
            SuspendLayout();

            // dgvServiceProviders
            dgvServiceProviders.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvServiceProviders.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvServiceProviders.Location = new Point(12, 12);
            dgvServiceProviders.Name = "dgvServiceProviders";
            dgvServiceProviders.Size = new Size(776, 397);
            dgvServiceProviders.TabIndex = 0;

            // btnOK
            btnOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOK.Location = new Point(558, 415);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(112, 34);
            btnOK.TabIndex = 1;
            btnOK.Text = "OK";
            btnOK.Click += btnOK_Click;

            // btnCancel
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.Location = new Point(676, 415);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(112, 34);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Cancel";
            btnCancel.Click += btnCancel_Click;

            // ServiceProviderForm
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 461);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(dgvServiceProviders);
            Name = "ServiceProviderForm";
            Text = "Service Providers";
            ((System.ComponentModel.ISupportInitialize)dgvServiceProviders).EndInit();
            ResumeLayout(false);
        }

        private AlternatingRowsDataGridView dgvServiceProviders;
        private Button btnOK;
        private Button btnCancel;
    }
}