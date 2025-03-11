namespace DiffLibrary
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            textBox1 = new TextBox();
            button1 = new Button();
            tbRootPath = new TextBox();
            tbOutput = new TextBox();
            SuspendLayout();
            // 
            // textBox1
            // 
            textBox1.Location = new Point(42, 120);
            textBox1.MaxLength = 999999999;
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(719, 243);
            textBox1.TabIndex = 0;
            // 
            // button1
            // 
            button1.Location = new Point(621, 381);
            button1.Name = "button1";
            button1.Size = new Size(112, 34);
            button1.TabIndex = 2;
            button1.Text = "button1";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // tbRootPath
            // 
            tbRootPath.Location = new Point(45, 47);
            tbRootPath.Name = "tbRootPath";
            tbRootPath.Size = new Size(716, 31);
            tbRootPath.TabIndex = 3;
            tbRootPath.Text = "C:\\Users\\maxhe\\source\\repos\\CloneTest\\MaxsAiTool\\";
            // 
            // tbOutput
            // 
            tbOutput.Location = new Point(30, 482);
            tbOutput.MaxLength = 3276700;
            tbOutput.Multiline = true;
            tbOutput.Name = "tbOutput";
            tbOutput.Size = new Size(716, 348);
            tbOutput.TabIndex = 4;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 846);
            Controls.Add(tbOutput);
            Controls.Add(tbRootPath);
            Controls.Add(button1);
            Controls.Add(textBox1);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox textBox1;
        private Button button1;
        private TextBox tbRootPath;
        private TextBox tbOutput;
    }
}
