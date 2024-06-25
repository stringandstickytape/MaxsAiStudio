using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Interfaces;
using Newtonsoft.Json;
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

        public async Task<AiResponse> FetchResponse(Model apiModel,Conversation conversation, string base64image)
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

                }
                ,
                stream = false
            };

            req.messages.AddRange(conversation.messages.Select(m => new LocalAIMessage
            {
                Role = m.role,
                Content = m.content

            }));

            //if (base64image != null)
            //{
            //    req.messages.Last().Content.Add(new LocalAIContent { Type = "image_url", ImageUrl = new LocalAIImageUrl { ImageUrl = $"data:image/jpeg;base64,{base64image}" } });
            //}

            var json = System.Text.Json.JsonSerializer.Serialize(req);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            if (!IsPortOpen(11434))
            {


                //System.Diagnostics.Process.Start("ollama", "run codeqwen:chat");

                // run "ollama run codeqwen:chat" but make sure the window is hidden
                var psi = new ProcessStartInfo("ollama", "run llama3")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var process = Process.Start(psi);

                // create a new thread which will kill that process in 1 second
                new Thread(() =>
                {
                    Thread.Sleep(1000);
                    process.Kill();
                }).Start();

            }

            var response = await client.PostAsync(apiModel.Url, content).ConfigureAwait(false);

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

            // derseialize the response


            var completion = System.Text.Json.JsonSerializer.Deserialize<LocalAIChatCompletion>(allTxt);
            Debug.WriteLine(allTxt);
            // deserialise allTxt dynamic using jsonconvert
            dynamic d = JsonConvert.DeserializeObject(allTxt);

            string s = d.message.content;






            //var deltas = ExtractTextDeltas(sb.ToString().Split(new char[] { '\n' }).ToList());
            return new AiResponse { ResponseText = s, Success = true };
        }

        bool IsPortOpen(int port)
        {

            try
            {
                var client = new TcpClient();
                if (client.ConnectAsync("127.0.0.1", port).Wait(100))
                {
                    return true;
                    // connection failure
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
        public string model { get; set; }
        public List<LocalAIMessage> messages { get; set; }
        public bool stream { get; set; }
    }

    public class LocalAILocalAIMessage
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class LocalAIChatCompletion
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("object")]
        public string Object { get; set; }

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("choices")]
        public List<LocalAIResponseChoice> Choices { get; set; }

        [JsonPropertyName("usage")]
        public LocalAIUsage Usage { get; set; }

        [JsonPropertyName("system_fingerprint")]
        public string SystemFingerprint { get; set; }
    }

    public class LocalAIChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public LocalAIMessage Message { get; set; }

        [JsonPropertyName("logprobs")]
        public object Logprobs { get; set; }

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }
    }

    public class LocalAIResponseChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public LocalAIResponseMessage Message { get; set; }

        [JsonPropertyName("logprobs")]
        public object Logprobs { get; set; }

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }
    }


    public class LocalAIMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public class LocalAIContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("text")]
        public string Text { get; set; }

    }

    public class LocalAIResponseMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public class LocalAIImageUrl
    {
        [JsonPropertyName("url")]
        public string ImageUrl { get; set; }
    }

    public class LocalAIUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}