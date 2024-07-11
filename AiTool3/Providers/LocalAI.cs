using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Interfaces;
using Newtonsoft.Json;
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

        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, Control textbox = null, bool useStreaming = false)
        {
            var req = new LocalAIRequest
            {
                model = apiModel.ModelName,
                messages = new List<LocalAIMessage>
                {
                    new LocalAIMessage
                    {
                        Role = "system",
                        Content = conversation.SystemPromptWithDateTime(),
                    }
                },
                stream = useStreaming
            };

            req.messages.AddRange(conversation.messages.Select(m => new LocalAIMessage
            {
                Role = m.role,
                Content = m.content
            }));

            var a = AiTool3.Settings.Settings.Load();

            var json = JsonConvert.SerializeObject(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            if (!IsPortOpen(a.OllamaLocalPort))
            {
                StartOllama();
            }

            var url = GetAdjustedUrl(apiModel.Url, a.OllamaLocalPort);

            if (useStreaming)
            {
                return await HandleStreamingResponse(url, content, cancellationToken, textbox);
            }
            else
            {
                return await HandleNonStreamingResponse(url, content, cancellationToken);
            }
        }

        private async Task<AiResponse> HandleStreamingResponse(string url, StringContent content, CancellationToken cancellationToken, Control textbox)
        {
            var response = await client.PostAsync(url, content, cancellationToken);
            var stream = await response.Content.ReadAsStreamAsync();
            var reader = new StreamReader(stream);

            StringBuilder fullResponse = new StringBuilder();
            int promptEvalCount = 0;
            int evalCount = 0;

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;

                var chunk = JsonConvert.DeserializeObject<LocalAIStreamResponse>(line);

                if (chunk.Message != null && !string.IsNullOrEmpty(chunk.Message.Content))
                {
                    fullResponse.Append(chunk.Message.Content);
                    Debug.WriteLine(chunk.Message.Content);
                    if (textbox != null)
                    {
                        textbox.Invoke(new Action(() => textbox.Text = fullResponse.ToString()));
                    }
                }

                if (chunk.Done)
                {
                    promptEvalCount = chunk.PromptEvalCount;
                    evalCount = chunk.EvalCount;
                    break;
                }
            }

            return new AiResponse
            {
                ResponseText = fullResponse.ToString(),
                Success = true,
                TokenUsage = new TokenUsage(promptEvalCount.ToString(), evalCount.ToString())
            };
        }

        private async Task<AiResponse> HandleNonStreamingResponse(string url, StringContent content, CancellationToken cancellationToken)
        {
            var response = await client.PostAsync(url, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<LocalAINonStreamingResponse>(responseContent);

            return new AiResponse
            {
                ResponseText = result.Message.Content,
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

        private void StartOllama()
        {
            var psi = new ProcessStartInfo("ollama", "run gemma2")
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
                process.Kill();
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