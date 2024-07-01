using NAudio.Wave;
using System.Diagnostics;
using Whisper.net.Ggml;
using Whisper.net;
using System.IO;
using static Microsoft.CodeAnalysis.CSharp.SyntaxTokenParser;

namespace AiTool3.Audio
{
    public class AudioRecorder
    {
        private MemoryStream memoryStream;
        private WaveFileWriter writer;

        public bool soundDetected = false;
        public DateTime? lastDateTimeAboveThreshold { get; set; }

        // Define the event
        public event EventHandler<string> AudioProcessed;

        public async Task RecordAudioAsync(CancellationToken cancellationToken)

        {
            memoryStream = new MemoryStream();

            using (WaveInEvent waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 1)
            })
            using (writer = new WaveFileWriter(memoryStream, waveIn.WaveFormat))
            {
                var tcs = new TaskCompletionSource<bool>();

                waveIn.DataAvailable += (sender, e) =>
                {
                    writer.Write(e.Buffer, 0, e.BytesRecorded);
                    var levelCheck = 7000;

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
                    }
                    else
                    {
                        //memoryStream.Position = 0;
                        //memoryStream.SetLength(0);

                        if (!soundDetected)
                        {
                            GetNewMemoryWriter();
                        }
                    }

                    // if sound detectes and mroe than 3 seconds of silence, transcribe audio and begin new memory stream


                    if (soundDetected && DateTime.Now - lastDateTimeAboveThreshold.Value > TimeSpan.FromMilliseconds(1000))
                    {

                        Debug.WriteLine("((");
                        writer.Flush();
                        memoryStream.Position = 0;
                        var buffer = new byte[memoryStream.Length];
                        memoryStream.Read(buffer, 0, buffer.Length);

                        // Run processaudio in a new thread
                        Task.Run(async () =>
                        {
                            await ProcessAudio(buffer);
                        });

                        writer.Dispose();
                        GetNewMemoryWriter();
                        soundDetected = false;
                        lastDateTimeAboveThreshold = DateTime.Now;
                    }

                };

                waveIn.RecordingStopped += (sender, e) =>
                {
                    writer.Flush();
                    writer.Dispose();
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

        public static byte[] ApplyLowPassFilter(byte[] inputWav, int cutoffFrequency)
        {
            // Constants
            const int HEADER_SIZE = 44;
            const int SAMPLE_RATE = 16000;
            const int BYTES_PER_SAMPLE = 2;

            // Extract audio data (skip WAV header)
            short[] audioData = new short[(inputWav.Length - HEADER_SIZE) / BYTES_PER_SAMPLE];
            Buffer.BlockCopy(inputWav, HEADER_SIZE, audioData, 0, inputWav.Length - HEADER_SIZE);

            // Calculate filter coefficients
            double dt = 1.0 / SAMPLE_RATE;
            double rc = 1.0 / (2 * Math.PI * cutoffFrequency);
            double alpha = dt / (rc + dt);

            // Apply low-pass filter
            double[] filteredData = new double[audioData.Length];
            filteredData[0] = audioData[0];
            for (int i = 1; i < audioData.Length; i++)
            {
                filteredData[i] = filteredData[i - 1] + alpha * (audioData[i] - filteredData[i - 1]);
            }

            // Convert filtered data back to short array
            short[] filteredAudioData = filteredData.Select(x => (short)Math.Round(x)).ToArray();

            // Create output byte array
            byte[] outputWav = new byte[inputWav.Length];
            Buffer.BlockCopy(inputWav, 0, outputWav, 0, HEADER_SIZE);
            Buffer.BlockCopy(filteredAudioData, 0, outputWav, HEADER_SIZE, filteredAudioData.Length * BYTES_PER_SAMPLE);

            return outputWav;
        }

        private void GetNewMemoryWriter()
        {
            if (writer != null)
                writer.Dispose();
            if (memoryStream != null)
                memoryStream.Dispose();

            memoryStream = new MemoryStream();
            writer = new WaveFileWriter(memoryStream, new WaveFormat(16000,1));


        }

        internal async Task<int> GetAudioLevelAsync()
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
        private string modelName;
        public AudioRecorder(string modelNameIn)
        {
            modelName = modelNameIn;
            DownloadModel();
                
            WhisperFactory = WhisperFactory.FromPath(modelName);
            WhisperProcessor = WhisperFactory.CreateBuilder()
                    .WithLanguage("en")
                    .Build(); ;
        }

        private WhisperFactory WhisperFactory;
        private WhisperProcessor WhisperProcessor;

        private async Task ProcessAudio(byte[] audioData)
        {
            await DownloadModel();

            var filteredAudioData = ApplyLowPassFilter(audioData, 4000);

            var retVal = "";

            try
            {
                using var memoryStream = new MemoryStream(audioData);
                Debug.WriteLine(">>>");
                await foreach (var result in WhisperProcessor.ProcessAsync(memoryStream))
                {
                    retVal += $"{result.Text}";
                    Debug.WriteLine(result.Text);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            if (!string.IsNullOrWhiteSpace(retVal))
            {
                // Fire the event
                OnAudioProcessed(retVal);
            }

            return;
        }

        private async Task DownloadModel()
        {
            if (!File.Exists(modelName))
            {
                using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.MediumEn);
                using var fileWriter = File.OpenWrite(modelName);
                await modelStream.CopyToAsync(fileWriter);
            }
        }

        // Method to invoke the event
        protected virtual void OnAudioProcessed(string result)
        {
            AudioProcessed?.Invoke(this, result);
        }

    }
}