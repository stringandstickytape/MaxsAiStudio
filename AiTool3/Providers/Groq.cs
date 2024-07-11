using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Windows.Forms.Design.AxImporter;

namespace AiTool3.Providers
{
    internal class Groq : IAiService
    {
        HttpClient client = new HttpClient();
        public Groq()
        {
        }

        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, Control textbox = null, bool useStreaming = false)
        {
            if (client.DefaultRequestHeaders.Authorization == null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiModel.Key);

            var req = new JObject
            {
                ["model"] = apiModel.ModelName,
                ["max_tokens"] = 4000,
                ["messages"] = new JArray(
                    conversation.messages.Select(m => new JObject
                    {
                        ["role"] = m.role,
                        ["content"] = m.content
                    })
                )
            };

            ((JArray)req["messages"]).Insert(0, new JObject
            {
                ["role"] = "system",
                ["content"] = conversation.SystemPromptWithDateTime()
            });

            var json = JsonConvert.SerializeObject(req);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(apiModel.Url, content, cancellationToken).ConfigureAwait(false);

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
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

            // deserialize the response
            var completion = JsonConvert.DeserializeObject<JObject>(allTxt);

            // get the number of input and output tokens but don't b0rk if either is missing
            var inputTokens = completion["usage"]?["prompt_tokens"]?.ToString();
            var outputTokens = completion["usage"]?["completion_tokens"]?.ToString();
            


            if (completion["choices"] == null)
            {
                return null;
            }
            return new AiResponse { ResponseText = completion["choices"][0]["message"]["content"].ToString(), Success = true, TokenUsage = new TokenUsage(inputTokens, outputTokens) };
        }
    }
}