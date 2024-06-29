using System.Diagnostics;
using Whisper.net.Ggml;
using Whisper.net;
using Whisper.net.Wave;

namespace AiTool3.Audio
{
    public class AudioRecorderManager2
    {
        private AudioRecorder2 recorder;
        private CancellationTokenSource cts;
        private Task recordingTask;
        public bool IsRecording { get; private set; }

        public const string ModelName = "ggml-smallen.bin";

        public async Task StartRecording()
        {
            recorder = new AudioRecorder2();
            cts = new CancellationTokenSource();


            // Start recording in a separate task
            recordingTask = recorder.RecordAudioAsync(cts.Token);

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

                return "";// await ProcessAudio();

            }
            return "error";
        }




    }
}
