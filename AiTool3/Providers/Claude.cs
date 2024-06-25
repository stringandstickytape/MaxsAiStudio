using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Interfaces;
using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Windows.Forms.Design.AxImporter;

namespace AiTool3.Providers
{
    internal class Claude : IAiService
    {
        HttpClient client = new HttpClient();

        bool clientInitialised = false;

        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image)
        {
            if(!clientInitialised)
            {
                client.DefaultRequestHeaders.Add("x-api-key", apiModel.Key);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                clientInitialised = true;
            }

            var req = new ClaudeRequest
            {
                model = apiModel.ModelName,
                system = conversation.SystemPromptWithDateTime(),
                max_tokens = 4000,
                messages =
                    conversation.messages.Select(m => new ClaudeMessage
                    {
                        role = m.role,
                        content = m.content
                    }).ToList()
            };

            var json = JsonSerializer.Serialize(req);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

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
            var completion = JsonSerializer.Deserialize<ClaudeResponse>(allTxt);
            if (completion.type == "error")
            {
                return new AiResponse { ResponseText = "error - " + completion.error.message, Success = false };
            }
            return new AiResponse { ResponseText = completion.content[0].text, Success = true };
        }
    }
    public class ClaudeRequest
    {
        public bool stream { get; set; }
        public string model { get; set; }

        public string system { get; set; }
        public List<ClaudeMessage> messages { get; set; }
        public int max_tokens { get; internal set; }
    }

    public class ClaudeMessage
    {
        public string role { get; set; }
        public string content { get; set; }
    }


    public class ClaudeResponse
    {
        public string id { get; set; }
        public string type { get; set; }
        public string role { get; set; }
        public ClaudeResponseContent[] content { get; set; }
        public string model { get; set; }
        public string stop_reason { get; set; }
        public object stop_sequence { get; set; }
        public ClaudeUsage usage { get; set; }

        public ClaudeError error { get; set; }
    }

    public class ClaudeError
    {
        public string message { get; set; }
        public string type { get; set; }
    }

    public class ClaudeUsage
    {
        public int input_tokens { get; set; }
        public int output_tokens { get; set; }
    }

    public class ClaudeResponseContent
    {
        public string type { get; set; }
        public string text { get; set; }
    }
}