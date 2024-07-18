using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using AiTool3.ExtensionMethods;
using AiTool3.Helpers;
using AiTool3.UI;
using Newtonsoft.Json.Linq;

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

        public async Task HandleAttachment(ChatWebView chatWebView)
        {
            var result = SimpleDialogsHelper.ShowAttachmentDialog();

            switch (result)
            {
                case DialogResult.Retry:
                    await AttachAndTranscribeMP4(chatWebView);
                    break;
                case DialogResult.Yes:
                    AttachImage();
                    break;
                case DialogResult.No:
                    await AttachTextFiles();
                    break;
            }
        }

        private async Task AttachAndTranscribeMP4(ChatWebView chatWebView)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "MP4 files (*.mp4)|*.mp4|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                await TranscribeMP4(openFileDialog.FileName, chatWebView);
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

        public async Task TranscribeMP4(string filename, ChatWebView chatWebView)
        {
            // Path to the Miniconda installation
            string condaPath = @"C:\ProgramData\miniconda3\Scripts\activate.bat";

            // Command to activate the WhisperX environment and run Whisper
            string arguments = $"/C {condaPath} && conda activate whisperx && whisperx \"{filename}\" --output_format json";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.OutputDataReceived += (sender, e) => Debug.WriteLine(e.Data);
                process.ErrorDataReceived += (sender, e) => Debug.WriteLine(e.Data);

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    // stip filename from path
                    var filenameOnly = filename.Split('\\').Last();

                    if (filenameOnly.Contains("."))
                    {
                        filenameOnly = filenameOnly.Substring(0, filenameOnly.LastIndexOf('.')) + ".json";
                    }
                    else
                    {
                        filenameOnly += ".json";
                    }
                    // add path back in

                    var json = File.ReadAllText(filenameOnly);


                    // deserz to dynamic, and get the object's segments array

                    List<string> result = new List<string>();

                    dynamic jsonObj = JObject.Parse(json);

                    foreach (var segment in jsonObj.segments)
                    {
                        double start = segment.start;
                        double end = segment.end;
                        string text = segment.text;
                        string formattedText = $"[{start:F3} - {end:F3}] {text.Trim()}";
                        result.Add(formattedText);
                    }

                    var output = $"{Form2.ThreeTicks}{filename.Split('\\').Last()}{Environment.NewLine}{string.Join(Environment.NewLine, result)}{Environment.NewLine}{Form2.ThreeTicks}{Environment.NewLine}";

                    await chatWebView.SetUserPrompt(output);
                }
            }
        }

        public void ClearBase64()
        {
            Base64Image = null;
            Base64ImageType = null;
        }
    }
}