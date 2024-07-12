namespace AiTool3.Helpers
{
    internal static class SimpleDialogsHelper
    {

        public static DialogResult ShowAttachmentDialog()
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Attach image or text?",
                StartPosition = FormStartPosition.CenterScreen
            };

            Button imageButton = new Button() { Left = 50, Top = 30, AutoSize = true, Text = "Image" };
            Button textButton = new Button() { Left = 150, Top = 30, AutoSize = true, Text = "Text" };
            Button cancelButton = new Button() { Left = 260, Top = 30, AutoSize = true, Text = "Cancel" };

            imageButton.Click += (sender, e) => { prompt.DialogResult = DialogResult.Yes; };
            textButton.Click += (sender, e) => { prompt.DialogResult = DialogResult.No; };
            cancelButton.Click += (sender, e) => { prompt.DialogResult = DialogResult.Cancel; };

            prompt.Controls.Add(imageButton);
            prompt.Controls.Add(textButton);
            prompt.Controls.Add(cancelButton);

            return prompt.ShowDialog();
        }
    }
}