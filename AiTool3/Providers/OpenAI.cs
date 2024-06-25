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
    internal class OpenAI : IAiService
    {
        HttpClient client = new HttpClient();

        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image)
        {
            // if there's no bearer header set yet...
            if (client.DefaultRequestHeaders.Authorization == null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiModel.Key);

            var req = new OpenAIRequest
            {
                model = apiModel.ModelName,
                messages = new List<OpenAIMessage>
                {
                    new OpenAIMessage
                    {
                        Role = "system",
                        Content = new List<OpenAIContent> { new OpenAIContent { Type = "text", Text = conversation.SystemPromptWithDateTime() } }
                    }
                }
            };

            req.messages.AddRange(conversation.messages.Select(m => new OpenAIMessage
            {
                Role = m.role,
                Content = new List<OpenAIContent> { new OpenAIContent { Type = "text", Text = m.content } }
            }));

            if (base64image != null)
            {
                req.messages.Last().Content.Add(new OpenAIContent { Type = "image_url", ImageUrl = new OpenAIImageUrl { ImageUrl = $"data:image/jpeg;base64,{base64image}" } });
            }

            //serialize the req, excluding null properties
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var json = JsonSerializer.Serialize(req, options);

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
            var completion = JsonSerializer.Deserialize<OpenAIChatCompletion>(allTxt);
            if (completion.Choices == null)
                return null;
            //var deltas = ExtractTextDeltas(sb.ToString().Split(new char[] { '\n' }).ToList());
            return new AiResponse { ResponseText = completion.Choices[0].Message.Content, Success = true };
        }
    }

    public class OpenAIRequest
    {
        public bool stream { get; set; }
        public string model { get; set; }
        public List<OpenAIMessage> messages { get; set; }
    }

    public class OpenAIOpenAIMessage
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class OpenAIChatCompletion
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
        public List<OpenAIResponseChoice> Choices { get; set; }

        [JsonPropertyName("usage")]
        public OpenAIUsage Usage { get; set; }

        [JsonPropertyName("system_fingerprint")]
        public string SystemFingerprint { get; set; }
    }

    public class OpenAIChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public OpenAIMessage Message { get; set; }

        [JsonPropertyName("logprobs")]
        public object Logprobs { get; set; }

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }
    }

    public class OpenAIResponseChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public OpenAIResponseMessage Message { get; set; }

        [JsonPropertyName("logprobs")]
        public object Logprobs { get; set; }

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }
    }


    public class OpenAIMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public List<OpenAIContent> Content { get; set; }
    }

    public class OpenAIContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("image_url")]
        public OpenAIImageUrl ImageUrl { get; set; }
    }

    public class OpenAIResponseMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public class OpenAIImageUrl
    {
        [JsonPropertyName("url")]
        public string ImageUrl { get; set; }
    }

    public class OpenAIUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}