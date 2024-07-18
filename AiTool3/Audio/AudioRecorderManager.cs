using System.Diagnostics;
using Whisper.net.Ggml;
using Whisper.net;
using Whisper.net.Wave;
using System.Text;
using NAudio.Wave;
using NAudio.MediaFoundation;

namespace AiTool3.Audio
{
    public class AudioRecorderManager
    {
        private AudioRecorder? recorder;
        private CancellationTokenSource? cts;

        public event EventHandler<string>? AudioProcessed;

        private Task? recordingTask;
        public bool IsRecording { get; private set; }

        private GgmlType ggmlType;

        private string modelName { get; set; }
            
        public AudioRecorderManager(GgmlType ggmlType)
        {
            this.ggmlType = ggmlType;
            modelName = $"ggml-smallen.bin";
        }


        public async Task StartRecording()
        {
            recorder = new AudioRecorder("ggml-smallen.bin");
            cts = new CancellationTokenSource();
            


            

            recorder.AudioProcessed += (sender, result) =>
            {
                AudioProcessed?.Invoke(this, result);
            };



            // Start recording in a separate task
            recordingTask = recorder.RecordAudioAsync(cts.Token);

            IsRecording = true;

            // check whether the level is silent
            var level = recorder.GetAudioLevel();



        }




        static async Task DownloadModel(string fileName, GgmlType ggmlType)
        {
            using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(ggmlType);
            using var fileWriter = File.OpenWrite(fileName);
            await modelStream.CopyToAsync(fileWriter);
        }
        public async Task StopRecording()
        {
            if (cts != null)
            {
                cts = MaxsAiStudio.ResetCancellationtoken(cts);

                // Wait for the recording task to complete
                await recordingTask!;

                IsRecording = false;

                // Clean up
                cts.Dispose();
                cts = null;
                recorder = null;

                return;

            }
            return;
        }

        private async Task<string> ProcessAudio()
        {
            // requires specific DLLs...

            if (!File.Exists(modelName))
            {
                using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(ggmlType);
                using var fileWriter = File.OpenWrite(modelName);
                await modelStream.CopyToAsync(fileWriter);
            }

            var retVal = "";


            using var whisperFactory = WhisperFactory.FromPath(modelName);

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
