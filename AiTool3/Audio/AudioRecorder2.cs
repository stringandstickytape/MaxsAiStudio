using NAudio.Wave;
using System.Diagnostics;
using System.IO;
using Whisper.net;
using Whisper.net.Ggml;

namespace AiTool3.Audio
{
    public class AudioRecorder2
    {
        private WaveInEvent waveIn;
        private MemoryStream memoryStream;
        private WaveFileWriter memoryWriter;
        private TaskCompletionSource<bool> tcs;
        private int fileCounter = 0;
        private string baseFilePath;
        private bool soundDetected = false;

        public DateTime? lastDateTimeAboveThreshold { get; set; }

        public async Task RecordAudioAsync(CancellationToken cancellationToken)
        {
            GetNewMemoryWriter();
            tcs = new TaskCompletionSource<bool>();

            waveIn.DataAvailable += OnDataAvailable;
            waveIn.RecordingStopped += OnRecordingStopped;

            cancellationToken.Register(() => StopRecording());

            waveIn.StartRecording();

            await tcs.Task;
        }

        private void GetNewMemoryWriter()
        {
            if (memoryStream != null)
                memoryStream.Dispose();
            if(memoryWriter != null)
                memoryWriter.Dispose();
            memoryStream = new MemoryStream();
            waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 1)
            };

            memoryWriter = new WaveFileWriter(memoryStream, waveIn.WaveFormat);
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            memoryWriter.Write(e.Buffer, 0, e.BytesRecorded);

            var levelCheck = 8000;
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
                soundDetected = true;
                // is it more than 3 seconds since?
            }

            if (lastDateTimeAboveThreshold.HasValue)
            {
                var timeSinceLastAbove50 = DateTime.Now - lastDateTimeAboveThreshold.Value;

                Debug.WriteLine($"t={timeSinceLastAbove50}");
                if (timeSinceLastAbove50.TotalMilliseconds > 3000 && soundDetected)
                {
                    lastDateTimeAboveThreshold = DateTime.Now;
                    soundDetected = false;
                    WriteAndContinue("output.wav");
                }
            }
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            memoryWriter.Flush();
            tcs.TrySetResult(true);
        }

        public void StopRecording(string outputFilePath = null)
        {
            waveIn.StopRecording();

            if (outputFilePath != null)
            {
                WriteToFile(outputFilePath);
            }

            Dispose();
        }

        private void WriteToFile(string outputFilePath)
        {
            memoryStream.Position = 0;
            using (var fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new WaveFileWriter(fileStream, waveIn.WaveFormat))
            {
                memoryStream.CopyTo(fileStream);
            }
        }



        private string GetNextFilePath()
        {
            string directory = Path.GetDirectoryName(baseFilePath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseFilePath);
            string extension = Path.GetExtension(baseFilePath);
            return Path.Combine(directory, $"{fileNameWithoutExtension}_{fileCounter}{extension}");
        }


        public async Task<int> GetAudioLevelAsync()
        {
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

        private void Dispose()
        {
            waveIn?.Dispose();
            memoryWriter?.Dispose();
            memoryStream?.Dispose();
        }

        public async void WriteAndContinue(string baseOutputFilePath)
        {
            // Get the current buffer as a byte array
            byte[] audioData = memoryStream.ToArray();

            // Transcribe the audio data
            string transcription = await TranscribeAudio(audioData);
            Debug.WriteLine($"!: {transcription}");
            // Clear the memory stream and create a new memory writer
            memoryStream.SetLength(0);
            memoryWriter.Dispose();
            GetNewMemoryWriter();

            // Increment file counter for next time (if still needed)
            fileCounter++;

            return;
        }

        private async Task<string> TranscribeAudio(byte[] audioData)
        {
            // requires specific DLLs...

            var ggmlType = GgmlType.Base;

            // write to wav file
            var wavFileName = "output.wav";
            using (var fileStream = new FileStream(wavFileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                await fileStream.WriteAsync(audioData, 0, audioData.Length);
            }

            var modelFileName = "ggml-base.bin";

            if (!File.Exists(modelFileName))
            {
                using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(ggmlType);
                using var fileWriter = File.OpenWrite(modelFileName);
                await modelStream.CopyToAsync(fileWriter);
            }

            var retVal = "";

            using var whisperFactory = WhisperFactory.FromPath(modelFileName);

            using var processor = whisperFactory.CreateBuilder()
                .WithLanguage("auto")
                .Build();

            using var memoryStream = new MemoryStream(audioData);

            await foreach (var result in processor.ProcessAsync(memoryStream))
            {
                retVal = $"-{retVal}{result.Text}";
            }
            return retVal;
        }
    }
}