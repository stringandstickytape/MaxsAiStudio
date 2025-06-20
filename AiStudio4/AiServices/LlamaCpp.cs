// C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiServices/LlamaCpp.cs
using AiStudio4.Convs;
using AiStudio4.Core.Models;
using AiStudio4.Core.Tools;
using AiStudio4.DataModels;
using AiStudio4.InjectedDependencies;
using Newtonsoft.Json;
using SharedClasses.Providers;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Text;

namespace AiStudio4.AiServices
{
    public class LlamaCpp : AiServiceBase
    {
        private Process _llamaServerProcess;
        private readonly int _serverPort;
        private readonly LlamaCppSettings _settings;
        private readonly string _binaryDirectory;
        private readonly string _llamaServerPath;
        private bool _serverReady = false;

        public LlamaCpp()
        {
            _serverPort = GetAvailablePort();
            _binaryDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4", "LlamaCpp");
            _llamaServerPath = Path.Combine(_binaryDirectory, "llama-server.exe");
            _settings = new LlamaCppSettings();
            
            // Configure HTTP client for llama-server communication
            client.BaseAddress = new Uri($"http://127.0.0.1:{_serverPort}");
            client.Timeout = TimeSpan.FromMinutes(10);
        }

        protected override void ConfigureHttpClientHeaders(ApiSettings apiSettings)
        {
            // llama-server doesn't require authentication headers
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        protected override async Task<AiResponse> FetchResponseInternal(AiRequestOptions options, bool forceNoTools = false)
        {
            try
            {
                // Ensure llama-server binary is available
                await EnsureLlamaServerBinary();
                
                // Ensure server is running with the correct model
                await EnsureServerRunning(options);
                
                // Build request using common pattern
                return await MakeStandardApiCall(options, async (content) =>
                {
                    return await HandleStreamingResponse(content, options.CancellationToken, options.OnStreamingUpdate, options.OnStreamingComplete);
                }, forceNoTools);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error in LlamaCpp provider");
            }
        }

        public override async Task<AiResponse> FetchResponseWithToolLoop(AiRequestOptions options, Core.Interfaces.IToolExecutor toolExecutor, v4BranchedConv branchedConv, string parentMessageId, string assistantMessageId, string clientId)
        {
            await EnsureLlamaServerBinary();
            await EnsureServerRunning(options);

            return await ExecuteCommonToolLoop(
                options,
                toolExecutor,
                makeApiCall: async (opts) => await MakeLlamaCppApiCall(opts),
                createAssistantMessage: CreateLlamaCppAssistantMessage,
                createToolResultMessage: CreateLlamaCppToolResultMessage,
                options.MaxToolIterations ?? 10);
        }

        private async Task<AiResponse> MakeLlamaCppApiCall(AiRequestOptions options)
        {
            var request = await BuildCommonRequest(options);
            await CustomizeRequest(request, options);

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            return await HandleStreamingResponse(content, options.CancellationToken, options.OnStreamingUpdate, options.OnStreamingComplete);
        }

        private LinearConvMessage CreateLlamaCppAssistantMessage(AiResponse response)
        {
            // Use OpenAI-compatible format since llama-server provides OpenAI API
            var contentArray = new JArray();
            
            // Add text content if any
            var textContent = response.ContentBlocks?.FirstOrDefault(c => c.ContentType == ContentType.Text)?.Content;
            if (!string.IsNullOrEmpty(textContent))
            {
                contentArray.Add(new JObject
                {
                    ["type"] = "text",
                    ["text"] = textContent
                });
            }

            // Add tool calls if present
            if (response.ToolResponseSet?.Tools?.Any() == true)
            {
                foreach (var tool in response.ToolResponseSet.Tools)
                {
                    var toolCallId = $"call_{Guid.NewGuid():N}".Substring(0, 24);
                    
                    contentArray.Add(new JObject
                    {
                        ["type"] = "tool_call",
                        ["id"] = toolCallId,
                        ["function"] = new JObject
                        {
                            ["name"] = tool.ToolName,
                            ["arguments"] = tool.ResponseText
                        }
                    });
                }
            }

            return new LinearConvMessage
            {
                role = "assistant",
                content = contentArray.ToString()
            };
        }

        private LinearConvMessage CreateLlamaCppToolResultMessage(List<ContentBlock> toolResultBlocks)
        {
            // Use OpenAI-compatible format
            var contentArray = new JArray();
            
            foreach (var block in toolResultBlocks)
            {
                if (block.ContentType == ContentType.ToolResponse)
                {
                    var toolData = JsonConvert.DeserializeObject<dynamic>(block.Content);
                    var toolCallId = $"call_{Guid.NewGuid():N}".Substring(0, 24);
                    
                    contentArray.Add(new JObject
                    {
                        ["type"] = "tool_result",
                        ["tool_call_id"] = toolCallId,
                        ["content"] = toolData.result?.ToString() ?? ""
                    });
                }
            }
            
            return new LinearConvMessage
            {
                role = "user",
                content = contentArray.ToString()
            };
        }

        protected override LinearConvMessage CreateUserInterjectionMessage(string interjectionText)
        {
            return new LinearConvMessage
            {
                role = "user",
                content = interjectionText
            };
        }

        private async Task EnsureLlamaServerBinary()
        {
            if (File.Exists(_llamaServerPath))
            {
                System.Diagnostics.Debug.WriteLine($"LlamaCpp: Binary already exists at {_llamaServerPath}");
                return;
            }

            System.Diagnostics.Debug.WriteLine("LlamaCpp: Downloading llama-server binary...");
            await DownloadLatestLlamaServer();
        }

        private async Task DownloadLatestLlamaServer()
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

                System.Diagnostics.Debug.WriteLine($"LlamaCpp: Downloading main binary {mainAsset.Name} ({mainAsset.Size / 1024 / 1024} MB)");

                // Download and extract main binary
                await DownloadAndExtractAsset(httpClient, mainAsset, "main binary");

                // Download and extract CUDA runtime if available
                if (cudaRuntimeAsset != null)
                {
                    System.Diagnostics.Debug.WriteLine($"LlamaCpp: Downloading CUDA runtime {cudaRuntimeAsset.Name} ({cudaRuntimeAsset.Size / 1024 / 1024} MB)");
                    await DownloadAndExtractAsset(httpClient, cudaRuntimeAsset, "CUDA runtime");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("LlamaCpp: No CUDA runtime package found, using CPU-only version");
                }

                // Verify llama-server.exe exists
                if (!File.Exists(_llamaServerPath))
                {
                    throw new Exception("llama-server.exe not found after extraction");
                }

                System.Diagnostics.Debug.WriteLine($"LlamaCpp: Successfully set up llama-server at {_llamaServerPath}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download llama-server: {ex.Message}", ex);
            }
        }

        private async Task DownloadAndExtractAsset(HttpClient httpClient, ReleaseAsset asset, string description)
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
                            System.Diagnostics.Debug.WriteLine($"LlamaCpp: Extracted {entry.Name} from {description}");
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"LlamaCpp: Successfully extracted {description}");
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
                    System.Diagnostics.Debug.WriteLine($"LlamaCpp: Selected main binary: {asset.Name}");
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
                System.Diagnostics.Debug.WriteLine($"LlamaCpp: Found CUDA runtime: {asset.Name}");
            }
            
            return asset;
        }

        private async Task EnsureServerRunning(AiRequestOptions options)
        {
            var modelPath = GetModelPath(options);
            
            if (_llamaServerProcess?.HasExited != false || !_serverReady)
            {
                await StartLlamaServer(modelPath);
            }
        }

        private string GetModelPath(AiRequestOptions options)
        {
            // Try to get model path from various sources
            if (!string.IsNullOrEmpty(options.Model?.AdditionalParams))
            {
                try
                {
                    var settings = JsonConvert.DeserializeObject<LlamaCppSettings>(options.Model.AdditionalParams);
                    if (!string.IsNullOrEmpty(settings?.ModelPath) && File.Exists(settings.ModelPath))
                    {
                        return settings.ModelPath;
                    }
                }
                catch { }
            }

            // Fallback: try to interpret model name as path
            if (!string.IsNullOrEmpty(options.Model?.ModelName) && File.Exists(options.Model.ModelName))
            {
                return options.Model.ModelName;
            }

            throw new Exception("Model path not specified or file not found. Please provide a valid GGUF model path in the model configuration.");
        }

        private async Task StartLlamaServer(string modelPath)
        {
            try
            {
                // Kill existing process if any
                if (_llamaServerProcess?.HasExited == false)
                {
                    _llamaServerProcess.Kill();
                    await Task.Delay(1000);
                }

                var args = BuildServerArguments(modelPath);
                
                System.Diagnostics.Debug.WriteLine($"LlamaCpp: Starting server with args: {args}");

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

                // Log server output for debugging
                _llamaServerProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        System.Diagnostics.Debug.WriteLine($"LlamaCpp Server: {e.Data}");
                };

                _llamaServerProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        System.Diagnostics.Debug.WriteLine($"LlamaCpp Server Error: {e.Data}");
                };

                _llamaServerProcess.Start();
                _llamaServerProcess.BeginOutputReadLine();
                _llamaServerProcess.BeginErrorReadLine();

                // Wait for server to be ready
                await WaitForServerReady();
                _serverReady = true;

                System.Diagnostics.Debug.WriteLine($"LlamaCpp: Server started successfully on port {_serverPort}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to start llama-server: {ex.Message}", ex);
            }
        }

        private string BuildServerArguments(string modelPath)
        {
            var args = new List<string>
            {
                $"--model \"{modelPath}\"",
                "--jinja",  // Enable tool calling support
                "-fa",      // Flash attention
                $"--host 127.0.0.1",
                $"--port {_serverPort}",
                $"--ctx-size {_settings.ContextSize}",
                $"--batch-size {_settings.BatchSize}",
                $"--threads {_settings.Threads}"
            };

            // Auto-detect GPU layers if not specified
            if (_settings.GpuLayerCount == -1)
            {
                args.Add("--n-gpu-layers 999"); // Let llama.cpp auto-detect
            }
            else if (_settings.GpuLayerCount > 0)
            {
                args.Add($"--n-gpu-layers {_settings.GpuLayerCount}");
            }

            if (_settings.FlashAttention)
            {
                args.Add("-fa");
            }

            if (!string.IsNullOrEmpty(_settings.AdditionalArgs))
            {
                args.Add(_settings.AdditionalArgs);
            }

            return string.Join(" ", args);
        }

        private async Task WaitForServerReady()
        {
            var timeout = TimeSpan.FromSeconds(60);
            var start = DateTime.Now;

            while (DateTime.Now - start < timeout)
            {
                try
                {
                    var response = await client.GetAsync("/health");
                    if (response.IsSuccessStatusCode)
                    {
                        return;
                    }
                }
                catch
                {
                    // Server not ready yet
                }

                await Task.Delay(1000);
            }

            throw new Exception($"llama-server failed to start within {timeout.TotalSeconds} seconds");
        }

        private static int GetAvailablePort()
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        protected override JObject CreateRequestPayload(string modelName, LinearConv conv, ApiSettings apiSettings)
        {
            var messages = new JArray();

            // Add system message if present
            if (!string.IsNullOrEmpty(conv.systemprompt))
            {
                messages.Add(new JObject
                {
                    ["role"] = "system",
                    ["content"] = conv.SystemPromptWithDateTime()
                });
            }

            // Add conversation messages
            foreach (var message in conv.messages)
            {
                messages.Add(CreateMessageObject(message));
            }

            return new JObject
            {
                ["model"] = "local-model", // llama-server ignores this
                ["messages"] = messages,
                ["temperature"] = apiSettings.Temperature,
                ["max_tokens"] = 2048,
                ["stream"] = true
            };
        }

        protected override JObject CreateMessageObject(LinearConvMessage message)
        {
            var messageObj = new JObject
            {
                ["role"] = message.role,
                ["content"] = message.content ?? ""
            };

            // Handle attachments if any
            if (message.attachments?.Any() == true)
            {
                var contentArray = new JArray();
                
                // Add text content
                if (!string.IsNullOrEmpty(message.content))
                {
                    contentArray.Add(new JObject
                    {
                        ["type"] = "text",
                        ["text"] = message.content
                    });
                }

                // Add image attachments
                foreach (var attachment in message.attachments.Where(a => a.Type.StartsWith("image/")))
                {
                    contentArray.Add(new JObject
                    {
                        ["type"] = "image_url",
                        ["image_url"] = new JObject
                        {
                            ["url"] = $"data:{attachment.Type};base64,{attachment.Content}"
                        }
                    });
                }

                messageObj["content"] = contentArray;
            }

            return messageObj;
        }

        protected override async Task<AiResponse> HandleStreamingResponse(
            HttpContent content,
            CancellationToken cancellationToken,
            Action<string> onStreamingUpdate,
            Action onStreamingComplete)
        {
            try
            {
                using var response = await SendRequest(content, cancellationToken);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await ProcessLlamaCppStream(stream, cancellationToken, onStreamingUpdate, onStreamingComplete);
            }
            catch (OperationCanceledException)
            {
                return HandleCancellation("", new TokenUsage("0", "0"), new ToolResponse { Tools = new List<ToolResponseItem>() });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error during streaming response");
            }
        }

        private async Task<AiResponse> ProcessLlamaCppStream(
            Stream stream,
            CancellationToken cancellationToken,
            Action<string> onStreamingUpdate,
            Action onStreamingComplete)
        {
            var responseBuilder = new StringBuilder();
            var toolResponseSet = new ToolResponse { Tools = new List<ToolResponseItem>() };
            string chosenTool = null;

            using var reader = new StreamReader(stream);
            string line;

            try
            {
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (line.StartsWith("data: "))
                    {
                        var data = line.Substring(6);
                        if (data == "[DONE]")
                            break;

                        try
                        {
                            var chunk = JsonConvert.DeserializeObject<JObject>(data);
                            var choices = chunk["choices"] as JArray;
                            
                            if (choices?.Count > 0)
                            {
                                var choice = choices[0] as JObject;
                                var delta = choice["delta"] as JObject;

                                // Handle text content
                                var content = delta?["content"]?.ToString();
                                if (!string.IsNullOrEmpty(content))
                                {
                                    responseBuilder.Append(content);
                                    onStreamingUpdate?.Invoke(content);
                                }

                                // Handle tool calls
                                var toolCalls = delta?["tool_calls"] as JArray;
                                if (toolCalls?.Count > 0)
                                {
                                    foreach (var toolCall in toolCalls)
                                    {
                                        var function = toolCall["function"];
                                        var name = function?["name"]?.ToString();
                                        var arguments = function?["arguments"]?.ToString();

                                        if (!string.IsNullOrEmpty(name))
                                        {
                                            chosenTool = name;
                                            var existingTool = toolResponseSet.Tools.FirstOrDefault(t => t.ToolName == name);
                                            if (existingTool == null)
                                            {
                                                toolResponseSet.Tools.Add(new ToolResponseItem
                                                {
                                                    ToolName = name,
                                                    ResponseText = arguments ?? ""
                                                });
                                            }
                                            else
                                            {
                                                existingTool.ResponseText += arguments ?? "";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (JsonException)
                        {
                            // Skip malformed JSON chunks
                        }
                    }
                }

                onStreamingComplete?.Invoke();

                return new AiResponse
                {
                    ContentBlocks = new List<ContentBlock> 
                    { 
                        new ContentBlock 
                        { 
                            Content = responseBuilder.ToString(), 
                            ContentType = ContentType.Text 
                        } 
                    },
                    Success = true,
                    TokenUsage = new TokenUsage("0", "0"), // llama-server doesn't always provide token counts in stream
                    ChosenTool = chosenTool,
                    ToolResponseSet = toolResponseSet,
                    IsCancelled = false
                };
            }
            catch (OperationCanceledException)
            {
                return HandleCancellation(responseBuilder.ToString(), new TokenUsage("0", "0"), toolResponseSet, chosenTool);
            }
        }

        protected override async Task<HttpResponseMessage> SendRequest(HttpContent content, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
            {
                Content = content
            };

            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }

        protected override ToolFormat GetToolFormat() => ToolFormat.OpenAI;
        protected override ProviderFormat GetProviderFormat() => ProviderFormat.OpenAI;

        protected override TokenUsage ExtractTokenUsage(JObject response)
        {
            var usage = response["usage"];
            return new TokenUsage(
                usage?["prompt_tokens"]?.ToString() ?? "0",
                usage?["completion_tokens"]?.ToString() ?? "0"
            );
        }

        //public override void Dispose()
        //{
        //    try
        //    {
        //        if (_llamaServerProcess?.HasExited == false)
        //        {
        //            _llamaServerProcess.Kill();
        //            _llamaServerProcess.WaitForExit(5000);
        //        }
        //        _llamaServerProcess?.Dispose();
        //    }
        //    catch { }
        //    
        //    base.Dispose();
        //}
    }

    public class LlamaCppSettings
    {
        public string ModelPath { get; set; }
        public int ContextSize { get; set; } = 4096;
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