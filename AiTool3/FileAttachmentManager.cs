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

        public async Task HandleAttachment(ChatWebView chatWebView, MaxsAiStudio maxsAiStudio, bool softwareToyMode)
        {
            var result = SimpleDialogsHelper.ShowAttachmentDialog();

            switch (result)
            {
                case DialogResult.Retry:
                    await AttachAndTranscribeMP4(chatWebView, maxsAiStudio, softwareToyMode);
                    break;
                case DialogResult.Yes:
                    DialogAndAttachImage();
                    break;
                case DialogResult.No:
                    await AttachTextFiles();
                    break;
                case DialogResult.Continue:

                    // get image from clipboard

                    await AttachClipboardImage();
                    break;
            }
        }

        private async Task AttachAndTranscribeMP4(ChatWebView chatWebView, MaxsAiStudio maxsAiStudio, bool softwareToyMode)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "MP4 files (*.mp4)|*.mp4|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                maxsAiStudio.ShowWorking("Attaching file", softwareToyMode);
                await Task.Run(async () =>
                {
                    var output = await TranscribeMP4(openFileDialog.FileName);
                    chatWebView.SetUserPrompt(output);
                });
                maxsAiStudio.HideWorking();
            }
        }

        private void DialogAndAttachImage()
        {
            OpenFileDialog openFileDialog = ImageHelpers.ShowAttachImageFileDialog(_settings.DefaultPath);

            if (openFileDialog.FileName != "")
            {
                AttachImage(openFileDialog.FileName);
            }
        }

        public async Task AttachImage(string filename)
        {
            Base64Image = ImageHelpers.ImageToBase64(filename);
            Base64ImageType = ImageHelpers.GetImageType(filename);
            _settings.SetDefaultPath(Path.GetDirectoryName(filename)!);
        }

        public async Task AttachClipboardImage()
        {
            if (Clipboard.ContainsImage())
            {
                using (var image = Clipboard.GetImage())
                {
                    if (image != null)
                    {
                        using (var ms = new MemoryStream())
                        {
                            try
                            {
                                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                Base64Image = Convert.ToBase64String(ms.ToArray());
                                Base64ImageType = ImageHelpers.GetImageType(".png");
                            }
                            catch (Exception ex)
                            {
                                // Log the exception or handle it appropriately
                                Console.WriteLine($"Error saving image: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Failed to get image from clipboard.");
                    }
                }
            }
        }

        private async Task AttachTextFiles()
        {
            OpenFileDialog attachTextFilesDialog = ImageHelpers.ShowAttachTextFilesDialog(_settings.DefaultPath);

            if (attachTextFilesDialog.FileNames.Length > 0)
            {
                var filenames = attachTextFilesDialog.FileNames;
                await AttachTextFiles(filenames);

                _settings.SetDefaultPath(Path.GetDirectoryName(attachTextFilesDialog.FileName)!);
            }
        }

        public  async Task AttachTextFiles(string[] filenames)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var file in filenames)
            {
                sb.AppendMany(MaxsAiStudio.ThreeTicks,
                    file,
                    Environment.NewLine,
                    File.ReadAllText(file),
                    Environment.NewLine,
                    MaxsAiStudio.ThreeTicks,
                    Environment.NewLine,
                    Environment.NewLine);
            }

            var existingPrompt = await _chatWebView.GetUserPrompt();
            await _chatWebView.SetUserPrompt($"{sb}{existingPrompt}");
        }

        public async Task<string> TranscribeMP4(string filename)
        {
            // Path to the Miniconda installation
            string condaPath = @"C:\Users\maxhe\miniconda3\Scripts\activate.bat";
            
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
                    string output = NewMethod(filename, result);

                    return output;

                }
                else
                {
                    throw new Exception("Couldn't transcribe video/audio file");
                }
            }
        }

        private static string NewMethod(string filename, List<string> result)
        {
            return $"{MaxsAiStudio.ThreeTicks}{filename.Split('\\').Last()}{Environment.NewLine}{string.Join(Environment.NewLine, result)}{Environment.NewLine}{MaxsAiStudio.ThreeTicks}{Environment.NewLine}";
        }

        public void ClearBase64()
        {
            Base64Image = null;
            Base64ImageType = null;
        }
    }
}