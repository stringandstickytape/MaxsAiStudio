using AiTool3.ExtensionMethods;
using AiTool3.Helpers;
using AiTool3.UI;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;

namespace AiTool3.FileAttachments
{
    public class FileAttachmentManager
    {
        private ChatWebView _chatWebView;

        public string? Base64Image { get; private set; }
        public string? Base64ImageType { get; private set; }

        public void InjectDependencies(ChatWebView chatWebView)
        {
            _chatWebView = chatWebView;
        }

        public async Task HandleAttachment(ChatWebView chatWebView, MaxsAiStudio maxsAiStudio, SettingsSet settings)
        {
            var result = SimpleDialogsHelper.ShowAttachmentDialog();

            switch (result)
            {
                case DialogResult.Retry:
                    await AttachAndTranscribeMP4(chatWebView, maxsAiStudio, settings.SoftwareToyMode, settings.HuggingFaceToken);
                    break;
                case DialogResult.Yes:
                    DialogAndAttachImage(settings);
                    break;
                case DialogResult.No:
                    await AttachTextFiles(settings);
                    break;
                case DialogResult.Continue:

                    // get image from clipboard

                    await AttachClipboardImage();
                    break;
            }
        }

        private async Task AttachAndTranscribeMP4(ChatWebView chatWebView, MaxsAiStudio maxsAiStudio, bool softwareToyMode, string hfToken)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "All supported files|*.mp4;*.m4a;*.mov;*.avi;*.mkv;*.flv;*.webm;*.wmv;*.asf;*.3gp;*.3g2;*.mpg;*.mpeg;*.ts;*.mts;*.m2ts;*.vob;*.ogv;*.mxf;*.wav;*.aac;*.mp3;*.ogg;*.flac;*.wma;*.aiff;*.alac|MP4 files (*.mp4)|*.mp4|M4A files (*.m4a)|*.m4a|MOV files (*.mov)|*.mov|AVI files (*.avi)|*.avi|MKV files (*.mkv)|*.mkv|FLV files (*.flv)|*.flv|WebM files (*.webm)|*.webm|WMV files (*.wmv)|*.wmv|ASF files (*.asf)|*.asf|3GP files (*.3gp)|*.3gp|3G2 files (*.3g2)|*.3g2|MPEG files (*.mpg;*.mpeg)|*.mpg;*.mpeg|TS files (*.ts;*.mts;*.m2ts)|*.ts;*.mts;*.m2ts|VOB files (*.vob)|*.vob|OGV files (*.ogv)|*.ogv|MXF files (*.mxf)|*.mxf|WAV files (*.wav)|*.wav|AAC files (*.aac)|*.aac|MP3 files (*.mp3)|*.mp3|OGG files (*.ogg)|*.ogg|FLAC files (*.flac)|*.flac|WMA files (*.wma)|*.wma|AIFF files (*.aiff)|*.aiff|ALAC files (*.alac)|*.alac|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                maxsAiStudio.ShowWorking("Attaching file", softwareToyMode);
                await Task.Run(async () =>
                {
                    var output = await TranscribeMP4(openFileDialog.FileName, maxsAiStudio.CurrentSettings.PathToCondaActivateScript, hfToken);
                    chatWebView.SetUserPrompt(output);
                });
                maxsAiStudio.HideWorking();
            }
        }

        private void DialogAndAttachImage(SettingsSet settings)
        {
            OpenFileDialog openFileDialog = ImageHelpers.ShowAttachImageFileDialog(settings.DefaultPath);

            if (openFileDialog.FileName != "")
            {
                AttachImage(openFileDialog.FileName, settings);
            }
        }

        public async Task AttachImage(string filename, SettingsSet settings)
        {
            Base64Image = ImageHelpers.ImageToBase64(filename);
            Base64ImageType = ImageHelpers.GetImageType(filename);
            settings.SetDefaultPath(Path.GetDirectoryName(filename)!);
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

        private async Task AttachTextFiles(SettingsSet settings)
        {
            OpenFileDialog attachTextFilesDialog = ImageHelpers.ShowAttachTextFilesDialog(settings.DefaultPath);

            if (attachTextFilesDialog.FileNames.Length > 0)
            {
                var filenames = attachTextFilesDialog.FileNames;
                await AttachTextFiles(filenames);

                settings.SetDefaultPath(Path.GetDirectoryName(attachTextFilesDialog.FileName)!);
            }
        }

        public async Task AttachTextFiles(string[] filenames)
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

        public async Task<string> TranscribeMP4(string filename, string condaActivateScriptPath, string hfToken)
        {
            // Path to the Miniconda installation
            string condaPath = Path.Combine(condaActivateScriptPath, "activate.bat");

            if (!File.Exists(condaPath))
            {
                MessageBox.Show($"Conda activate script not found at {condaPath}{Environment.NewLine}You can set the path in Edit -> Settings.");
                return "";
            }

            // Command to activate the WhisperX environment and run Whisper
            string arguments = $"/C {condaPath} && conda activate whisperx && whisperx \"{filename}\"  --language en --model  large-v3 --output_dir \"{Path.GetDirectoryName(filename)}\" ";

            if(!string.IsNullOrEmpty(hfToken))
            {
                arguments += $"--hf_token {hfToken} --diarize ";
            }
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
                    var filenameOnly = filename.Split('\\').Last();

                    if (filenameOnly.Contains("."))
                    {
                        filenameOnly = filenameOnly.Substring(0, filenameOnly.LastIndexOf('.')) + ".vtt";
                    }
                    else
                    {
                        filenameOnly += ".json";
                    }
                    var fullFilename = Path.Combine(Path.GetDirectoryName(filename)!, filenameOnly);

                    var json = File.ReadAllText(fullFilename);

                    //List<string> result = new List<string>();
                    //
                    //dynamic jsonObj = JObject.Parse(json);
                    //
                    //foreach (var segment in jsonObj.segments)
                    //{
                    //    double start = segment.start;
                    //    double end = segment.end;
                    //    string text = segment.text;
                    //    string formattedText = $"[{start:F3} - {end:F3}] {text.Trim()}";
                    //    result.Add(formattedText);
                    //}
                    //string output = NewMethod(filename, result);

                    return json;

                }
                else
                {
                    MessageBox.Show("Unable to transcribe file.");
                    return "";
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

        public void SetBase64(string image, string type)
        {
            Base64Image = image;
            Base64ImageType = type;
        }

        internal async Task FileDropped(string filename, SettingsSet settings)
        {
            if (filename.StartsWith("http"))
            {
                var textFromUrl = await HtmlTextExtractor.ExtractTextFromUrlAsync(filename);

                var quotedFile = HtmlTextExtractor.QuoteFile(filename, textFromUrl);

                var currentPrompt = await _chatWebView.GetUserPrompt();
                await _chatWebView.SetUserPrompt($"{quotedFile}{Environment.NewLine}{currentPrompt}");

                return;
            }


            var uri = new Uri(filename);
            filename = uri.LocalPath;

            try
            {

                var classification = FileTypeClassifier.GetFileClassification(Path.GetExtension(filename));

                switch (classification)
                {
                    case FileTypeClassifier.FileClassification.Video:
                    case FileTypeClassifier.FileClassification.Audio:
                        var output = await TranscribeMP4(filename, settings.PathToCondaActivateScript, settings.HuggingFaceToken);
                        _chatWebView.SetUserPrompt(output);
                        break;
                    case FileTypeClassifier.FileClassification.Image:
                        await AttachImage(filename, settings);
                        break;
                    default:
                        await AttachTextFiles(new string[] { filename });
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}