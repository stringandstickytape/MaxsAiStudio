namespace AiTool3.Helpers
{
    internal static class SimpleDialogsHelper
    {

        public static DialogResult ShowAttachmentDialog()
        {
            Form prompt = new Form()
            {
                Width = 500,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Attach image or text?",
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.FromArgb(60, 60, 60)
            };

            Button imageButton = new Button() { ForeColor = Color.White, Left = 30, Top = 30, AutoSize = true, Text = "Image" };
            Button videoButton = new Button() { ForeColor = Color.White, Left = 120, Top = 15, AutoSize = true, Text = "Media\r\nTranscript" };
            Button textButton = new Button()  { ForeColor = Color.White, Left = 230, Top = 30, AutoSize = true, Text = "Text" };
            Button cancelButton = new Button(){ ForeColor = Color.White, Left = 360, Top = 30, AutoSize = true, Text = "Cancel" };

            imageButton.Click += (sender, e) => { prompt.DialogResult = DialogResult.Yes; };
            textButton.Click += (sender, e) => { prompt.DialogResult = DialogResult.No; };
            cancelButton.Click += (sender, e) => { prompt.DialogResult = DialogResult.Cancel; };
            videoButton.Click += (sender, e) => { prompt.DialogResult = DialogResult.Retry; };

            prompt.Controls.Add(imageButton);
            prompt.Controls.Add(videoButton);
            prompt.Controls.Add(textButton);
            prompt.Controls.Add(cancelButton);

            return prompt.ShowDialog();
        }
    }
}