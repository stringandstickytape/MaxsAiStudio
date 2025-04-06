// AiStudio4.Services/AudioTranscriptionService.cs
using AiStudio4.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;

namespace AiStudio4.Services
{
    public class AudioTranscriptionService : IAudioTranscriptionService
    {
        private readonly ILogger<AudioTranscriptionService> _logger;
        private readonly string _modelDirectory;
        private readonly string _modelFileName = "ggml-base.en.bin"; // Or choose another model like small.en, etc.
        private readonly GgmlType _ggmlType = GgmlType.BaseEn;
        private WhisperFactory? _whisperFactory;

        public AudioTranscriptionService(ILogger<AudioTranscriptionService> logger)
        {
            _logger = logger;
            // Store models in a subdirectory relative to the application executable
            string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
            _modelDirectory = Path.Combine(assemblyLocation, "WhisperModels");
            Directory.CreateDirectory(_modelDirectory); // Ensure the directory exists
        }

        private async Task InitializeFactoryAsync(CancellationToken cancellationToken)
        {
            if (_whisperFactory != null)
            {
                return;
            }

            string modelPath = Path.Combine(_modelDirectory, _modelFileName);
            _logger.LogInformation("Whisper model path: {ModelPath}", modelPath);

            if (!File.Exists(modelPath))
            {
                _logger.LogInformation("Downloading Whisper model: {ModelFileName}...", _modelFileName);
                try

                {
                    using (var httpClient = new HttpClient())
                    {
                        var downloader = new WhisperGgmlDownloader(httpClient);
                        using var modelStream = await downloader.GetGgmlModelAsync(_ggmlType, cancellationToken: cancellationToken);
                        using var fileWriter = File.OpenWrite(modelPath);

                        await modelStream.CopyToAsync(fileWriter, cancellationToken);
                        _logger.LogInformation("Model download complete.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to download Whisper model.");
                    throw;
                }
            }
            else
            {
                _logger.LogInformation("Whisper model already exists.");
            }

            try
            {
                _whisperFactory = WhisperFactory.FromPath(modelPath);
                _logger.LogInformation("WhisperFactory initialized successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize WhisperFactory from path {ModelPath}.", modelPath);
                throw;
            }
        }

        public async Task<string> TranscribeAudioAsync(Stream audioStream, CancellationToken cancellationToken = default)
        {
            try
            {
                await InitializeFactoryAsync(cancellationToken); // Ensure factory is ready

                if (_whisperFactory == null)
                {
                    throw new InvalidOperationException("WhisperFactory could not be initialized.");
                }

                // Note: Whisper.net ideally wants WAV format, 16kHz, mono.
                // Consider adding conversion logic if supporting other formats like MP3.
                _logger.LogInformation("Starting audio transcription...");

                using var processor = _whisperFactory.CreateBuilder()
                    .WithLanguage("en") // Or use WithLanguageDetection()
                    .Build();

                var transcriptionResult = new StringBuilder();
                await foreach (var result in processor.ProcessAsync(audioStream, cancellationToken))
                {
                    _logger.LogDebug("Segment: {Start} -> {End}: {Text}", result.Start, result.End, result.Text);
                    transcriptionResult.Append(result.Text).Append(" "); // Add space between segments
                }

                string fullText = transcriptionResult.ToString().Trim();
                _logger.LogInformation("Transcription completed successfully. Length: {Length}", fullText.Length);
                return fullText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audio transcription failed.");
                throw; // Re-throw to allow calling code to handle
            }
        }
    }
}