namespace AiTool3.Helpers
{
    internal static class SimpleDialogsHelper
    {
        public static DialogResult ShowAttachmentDialog()
        {
            Form prompt = new Form()
            {
                Width = 500,
                Height = 500,
                Text = "Attach image or text?",
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.FromArgb(60, 60, 60)
            };

            int buttonWidth = 120;
            int buttonHeight = 40;
            int margin = 20;

            Button imageButton = new Button() { ForeColor = Color.White, Left = margin, Top = margin, Width = buttonWidth, Height = buttonHeight, Text = "Image" };
            Button videoButton = new Button() { ForeColor = Color.White, Left = margin * 2 + buttonWidth, Top = margin, Width = buttonWidth, Height = buttonHeight, Text = "Media\r\nTranscript" };
            Button textButton = new Button() { ForeColor = Color.White, Left = margin * 3 + buttonWidth * 2, Top = margin, Width = buttonWidth, Height = buttonHeight, Text = "Text" };
            Button cancelButton = new Button() { ForeColor = Color.White, Left = prompt.Width - buttonWidth - margin, Top = prompt.Height - buttonHeight - margin, Width = buttonWidth, Height = buttonHeight, Text = "Cancel" };

            prompt.Controls.Add(imageButton);
            prompt.Controls.Add(videoButton);
            prompt.Controls.Add(textButton);
            prompt.Controls.Add(cancelButton);

            if (Clipboard.ContainsImage())
            {
                var image = Clipboard.GetImage();
                Button clipboardImageButton = new Button() { ForeColor = Color.White, Left = margin, Top = margin * 2 + buttonHeight, Width = buttonWidth * 2, Height = buttonHeight, Text = "Clipboard Image" };
                prompt.Controls.Add(clipboardImageButton);

                var w = 160;
                var h = 160;

                if (image.Width > image.Height)
                {
                    h = (int)(image.Height * (160.0 / image.Width));
                    w = 160;
                }
                else
                {
                    w = (int)(image.Width * (160.0 / image.Height));
                    h = 160;
                }
                w += 2;
                h += 2;
                // draw a one-pixel white border around the image
                using (Bitmap bmp = new Bitmap(w, h))
                {
                    Graphics g = Graphics.FromImage(bmp);
                    g.Clear(Color.White);
                    g.DrawImage(image, 1, 1, w, h);

                    // on the right and bottom, draw a black border
                    g.DrawLine(Pens.Black, w - 1, 0, w - 1, h - 1);
                    g.DrawLine(Pens.Black, 0, h - 1, w - 1, h - 1);


                    g.Dispose();
                    image = bmp;
                    PictureBox pictureBox = new PictureBox() { Left = margin, Top = margin * 2 + buttonHeight + 30, Width = w, Height = h, Image = image.GetThumbnailImage(w, h, null, IntPtr.Zero) };
                    pictureBox.Click += (sender, e) => { prompt.DialogResult = DialogResult.Continue; };
                    prompt.Controls.Add(pictureBox);
                }

                clipboardImageButton.Click += (sender, e) => { prompt.DialogResult = DialogResult.Continue; };
            }

            imageButton.Click += (sender, e) => { prompt.DialogResult = DialogResult.Yes; };
            textButton.Click += (sender, e) => { prompt.DialogResult = DialogResult.No; };
            cancelButton.Click += (sender, e) => { prompt.DialogResult = DialogResult.Cancel; };
            videoButton.Click += (sender, e) => { prompt.DialogResult = DialogResult.Retry; };

            return prompt.ShowDialog();
        }
    }
}