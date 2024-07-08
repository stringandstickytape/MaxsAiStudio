using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Windows.Forms.Design.AxImporter;

namespace AiTool3.Providers
{
    internal class LocalAI : IAiService
    {
        HttpClient client = new HttpClient();

        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken)
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
                stream = false
            };

            req.messages.AddRange(conversation.messages.Select(m => new LocalAIMessage
            {
                Role = m.role,
                Content = m.content
            }));

            // bit thin, this...
            var a = AiTool3.Settings.Settings.Load();

            var json = JsonConvert.SerializeObject(req);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            if (!IsPortOpen(a.OllamaLocalPort))
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

            var url = apiModel.Url;

            if(url.Contains("11434") && a.OllamaLocalPort != 11434)
            {
                url = url.Replace("11434", a.OllamaLocalPort.ToString());
            }

            var response = await client.PostAsync(apiModel.Url, content, cancellationToken).ConfigureAwait(false);

            var stream = await response.Content.ReadAsStreamAsync();
            var buffer = new byte[256];
            var bytesRead = 0;

            StringBuilder sb = new StringBuilder();

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                var chunk = new byte[bytesRead];
                Array.Copy(buffer, chunk, bytesRead);
                var chunkTxt = Encoding.UTF8.GetString(chunk);

                sb.Append(chunkTxt);
                Debug.WriteLine(chunkTxt);
            }
            var allTxt = sb.ToString();

            dynamic completion = JsonConvert.DeserializeObject(allTxt);
            Debug.WriteLine(allTxt);

            dynamic d = JsonConvert.DeserializeObject(allTxt);

            // {"model":"llama3","created_at":"2024-06-27T01:56:15.5661741Z","message":{"role":"assistant","content":"Hi!"},"done_reason":"stop","done":true,"total_duration":123866400,"load_duration":1042500,"prompt_eval_count":36,"prompt_eval_duration":59031000,"eval_count":3,"eval_duration":61634000}

            // get the number of input and output tokens but don't b0rk if either is missing
            var inputTokens = d.prompt_eval_count.ToString();
            var outputTokens = d.eval_count.ToString();


            string s = d.message.content;

            return new AiResponse { ResponseText = s, Success = true, TokenUsage = new TokenUsage(inputTokens, outputTokens) };
        }

        bool IsPortOpen(int port)
        {
            try
            {
                var client = new TcpClient();
                if (client.ConnectAsync("127.0.0.1", port).Wait(100))
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }
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

    public class LocalAILocalAIMessage
    {
        [JsonProperty("role")]
        public string role { get; set; }

        [JsonProperty("content")]
        public string content { get; set; }
    }

    public class LocalAIMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public class LocalAIContent
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class LocalAIImageUrl
    {
        [JsonProperty("url")]
        public string ImageUrl { get; set; }
    }
}