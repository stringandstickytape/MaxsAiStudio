using System.Drawing;
using System.Windows.Forms;

namespace AiTool3.Helpers
{
    public static class SimpleDialogsHelper
    {
        internal static readonly int ButtonWidth = 120;
        internal static readonly int ButtonHeight = 40;
        internal static readonly int Margin = 20;
        internal static readonly int ThumbnailSize = 160;

        public static DialogResult ShowAttachmentDialog()
        {
            var dialog = CreateBaseDialog();
            AddStandardButtons(dialog);

            if (Clipboard.ContainsImage())
            {
                AddClipboardImageControls(dialog);
            }

            return dialog.ShowDialog();
        }

        public static Form CreateBaseDialog()
        {
            return new Form
            {
                Width = 500,
                Height = 500,
                Text = "Attach image or text?",
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.FromArgb(60, 60, 60)
            };
        }

        private static void AddStandardButtons(Form dialog)
        {
            var imageButton = CreateButton(dialog, "Image", Margin, Margin);
            var videoButton = CreateButton(dialog, "Media\r\nTranscript", Margin * 2 + ButtonWidth, Margin);
            var textButton = CreateButton(dialog, "Text", Margin * 3 + ButtonWidth * 2, Margin);
            var cancelButton = CreateButton(dialog, "Cancel", dialog.Width - ButtonWidth - Margin, dialog.Height - ButtonHeight - Margin);

            imageButton.Click += (sender, e) => { dialog.DialogResult = DialogResult.Yes; };
            videoButton.Click += (sender, e) => { dialog.DialogResult = DialogResult.Retry; };
            textButton.Click += (sender, e) => { dialog.DialogResult = DialogResult.No; };
            cancelButton.Click += (sender, e) => { dialog.DialogResult = DialogResult.Cancel; };

            dialog.Controls.AddRange(new Control[] { imageButton, videoButton, textButton, cancelButton });
        }

        private static Button CreateButton(Form dialog, string text, int left, int top)
        {
            return new Button
            {
                ForeColor = Color.White,
                Left = left,
                Top = top,
                Width = ButtonWidth,
                Height = ButtonHeight,
                Text = text
            };


        }

        private static void AddClipboardImageControls(Form dialog)
        {
            var image = Clipboard.GetImage();
            var clipboardImageButton = CreateButton(dialog, "Clipboard Image", Margin, Margin * 2 + ButtonHeight);
            clipboardImageButton.Width = ButtonWidth * 2;
            dialog.Controls.Add(clipboardImageButton);

            var (width, height) = CalculateImageDimensions(image);
            var borderedImage = CreateBorderedImage(image, width, height);
            var pictureBox = CreatePictureBox(dialog, borderedImage, width, height);

            clipboardImageButton.Click += (sender, e) => { dialog.DialogResult = DialogResult.Continue; };
            pictureBox.Click += (sender, e) => { dialog.DialogResult = DialogResult.Continue; };
        }

        private static (int width, int height) CalculateImageDimensions(Image image)
        {
            int width, height;
            if (image.Width > image.Height)
            {
                height = (int)(image.Height * (ThumbnailSize / (double)image.Width));
                width = ThumbnailSize;
            }
            else
            {
                width = (int)(image.Width * (ThumbnailSize / (double)image.Height));
                height = ThumbnailSize;
            }
            return (width + 2, height + 2);
        }

        private static Image CreateBorderedImage(Image originalImage, int width, int height)
        {
            using var bmp = new Bitmap(width, height);
            using var g = Graphics.FromImage(bmp);
            
            g.Clear(Color.White);
            g.DrawImage(originalImage, 1, 1, width - 2, height - 2);
            
            // Draw black border on right and bottom
            g.DrawLine(Pens.Black, width - 1, 0, width - 1, height - 1);
            g.DrawLine(Pens.Black, 0, height - 1, width - 1, height - 1);

            return bmp.Clone() as Image;
        }

        private static PictureBox CreatePictureBox(Form dialog, Image image, int width, int height)
        {
            var pictureBox = new PictureBox
            {
                Left = Margin,
                Top = Margin * 2 + ButtonHeight + 30,
                Width = width,
                Height = height,
                Image = image.GetThumbnailImage(width, height, null, IntPtr.Zero)
            };
            
            dialog.Controls.Add(pictureBox);
            return pictureBox;
        }
    }
}