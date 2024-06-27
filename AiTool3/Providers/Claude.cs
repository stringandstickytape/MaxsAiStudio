using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Interfaces;
using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.Windows.Forms.Design.AxImporter;

namespace AiTool3.Providers
{
    internal class Claude : IAiService
    {
        HttpClient client = new HttpClient();

        bool clientInitialised = false;

        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image)
        {
            if (!clientInitialised)
            {
                client.DefaultRequestHeaders.Add("x-api-key", apiModel.Key);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                clientInitialised = true;
            }

            var req = new JObject
            {
                ["model"] = apiModel.ModelName,
                ["system"] = conversation.SystemPromptWithDateTime(),
                ["max_tokens"] = 4096,
                ["messages"] = new JArray(
                    conversation.messages.Select(m => new JObject
                    {
                        ["role"] = m.role,
                        ["content"] = m.content
                    })
                )
            };

            var json = JsonConvert.SerializeObject(req);

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

            // deserialize the response

            var completion = JsonConvert.DeserializeObject<JObject>(allTxt);

            // get the number of input and output tokens but don't b0rk if either is missing
            var inputTokens = completion["usage"]?["input_tokens"]?.ToString();
            var outputTokens = completion["usage"]?["output_tokens"]?.ToString();

            if (completion["type"].ToString() == "error")
            {
                return new AiResponse { ResponseText = "error - " + completion["error"]["message"].ToString(), Success = false };
            }
            return new AiResponse { ResponseText = completion["content"][0]["text"].ToString(), Success = true };
        }
    }
}