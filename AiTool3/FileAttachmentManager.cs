using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using AiTool3.ExtensionMethods;
using AiTool3.Helpers;
using AiTool3.UI;

namespace AiTool3
{
    public class FileAttachmentManager
    {
        private readonly ChatWebView _chatWebView;
        private readonly SettingsSet _settings;

        public string? Base64Image { get; private set; }
        public string? Base64ImageType { get; private set; }

        public FileAttachmentManager(ChatWebView chatWebView, SettingsSet settings)
        {
            _chatWebView = chatWebView;
            _settings = settings;
        }

        public async Task HandleAttachment()
        {
            var result = SimpleDialogsHelper.ShowAttachmentDialog();

            switch (result)
            {
                case DialogResult.Retry:
                    await AttachAndTranscribeMP4();
                    break;
                case DialogResult.Yes:
                    AttachImage();
                    break;
                case DialogResult.No:
                    await AttachTextFiles();
                    break;
            }
        }

        private async Task AttachAndTranscribeMP4()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "MP4 files (*.mp4)|*.mp4|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                await TranscribeMP4(openFileDialog.FileName);
            }
        }

        private void AttachImage()
        {
            OpenFileDialog openFileDialog = ImageHelpers.ShowAttachImageFileDialog(_settings.DefaultPath);

            if (openFileDialog.FileName != "")
            {
                Base64Image = ImageHelpers.ImageToBase64(openFileDialog.FileName);
                Base64ImageType = ImageHelpers.GetImageType(openFileDialog.FileName);
                _settings.SetDefaultPath(Path.GetDirectoryName(openFileDialog.FileName)!);
            }
        }

        private async Task AttachTextFiles()
        {
            OpenFileDialog attachTextFilesDialog = ImageHelpers.ShowAttachTextFilesDialog(_settings.DefaultPath);

            if (attachTextFilesDialog.FileNames.Length > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var file in attachTextFilesDialog.FileNames)
                {
                    sb.AppendMany(Form2.ThreeTicks,
                        Path.GetFileName(file),
                        Environment.NewLine,
                        File.ReadAllText(file),
                        Environment.NewLine,
                        Form2.ThreeTicks,
                        Environment.NewLine,
                        Environment.NewLine);
                }

                var existingPrompt = await _chatWebView.GetUserPrompt();
                await _chatWebView.SetUserPrompt($"{sb}{existingPrompt}");

                _settings.SetDefaultPath(Path.GetDirectoryName(attachTextFilesDialog.FileName)!);
            }
        }

        private async Task TranscribeMP4(string fileName)
        {
            // Implement MP4 transcription logic here
            throw new NotImplementedException("MP4 transcription not implemented yet.");
        }
    }
}