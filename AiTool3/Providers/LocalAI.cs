using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AiTool3.Providers
{
    internal class LocalAI : IAiService
    {
        HttpClient client = new HttpClient();
        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;
        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, Settings.Settings currentSettings, bool useStreaming = false)
        {
            var req = new JObject
            {
                ["model"] = apiModel.ModelName,
                ["messages"] = new JArray
                {
                    new JObject
                    {
                        ["role"] = "system",
                        ["content"] = conversation.SystemPromptWithDateTime()
                    }
                },
                ["stream"] = useStreaming
            };

            var x = conversation.messages.Select(m => new LocalAIMessage
            {
                Role = m.role,
                Content = m.content
            });

            foreach (var m in x)
            {
                req["messages"].Last.AddAfterSelf(JObject.FromObject(m));
            }

            if (base64image != null)
            {
                req["messages"].Last["images"] = new JArray { base64image };
                base64image = null;
            }

            var newInput = await OllamaEmbeddingsHelper.AddEmbeddingsToInput(conversation, currentSettings, conversation.messages.Last().content);
            req["messages"].Last()["content"] = newInput;

            var settings = AiTool3.Settings.Settings.Load();

            var json = JsonConvert.SerializeObject(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            StartOllama(apiModel.ModelName);

            var url = GetAdjustedUrl(apiModel.Url, settings.OllamaLocalPort);

            if (useStreaming)
            {
                return await HandleStreamingResponse(url, content, cancellationToken);
            }
            else
            {
                return await HandleNonStreamingResponse(url, content, cancellationToken);
            }
        }

        private async Task<AiResponse> HandleStreamingResponse(string url, StringContent content, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = content;

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            StringBuilder fullResponse = new StringBuilder();
            StringBuilder lineBuilder = new StringBuilder();
            int promptEvalCount = 0;
            int evalCount = 0;

            byte[] buffer = new byte[48];  // Read in larger chunks
            var decoder = Encoding.UTF8.GetDecoder();

            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (bytesRead == 0) break;

                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytesRead)];
                int charsDecodedCount = decoder.GetChars(buffer, 0, bytesRead, chars, 0);

                for (int i = 0; i < charsDecodedCount; i++)
                {
                    char c = chars[i];
                    lineBuilder.Append(c);

                    if (c == '\n')
                    {
                        Debug.WriteLine(lineBuilder.ToString().Trim());
                        ProcessLine(lineBuilder.ToString().Trim(), fullResponse, ref promptEvalCount, ref evalCount);
                        lineBuilder.Clear();
                    }
                }
            }

            // Process any remaining content
            if (lineBuilder.Length > 0)
            {
                ProcessLine(lineBuilder.ToString().Trim(), fullResponse, ref promptEvalCount, ref evalCount);
            }
            StreamingComplete?.Invoke(this, null);
            return new AiResponse
            {
                ResponseText = fullResponse.ToString(),
                Success = true,
                TokenUsage = new TokenUsage(promptEvalCount.ToString(), evalCount.ToString())
            };
        }

        private void ProcessLine(string line, StringBuilder fullResponse, ref int promptEvalCount, ref int evalCount)
        {
            if (string.IsNullOrEmpty(line)) return;

            try
            {
                var chunkResponse = JsonConvert.DeserializeObject<LocalAIStreamResponse>(line);

                if (chunkResponse?.Message != null && !string.IsNullOrEmpty(chunkResponse.Message.Content))
                {
                    fullResponse.Append(chunkResponse.Message.Content);
                    Debug.WriteLine(chunkResponse.Message.Content);
                    StreamingTextReceived?.Invoke(this, chunkResponse.Message.Content);
                }

                if (chunkResponse?.Done == true)
                {
                    promptEvalCount = chunkResponse.PromptEvalCount;
                    evalCount = chunkResponse.EvalCount;
                }
            }
            catch (JsonException)
            {
                // Handle or log JSON parsing errors
            }
        }


        private async Task<AiResponse> HandleNonStreamingResponse(string url, StringContent content, CancellationToken cancellationToken)
        {
            var response = await client.PostAsync(url, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<LocalAINonStreamingResponse>(responseContent);

            return new AiResponse
            {
                ResponseText = result!.Message?.Content,
                Success = true,
                TokenUsage = new TokenUsage(result.PromptEvalCount.ToString(), result.EvalCount.ToString())
            };
        }

        private bool IsPortOpen(int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    return client.ConnectAsync("127.0.0.1", port).Wait(100);
                }
            }
            catch
            {
                return false;
            }
        }

        private void StartOllama(string modelName)
        {
            var psi = new ProcessStartInfo("ollama", $"run {modelName}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = Process.Start(psi);

            new Thread(() =>
            {
                Thread.Sleep(1000);
                process!.Kill();
            }).Start();
        }

        private string GetAdjustedUrl(string originalUrl, int localPort)
        {
            if (originalUrl.Contains("11434") && localPort != 11434)
            {
                return originalUrl.Replace("11434", localPort.ToString());
            }
            return originalUrl;
        }
    }

    public class LocalAINonStreamingResponse
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("message")]
        public LocalAIMessage Message { get; set; }

        [JsonProperty("done")]
        public bool Done { get; set; }

        [JsonProperty("total_duration")]
        public long TotalDuration { get; set; }

        [JsonProperty("load_duration")]
        public long LoadDuration { get; set; }

        [JsonProperty("prompt_eval_count")]
        public int PromptEvalCount { get; set; }

        [JsonProperty("prompt_eval_duration")]
        public long PromptEvalDuration { get; set; }

        [JsonProperty("eval_count")]
        public int EvalCount { get; set; }

        [JsonProperty("eval_duration")]
        public long EvalDuration { get; set; }
    }


        public class LocalAIRequest
    {
        [JsonProperty("model")]
        public string model { get; set; }

        [JsonProperty("messages")]
        public List<LocalAIMessage> messages { get; set; }

        [JsonProperty("stream")]
        public bool stream { get; set; }
    }

    public class LocalAIMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public class LocalAIStreamResponse
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("message")]
        public LocalAIMessage Message { get; set; }

        [JsonProperty("done")]
        public bool Done { get; set; }

        [JsonProperty("total_duration")]
        public long TotalDuration { get; set; }

        [JsonProperty("load_duration")]
        public long LoadDuration { get; set; }

        [JsonProperty("prompt_eval_count")]
        public int PromptEvalCount { get; set; }

        [JsonProperty("prompt_eval_duration")]
        public long PromptEvalDuration { get; set; }

        [JsonProperty("eval_count")]
        public int EvalCount { get; set; }

        [JsonProperty("eval_duration")]
        public long EvalDuration { get; set; }
    }
}