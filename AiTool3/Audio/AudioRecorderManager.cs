using System.Diagnostics;


namespace AiTool3.Audio
{
    public class AudioRecorderManager
    {
        private AudioRecorder recorder;
        private CancellationTokenSource cts;
        private Task recordingTask;
        public bool IsRecording { get; private set; }

        public async Task StartRecording()
        {
            recorder = new AudioRecorder();
            cts = new CancellationTokenSource();

            // Start recording in a separate task
            recordingTask = recorder.RecordAudioAsync("output.wav", cts.Token);

            IsRecording = true;
        }

        public async Task StopRecording()
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

                await ProcessAudio();
            }
        }

        private async Task ProcessAudio()
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "cmd.exe";
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = false;

            process.StartInfo = startInfo;
            process.Start();

            process.StandardInput.WriteLine("call C:\\ProgramData\\Miniconda3\\condabin\\activate.bat");
            process.StandardInput.WriteLine("conda activate whisperx");
            process.StandardInput.WriteLine("whisperx output.wav");
            process.StandardInput.WriteLine("exit");

            // wait for completion
            await process.WaitForExitAsync();
        }

        public string GetTranscription()
        {
            // get the output from output.txt
            return File.ReadAllText("output.txt");
        }
    }

}
