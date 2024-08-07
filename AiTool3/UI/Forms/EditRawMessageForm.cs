namespace AiTool3.UI.Forms
{
    public class EditRawMessageForm : Form
    {
        public string EditedContent { get; private set; }

        public EditRawMessageForm(string initialContent)
        {
            InitializeComponent();
            textBoxContent.Text = initialContent.Replace("\r\n", "\n").Replace("\n", "\r\n");
        }

        private void InitializeComponent()
        {
            textBoxContent = new TextBox();
            buttonOK = new Button();
            buttonCancel = new Button();
            SuspendLayout();
            // 
            // textBoxContent
            // 
            textBoxContent.Anchor = AnchorStyles.Top | AnchorStyles.Bottom
            | AnchorStyles.Left
            | AnchorStyles.Right;
            textBoxContent.Location = new Point(12, 12);
            textBoxContent.Multiline = true;
            textBoxContent.Name = "textBoxContent";
            textBoxContent.ScrollBars = ScrollBars.Vertical;
            textBoxContent.Size = new Size(776, 397);
            textBoxContent.TabIndex = 0;
            // 
            // buttonOK
            // 
            buttonOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonOK.DialogResult = DialogResult.OK;
            buttonOK.Location = new Point(632, 415);
            buttonOK.Name = "buttonOK";
            buttonOK.Size = new Size(75, 23);
            buttonOK.TabIndex = 1;
            buttonOK.Text = "OK";
            buttonOK.UseVisualStyleBackColor = true;
            buttonOK.Click += new EventHandler(buttonOK_Click);
            // 
            // buttonCancel
            // 
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.DialogResult = DialogResult.Cancel;
            buttonCancel.Location = new Point(713, 415);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(75, 23);
            buttonCancel.TabIndex = 2;
            buttonCancel.Text = "Cancel";
            buttonCancel.UseVisualStyleBackColor = true;
            // 
            // EditRawMessageForm
            // 
            AcceptButton = buttonOK;
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = buttonCancel;
            ClientSize = new Size(800, 450);
            Controls.Add(buttonCancel);
            Controls.Add(buttonOK);
            Controls.Add(textBoxContent);
            Name = "EditRawMessageForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Edit Raw Message";
            ResumeLayout(false);
            PerformLayout();

        }

        private TextBox textBoxContent;
        private Button buttonOK;
        private Button buttonCancel;

        private void buttonOK_Click(object sender, EventArgs e)
        {
            EditedContent = textBoxContent.Text.Replace("\r\n", "\n");
        }
    }
}