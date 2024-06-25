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
    internal class Groq : IAiService
    {
        HttpClient client = new HttpClient();
        public Groq()
        {
        }

        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image)
        {
            if (client.DefaultRequestHeaders.Authorization == null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiModel.Key);

            var req = new GroqRequest
            {
                model = apiModel.ModelName,
                //system = conversation.SystemPromptWithDateTime(),
                max_tokens = 4000,
                messages =
                    conversation.messages.Select(m => new GroqMessage
                    {
                        role = m.role,
                        content = m.content
                    }).ToList()
            };

            req.messages = req.messages.Prepend(new GroqMessage
            {
                role = "system",
                content = conversation.SystemPromptWithDateTime()
            }).ToList();

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
            var completion = JsonSerializer.Deserialize<GroqResponse>(allTxt);
            if(completion.choices == null)
            {
                return null;
            }
            //if (completion.type == "error")
            //{
            //    return new AiResponse { ResponseText = "error - "/* + completion.error.message*/, Success = false };
            //}
            //var deltas = ExtractTextDeltas(sb.ToString().Split(new char[] { '\n' }).ToList());
            return new AiResponse { ResponseText = completion.choices[0].message.content, Success = true };
        }
    }
    public class GroqRequest
    {
        public bool stream { get; set; }
        public string model { get; set; }

        public List<GroqMessage> messages { get; set; }
        public int max_tokens { get; internal set; }
    }

    public class GroqMessage
    {
        public string role { get; set; }
        public string content { get; set; }
    }




    public class GroqResponse
    {
        public string id { get; set; }
        public string _object { get; set; }
        public int created { get; set; }
        public string model { get; set; }
        public Choice[] choices { get; set; }
        public Usage usage { get; set; }
        public string system_fingerprint { get; set; }
        public X_Groq x_groq { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public float prompt_time { get; set; }
        public int completion_tokens { get; set; }
        public float completion_time { get; set; }
        public int total_tokens { get; set; }
        public float total_time { get; set; }
    }

    public class X_Groq
    {
        public string id { get; set; }
    }

    public class Choice
    {
        public int index { get; set; }
        public Message message { get; set; }
        public object logprobs { get; set; }
        public string finish_reason { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }


}