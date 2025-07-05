// C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/Services/LlamaServerService.cs
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using AiStudio4.Services.Interfaces;

namespace AiStudio4.Services
{

    public class LlamaServerService : ILlamaServerService
    {
        private readonly ILogger<LlamaServerService> _logger;
        private readonly HttpClient _httpClient;
        private Process _llamaServerProcess;
        private string _currentModelPath;
        private LlamaServerSettings _currentSettings;
        private readonly string _binaryDirectory;
        private readonly string _llamaServerPath;
        private readonly int _serverPort;
        private bool _serverReady = false;
        private readonly SemaphoreSlim _serverLock = new(1, 1);

        public bool IsServerRunning => _llamaServerProcess?.HasExited == false && _serverReady;
        public string ServerBaseUrl => $"http://127.0.0.1:{_serverPort}";

        public LlamaServerService(ILogger<LlamaServerService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            _serverPort = GetAvailablePort();
            _binaryDirectory = PathHelper.GetProfileSubPath("llama-cpp");
            _llamaServerPath = Path.Combine(_binaryDirectory, "llama-server.exe");
            
            _logger.LogInformation("LlamaServerService initialized with port {Port}", _serverPort);
        }

        public async Task<string> EnsureServerRunningAsync(string modelPath, LlamaServerSettings settings = null)
        {
            if (string.IsNullOrEmpty(modelPath) || !File.Exists(modelPath))
            {
                throw new ArgumentException($"Model path not found: {modelPath}");
            }

            settings ??= new LlamaServerSettings();

            await _serverLock.WaitAsync();
            try
            {
                // Check if we need to restart the server (different model or settings)
                bool needsRestart = !IsServerRunning || 
                                  _currentModelPath != modelPath || 
                                  !AreSettingsEqual(_currentSettings, settings);

                if (needsRestart)
                {
                    _logger.LogInformation("Starting llama-server for model: {ModelPath}", modelPath);
                    
                    // Ensure binary is available
                    await EnsureLlamaServerBinaryAsync();
                    
                    // Stop existing server if running
                    if (IsServerRunning)
                    {
                        _logger.LogInformation("Stopping existing llama-server to switch models/settings");
                        await StopServerInternalAsync();
                    }

                    // Start new server
                    await StartServerAsync(modelPath, settings);
                    
                    _currentModelPath = modelPath;
                    _currentSettings = settings;
                }
                else if (!await IsServerHealthyAsync())
                {
                    _logger.LogWarning("Server appears unhealthy, restarting...");
                    await StopServerInternalAsync();
                    await StartServerAsync(modelPath, settings);
                }

                return ServerBaseUrl;
            }
            finally
            {
                _serverLock.Release();
            }
        }

        public async Task<bool> IsServerHealthyAsync()
        {
            if (!IsServerRunning) return false;

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _httpClient.GetAsync($"{ServerBaseUrl}/health", cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Health check failed: {Error}", ex.Message);
                return false;
            }
        }

        public async Task StopServerAsync()
        {
            await _serverLock.WaitAsync();
            try
            {
                await StopServerInternalAsync();
            }
            finally
            {
                _serverLock.Release();
            }
        }

        private async Task StopServerInternalAsync()
        {
            if (_llamaServerProcess?.HasExited == false)
            {
                try
                {
                    _logger.LogInformation("Stopping llama-server process");
                    _llamaServerProcess.Kill();
                    
                    // Wait for graceful shutdown
                    if (!_llamaServerProcess.WaitForExit(5000))
                    {
                        _logger.LogWarning("llama-server did not exit gracefully, force killing");
                        _llamaServerProcess.Kill();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping llama-server");
                }
            }

            _llamaServerProcess?.Dispose();
            _llamaServerProcess = null;
            _serverReady = false;
            _currentModelPath = null;
            _currentSettings = null;
        }

        private async Task StartServerAsync(string modelPath, LlamaServerSettings settings)
        {
            var args = BuildServerArguments(modelPath, settings);
            
            _logger.LogInformation("Starting llama-server with args: {Args}", args);

            _llamaServerProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _llamaServerPath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = _binaryDirectory
                }
            };

            // Log server output
            _llamaServerProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    _logger.LogDebug("LlamaServer: {Output}", e.Data);
            };

            _llamaServerProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    _logger.LogWarning("LlamaServer Error: {Error}", e.Data);
            };

            _llamaServerProcess.Start();
            _llamaServerProcess.BeginOutputReadLine();
            _llamaServerProcess.BeginErrorReadLine();

            // Wait for server to be ready
            await WaitForServerReadyAsync();
            _serverReady = true;

            _logger.LogInformation("llama-server started successfully on {BaseUrl}", ServerBaseUrl);
        }

        private async Task WaitForServerReadyAsync()
        {
            var timeout = TimeSpan.FromSeconds(60);
            var start = DateTime.Now;
            var delay = TimeSpan.FromSeconds(1);

            while (DateTime.Now - start < timeout)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    var response = await _httpClient.GetAsync($"{ServerBaseUrl}/health", cts.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        return;
                    }
                }
                catch
                {
                    // Server not ready yet
                }

                await Task.Delay(delay);
            }

            throw new TimeoutException($"llama-server failed to start within {timeout.TotalSeconds} seconds");
        }

        private string BuildServerArguments(string modelPath, LlamaServerSettings settings)
        {
            var args = new List<string>
            {
                $"--model \"{modelPath}\"",
                "--jinja",  // Enable tool calling support
                $"--host 127.0.0.1",
                $"--port {_serverPort}",
                $"--ctx-size {settings.ContextSize}",
                $"--batch-size {settings.BatchSize}",
                $"--threads {settings.Threads}"
            };

            // Auto-detect GPU layers if not specified
            if (settings.GpuLayerCount == -1)
            {
                args.Add("--n-gpu-layers 999"); // Let llama.cpp auto-detect
            }
            else if (settings.GpuLayerCount > 0)
            {
                args.Add($"--n-gpu-layers {settings.GpuLayerCount}");
            }

            if (settings.FlashAttention)
            {
                args.Add("-fa");
            }

            if (!string.IsNullOrEmpty(settings.AdditionalArgs))
            {
                args.Add(settings.AdditionalArgs);
            }

            return string.Join(" ", args);
        }

        private async Task EnsureLlamaServerBinaryAsync()
        {
            if (File.Exists(_llamaServerPath))
            {
                _logger.LogDebug("llama-server binary already exists at {Path}", _llamaServerPath);
                return;
            }

            _logger.LogInformation("Downloading llama-server binary...");
            await DownloadLatestLlamaServerAsync();
        }

        private async Task DownloadLatestLlamaServerAsync()
        {
            try
            {
                Directory.CreateDirectory(_binaryDirectory);

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-LlamaCpp");

                // Get latest release info
                var releaseUrl = "https://api.github.com/repos/ggerganov/llama.cpp/releases/latest";
                var releaseJson = await httpClient.GetStringAsync(releaseUrl);
                var release = JsonConvert.DeserializeObject<GitHubRelease>(releaseJson);

                // Find both main binary and CUDA runtime assets
                var mainAsset = FindMainBinaryAsset(release.Assets);
                var cudaRuntimeAsset = FindCudaRuntimeAsset(release.Assets);

                if (mainAsset == null)
                {
                    throw new Exception("No compatible llama.cpp main binary found in latest release");
                }

                _logger.LogInformation("Downloading main binary {Name} ({Size} MB)", mainAsset.Name, mainAsset.Size / 1024 / 1024);

                // Download and extract main binary
                await DownloadAndExtractAssetAsync(httpClient, mainAsset, "main binary");

                // Download and extract CUDA runtime if available
                if (cudaRuntimeAsset != null)
                {
                    _logger.LogInformation("Downloading CUDA runtime {Name} ({Size} MB)", cudaRuntimeAsset.Name, cudaRuntimeAsset.Size / 1024 / 1024);
                    await DownloadAndExtractAssetAsync(httpClient, cudaRuntimeAsset, "CUDA runtime");
                }
                else
                {
                    _logger.LogInformation("No CUDA runtime package found, using CPU-only version");
                }

                // Verify llama-server.exe exists
                if (!File.Exists(_llamaServerPath))
                {
                    throw new Exception("llama-server.exe not found after extraction");
                }

                _logger.LogInformation("Successfully set up llama-server at {Path}", _llamaServerPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download llama-server");
                throw;
            }
        }

        private async Task DownloadAndExtractAssetAsync(HttpClient httpClient, ReleaseAsset asset, string description)
        {
            var zipPath = Path.Combine(_binaryDirectory, asset.Name);
            
            try
            {
                // Download the archive
                using (var response = await httpClient.GetAsync(asset.BrowserDownloadUrl))
                {
                    response.EnsureSuccessStatusCode();
                    using var fileStream = File.Create(zipPath);
                    await response.Content.CopyToAsync(fileStream);
                }

                // Extract all files to the binary directory
                using (var archive = ZipFile.OpenRead(zipPath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (!string.IsNullOrEmpty(entry.Name)) // Skip directories
                        {
                            var extractPath = Path.Combine(_binaryDirectory, entry.Name);
                            
                            // Create directory if needed
                            var directory = Path.GetDirectoryName(extractPath);
                            if (!Directory.Exists(directory))
                            {
                                Directory.CreateDirectory(directory);
                            }
                            
                            entry.ExtractToFile(extractPath, true);
                            _logger.LogDebug("Extracted {Name} from {Description}", entry.Name, description);
                        }
                    }
                }

                _logger.LogInformation("Successfully extracted {Description}", description);
            }
            finally
            {
                // Clean up zip file
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }
            }
        }

        private ReleaseAsset FindMainBinaryAsset(ReleaseAsset[] assets)
        {
            // Look for main binary packages in priority order
            var candidates = new[]
            {
                "-bin-win-cuda-12", // Current naming pattern: llama-b5711-bin-win-cuda-12.4-x64.zip
                "win-cuda-cu12", // Legacy pattern
                "win-cuda-cu11", 
                "win-avx2-x64",
                "win-x64"
            };

            foreach (var candidate in candidates)
            {
                var asset = assets.FirstOrDefault(a => 
                    a.Name.Contains(candidate, StringComparison.OrdinalIgnoreCase) && 
                    a.Name.Contains("bin", StringComparison.OrdinalIgnoreCase) &&
                    !a.Name.Contains("cudart", StringComparison.OrdinalIgnoreCase) && // Exclude CUDA runtime
                    a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
                if (asset != null)
                {
                    _logger.LogDebug("Selected main binary: {Name}", asset.Name);
                    return asset;
                }
            }

            return null;
        }

        private ReleaseAsset FindCudaRuntimeAsset(ReleaseAsset[] assets)
        {
            // Look for CUDA runtime package
            var asset = assets.FirstOrDefault(a => 
                a.Name.Contains("cudart", StringComparison.OrdinalIgnoreCase) && 
                a.Name.Contains("win", StringComparison.OrdinalIgnoreCase) &&
                a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
            
            if (asset != null)
            {
                _logger.LogDebug("Found CUDA runtime: {Name}", asset.Name);
            }
            
            return asset;
        }

        private static bool AreSettingsEqual(LlamaServerSettings a, LlamaServerSettings b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            return a.ContextSize == b.ContextSize &&
                   a.GpuLayerCount == b.GpuLayerCount &&
                   a.Threads == b.Threads &&
                   a.BatchSize == b.BatchSize &&
                   a.FlashAttention == b.FlashAttention &&
                   a.AdditionalArgs == b.AdditionalArgs;
        }

        private static int GetAvailablePort()
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public void Dispose()
        {
            _serverLock?.Wait(5000);
            try
            {
                StopServerInternalAsync().Wait(5000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disposal");
            }
            finally
            {
                _serverLock?.Release();
                _serverLock?.Dispose();
            }
        }
    }

    public class LlamaServerSettings
    {
        public int ContextSize { get; set; } = 32768;
        public int GpuLayerCount { get; set; } = -1; // Auto-detect
        public int Threads { get; set; } = -1; // Auto-detect
        public int BatchSize { get; set; } = 2048;
        public bool FlashAttention { get; set; } = true;
        public string AdditionalArgs { get; set; } = "";
    }

    public class GitHubRelease
    {
        [JsonProperty("assets")]
        public ReleaseAsset[] Assets { get; set; }
    }

    public class ReleaseAsset
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }
    }
}