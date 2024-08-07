using Whisper.net.Ggml;
using Whisper.net;
using AiTool3.UI;

namespace AiTool3.Audio
{
    public class AudioRecorderManager
    {
        private AudioRecorder? recorder;
        private CancellationTokenSource? cts;
        private ChatWebView chatWebView;

        public event EventHandler<string>? AudioProcessed;

        private Task? recordingTask;
        public bool IsRecording { get; private set; }

        private GgmlType ggmlType;

        private string modelName { get; set; }
            
        public AudioRecorderManager(GgmlType ggmlType, ChatWebView chatWebView)
        {
            this.ggmlType = ggmlType;
            modelName = $"ggml-smallen.bin";
            this.chatWebView = chatWebView;
        }


        public async Task StartRecording()
        {
            recorder = new AudioRecorder("ggml-smallen.bin");
            
            cts = new CancellationTokenSource();
            
            recorder.AudioProcessed += (sender, result) =>
            {
                AudioProcessed?.Invoke(this, result);
            };

            chatWebView.SetIndicator("Voice", "#FF0000");

            // Start recording in a separate task
            recordingTask = recorder.RecordAudioAsync(cts.Token);

            IsRecording = true;

            // check whether the level is silent
            var level = recorder.GetAudioLevel();

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
            }

            chatWebView.ClearIndicator("Voice");

            recorder?.Dispose();
            recorder = null;

            return;
        }
    }
}
