using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.Embeddings;
using AiTool3.Interfaces;
using AiTool3.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace AiTool3.Providers
{
    internal class LocalAI : IAiService
    {
        public ToolManager ToolManager { get; set; }
        HttpClient client = new HttpClient();
        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;
        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, SettingsSet currentSettings, bool mustNotUseEmbedding, List<string> toolIDs, bool useStreaming = false, bool addEmbeddings = false)
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

            var x = conversation.messages.Select(m => new JObject
            {
                ["role"] = m.role,
                ["content"] = m.content,
                ["base64Image"] = m.base64image,
                ["base64Type"] = m.base64type
            });

            // copy the messages in, with base 64 images
            foreach (var m in x)
            {
                req["messages"].Last.AddAfterSelf(m);
                if (m["base64Image"] != null && m["base64Image"].ToString() != "")
                {
                    req["messages"].Last["images"] = new JArray { m["base64Image"] };
                }
            }

            if (addEmbeddings)
            {
                var newInput = await OllamaEmbeddingsHelper.AddEmbeddingsToInput(conversation, currentSettings, conversation.messages.Last().content, mustNotUseEmbedding);
                req["messages"].Last()["content"] = newInput;
            }

            var json = JsonConvert.SerializeObject(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            StartOllama(apiModel.ModelName);

            if (useStreaming)
            {
                return await HandleStreamingResponse(apiModel.Url, content, cancellationToken);
            }
            else
            {
                return await HandleNonStreamingResponse(apiModel.Url, content, cancellationToken);
            }
        }

        private async Task<AiResponse> HandleStreamingResponse(string url, StringContent content, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = content;
            client.Timeout = TimeSpan.FromSeconds(1800);
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
                var chunkResponse = JObject.Parse(line);

                if (chunkResponse["message"] != null && !string.IsNullOrEmpty(chunkResponse["message"]["content"]?.ToString()))
                {
                    fullResponse.Append(chunkResponse["message"]["content"]);
                    Debug.WriteLine(chunkResponse["message"]["content"]);
                    StreamingTextReceived?.Invoke(this, chunkResponse["message"]["content"].ToString());
                }

                if (chunkResponse["done"]?.Value<bool>() == true)
                {
                    promptEvalCount = chunkResponse["prompt_eval_count"]?.Value<int>() ?? 0;
                    evalCount = chunkResponse["eval_count"]?.Value<int>() ?? 0;
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
            var result = JObject.Parse(responseContent);

            return new AiResponse
            {
                ResponseText = result["message"]?["content"]?.ToString(),
                Success = true,
                TokenUsage = new TokenUsage(
                    result["prompt_eval_count"]?.ToString() ?? "0",
                    result["eval_count"]?.ToString() ?? "0"
                )
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

        public static void StartOllama(string modelName)
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
}