using NAudio.Wave;

namespace AiTool3.Audio
{
    public class AudioRecorder
    {
        private WaveInEvent waveIn;
        private WaveFileWriter writer;

        public async Task RecordAudioAsync(string outputFilePath, CancellationToken cancellationToken)
        {
            using (waveIn = new WaveInEvent())
            using (writer = new WaveFileWriter(outputFilePath, waveIn.WaveFormat))
            {
                var tcs = new TaskCompletionSource<bool>();

                waveIn.DataAvailable += (sender, e) =>
                {
                    writer.Write(e.Buffer, 0, e.BytesRecorded);
                };

                waveIn.RecordingStopped += (sender, e) =>
                {
                    writer.Flush();
                    tcs.TrySetResult(true);
                };

                cancellationToken.Register(() =>
                {
                    waveIn.StopRecording();
                });

                waveIn.StartRecording();

                await tcs.Task;
            }
        }
    }

}

