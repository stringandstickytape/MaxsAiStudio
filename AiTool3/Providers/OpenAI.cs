using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
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

            var req = new JObject
            {
                ["model"] = apiModel.ModelName,
                ["messages"] = new JArray
                {
                    new JObject
                    {
                        ["role"] = "system",
                        ["content"] = new JArray
                        {
                            new JObject
                            {
                                ["type"] = "text",
                                ["text"] = conversation.SystemPromptWithDateTime()
                            }
                        }
                    }
                }
            };

            foreach (var m in conversation.messages)
            {
                req["messages"].Last.AddAfterSelf(new JObject
                {
                    ["role"] = m.role,
                    ["content"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "text",
                            ["text"] = m.content
                        }
                    }
                });
            }

            if (base64image != null)
            {
                ((JArray)req["messages"].Last["content"]).Add(new JObject
                {
                    ["type"] = "image_url",
                    ["image_url"] = new JObject
                    {
                        ["url"] = $"data:image/jpeg;base64,{base64image}"
                    }
                });
            }

            var json = JsonConvert.SerializeObject(req, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

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
            if (completion["choices"] == null)
                return null;
            return new AiResponse { ResponseText = completion["choices"][0]["message"]["content"].ToString(), Success = true };
        }
    }
}