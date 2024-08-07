namespace AiTool3.UI
{
    partial class AutoSuggestUserInput
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
            label1 = new Label();
            tbAutoSuggestUserInput = new TextBox();
            label2 = new Label();
            btnAutoSuggestOK = new Button();
            btnAutoSuggestCancel = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(16, 19);
            label1.Name = "label1";
            label1.Size = new Size(59, 25);
            label1.TabIndex = 0;
            label1.Text = "label1";
            // 
            // tbAutoSuggestUserInput
            // 
            tbAutoSuggestUserInput.Location = new Point(17, 59);
            tbAutoSuggestUserInput.Name = "tbAutoSuggestUserInput";
            tbAutoSuggestUserInput.Size = new Size(544, 31);
            tbAutoSuggestUserInput.TabIndex = 1;
            tbAutoSuggestUserInput.Text = "fun and interesting";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(22, 107);
            label2.Name = "label2";
            label2.Size = new Size(59, 25);
            label2.TabIndex = 2;
            label2.Text = "label2";
            // 
            // btnAutoSuggestOK
            // 
            btnAutoSuggestOK.Location = new Point(449, 184);
            btnAutoSuggestOK.Name = "btnAutoSuggestOK";
            btnAutoSuggestOK.Size = new Size(112, 34);
            btnAutoSuggestOK.TabIndex = 3;
            btnAutoSuggestOK.Text = "OK";
            btnAutoSuggestOK.UseVisualStyleBackColor = true;
            btnAutoSuggestOK.Click += btnAutoSuggestOK_Click;
            // 
            // btnAutoSuggestCancel
            // 
            btnAutoSuggestCancel.Location = new Point(331, 184);
            btnAutoSuggestCancel.Name = "btnAutoSuggestCancel";
            btnAutoSuggestCancel.Size = new Size(112, 34);
            btnAutoSuggestCancel.TabIndex = 4;
            btnAutoSuggestCancel.Text = "Cancel";
            btnAutoSuggestCancel.UseVisualStyleBackColor = true;
            btnAutoSuggestCancel.Click += btnAutoSuggestCancel_Click;
            // 
            // AutoSuggestUserInput
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(573, 230);
            Controls.Add(btnAutoSuggestCancel);
            Controls.Add(btnAutoSuggestOK);
            Controls.Add(label2);
            Controls.Add(tbAutoSuggestUserInput);
            Controls.Add(label1);
            Name = "AutoSuggestUserInput";
            Text = "AutoSuggestUserInput";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox tbAutoSuggestUserInput;
        private Label label2;
        private Button btnAutoSuggestOK;
        private Button btnAutoSuggestCancel;
    }
}