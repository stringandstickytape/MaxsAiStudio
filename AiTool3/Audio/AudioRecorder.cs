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

                    var levelCheck = 10000;
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
                        var t = Task.Run(async () =>
                        {
                            var x = await ProcessAudio(buffer);
                            if (!string.IsNullOrWhiteSpace(x))
                            {
                                Debug.WriteLine(x);
                                // Fire the event
                                OnAudioProcessed(x);
                            }

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
                    writer.Flush();
                    // copy memory stream to byte array
                    memoryStream.Position = 0;
                    var buffer = new byte[memoryStream.Length];
                    memoryStream.Read(buffer, 0, buffer.Length);

                    var output = ProcessAudio(buffer);

                    waveIn.StopRecording();
                });

                waveIn.StartRecording();

                await tcs.Task;
            }
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

        private async Task<string> ProcessAudio(byte[] audioData)
        {
            var modelName = "ggml-meden.bin";

            if (!File.Exists(modelName))
            {
                using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.MediumEn);
                using var fileWriter = File.OpenWrite(modelName);
                await modelStream.CopyToAsync(fileWriter);
            }

            var retVal = "";

            try
            {
                using var whisperFactory = WhisperFactory.FromPath(modelName);

                using var processor = whisperFactory.CreateBuilder()
                    .WithLanguage("auto")
                    .Build();

                using var memoryStream = new MemoryStream(audioData);
                Debug.WriteLine(">>>");
                await foreach (var result in processor.ProcessAsync(memoryStream))
                {
                    retVal += $"{result.Text}";
                    Debug.WriteLine(result.Text);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return retVal;
        }

        // Method to invoke the event
        protected virtual void OnAudioProcessed(string result)
        {
            AudioProcessed?.Invoke(this, result);
        }

    }
}