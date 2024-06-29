using NAudio.Wave;
using System.Diagnostics;

namespace AiTool3.Audio
{
    public class AudioRecorder
    {
        private WaveFileWriter writer;

        public DateTime? lastDateTimeAboveThreshold { get; set; }

        public async Task RecordAudioAsync(string outputFilePath, CancellationToken cancellationToken)
        {
            // record at 16khz
;
            using (WaveInEvent waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 1)
            })
            using (writer = new WaveFileWriter(new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite), waveIn.WaveFormat))
            {
                var tcs = new TaskCompletionSource<bool>();

                waveIn.DataAvailable += (sender, e) =>
                {
                    writer.Write(e.Buffer, 0, e.BytesRecorded);

                    // check whether ever a higher level than 50
                    var levelCheck = 5000;
                    var max = 0;
                    for (int i = 0; i < e.BytesRecorded; i += 2)
                    {
                        var sample = BitConverter.ToInt16(e.Buffer, i);
                        if (sample > max)
                        {
                            max = sample;
                        }
                    }
                    if (max > levelCheck)
                    {
                        lastDateTimeAboveThreshold = DateTime.Now;
                        Debug.WriteLine($"Level is higher than {levelCheck}");
                    }
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

        internal async Task<int> GetAudioLevelAsync()
        {
           // work out how many milliseconds since last time level was above 50
            if (lastDateTimeAboveThreshold.HasValue)
            {
                var timeSinceLastAbove50 = DateTime.Now - lastDateTimeAboveThreshold.Value;
                return (int)timeSinceLastAbove50.TotalMilliseconds;
            }
            else
            {
                return -1;
            }
        }
    }

}

