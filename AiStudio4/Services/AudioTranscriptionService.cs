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
        private readonly IWebSocketNotificationService _webSocketNotificationService;
        private readonly string _modelDirectory;
        private readonly string _modelFileName = "ggml-base.en.bin"; // Or choose another model like small.en, etc.
        private readonly GgmlType _ggmlType = GgmlType.BaseEn;
        private WhisperFactory? _whisperFactory;

        public AudioTranscriptionService(
            ILogger<AudioTranscriptionService> logger,
            IWebSocketNotificationService webSocketNotificationService)
        {
            _logger = logger;
            _webSocketNotificationService = webSocketNotificationService;
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

        public async Task<string> TranscribeAudioAsync(Stream audioStream, string clientId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audio transcription failed.");
                throw; // Re-throw to allow calling code to handle
            }
        }
    }
}