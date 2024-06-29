using System.Diagnostics;
using Whisper.net.Ggml;
using Whisper.net;
using Whisper.net.Wave;

namespace AiTool3.Audio
{
    public class AudioRecorderManager
    {
        private AudioRecorder recorder;
        private CancellationTokenSource cts;
        private Task recordingTask;
        public bool IsRecording { get; private set; }

        public const string ModelName = "ggml-smallen.bin";

        public async Task StartRecording()
        {
            recorder = new AudioRecorder();
            cts = new CancellationTokenSource();
            var ggmlType = GgmlType.TinyEn;
            var modelFileName = "ggml-tinyen.bin";
            var wavFileName = "output.wav";

            if (!File.Exists(modelFileName))
            {
                await DownloadModel(modelFileName, ggmlType);
            }

            // Start recording in a separate task
            recordingTask = recorder.RecordAudioAsync(wavFileName, cts.Token);

            IsRecording = true;

            // check whether the level is silent
            var level = await recorder.GetAudioLevelAsync();



        }
        static async Task DownloadModel(string fileName, GgmlType ggmlType)
        {
            Console.WriteLine($"Downloading Model {fileName}");
            using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(ggmlType);
            using var fileWriter = File.OpenWrite(fileName);
            await modelStream.CopyToAsync(fileWriter);
        }
        public async Task<string> StopRecordingAndReturnTranscription()
        {
            if (cts != null)
            {
                // Stop the recording
                cts.Cancel();

                // Wait for the recording task to complete
                await recordingTask;

                IsRecording = false;

                // Clean up
                cts.Dispose();
                cts = null;
                recorder = null;

                return await ProcessAudio();

            }
            return "error";
        }

        private async Task<string> ProcessAudio()
        {
            // requires specific DLLs...

            if (!File.Exists(ModelName))
            {
                using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.SmallEn);
                using var fileWriter = File.OpenWrite(ModelName);
                await modelStream.CopyToAsync(fileWriter);
            }

            var retVal = "";


            using var whisperFactory = WhisperFactory.FromPath(ModelName);

            using var processor = whisperFactory.CreateBuilder()
                .WithLanguage("auto")
                .Build();

            using var fileStream = File.OpenRead("output.wav");

            await foreach (var result in processor.ProcessAsync(fileStream))
            {
                // write to output.txt
                retVal = $"{retVal}{result.Text}";
            }
            return retVal;
        }


    }
}
