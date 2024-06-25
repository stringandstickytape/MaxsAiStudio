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
    internal class Gemini : IAiService
    {
        HttpClient client = new HttpClient();
        public Gemini()
        {
        }

        public async Task<AiResponse> FetchResponse(Model apiModel,  Conversation conversation, string base64image)
        {
            // var req = new GeminiRequest
            // {
            //     model = model,
            //     //system = conversation.SystemPromptWithDateTime(),
            //     max_tokens = 4000,
            //     messages =
            //         conversation.messages.Select(m => new GeminiMessage
            //         {
            //             role = m.role,
            //             content = m.content
            //         }).ToList()
            // };
            //
            // var json = System.Text.Json.JsonSerializer.Serialize(req);
            //
            // var content = new StringContent(json, Encoding.UTF8, "application/json");
            //
            // var response = await client.PostAsync("https://api.Gemini.com/openai/v1/chat/completions", content).ConfigureAwait(false);
            //
            // var stream = await response.Content.ReadAsStreamAsync();
            // var buffer = new byte[256];
            // var bytesRead = 0;
            //
            // StringBuilder sb = new StringBuilder();
            //
            // while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            // {
            //     var chunk = new byte[bytesRead];
            //     Array.Copy(buffer, chunk, bytesRead);
            //     var chunkTxt = System.Text.Encoding.UTF8.GetString(chunk);
            //
            //     sb.Append(chunkTxt);
            //     Debug.WriteLine(chunkTxt);
            // }
            // var allTxt = sb.ToString();
            //
            // // derseialize the response
            // var completion = System.Text.Json.JsonSerializer.Deserialize<GeminiResponse>(allTxt);
            // //if (completion.type == "error")
            // //{
            // //    return new AiResponse { ResponseText = "error - "/* + completion.error.message*/, Success = false };
            // //}
            // //var deltas = ExtractTextDeltas(sb.ToString().Split(new char[] { '\n' }).ToList());

            string url = $"{apiModel.Url}{apiModel.ModelName}:generateContent?key={apiModel.Key}";

            //var obj = new GeminiOutgoingMessage
            //{
            //    contents = new GeminiOutgoingContent[]
            //    {
            //        new GeminiOutgoingContent
            //        {
            //            role = "user",
            //            parts = new GeminiOutgoingPart[]
            //            {
            //                new GeminiOutgoingPart
            //                {
            //                    text = input
            //                }
            //            }
            //        }
            //    }
            //};



            var obj = new GeminiOutgoingMessage
            {
                //system = conversation.SystemPromptWithDateTime(),
                contents =
                    conversation.messages.Select(m => new GeminiOutgoingContent
                    {
                        role = m.role == "assistant" ? "model" : m.role,
                        parts = new List<GeminiOutgoingPart>
                        {
                            new GeminiOutgoingPart
                            {
                                text = m.content
                            }
                        }
                    }).ToArray()
            };

            obj.contents = obj.contents.Prepend(new GeminiOutgoingContent
            {
                role = "model",
                parts = new List<GeminiOutgoingPart>
                {
                    new GeminiOutgoingPart
                    {
                        text = "Understood."
                    }
                }
            }).ToArray();

            obj.contents = obj.contents.Prepend(new GeminiOutgoingContent
            {
                role = "user",
                parts = new List<GeminiOutgoingPart>
                {
                    new GeminiOutgoingPart
                    {
                        text = conversation.SystemPromptWithDateTime()
                    }
                }
            }).ToArray();

            //var sysContent = new GeminiOutgoingContent
            //{
            //    role = "user",
            //    parts = new GeminiOutgoingPart[]
            //    {
            //        new GeminiOutgoingPart
            //        {
            //            text = $"You are an AI; here is your system message: {conversation.SystemPromptWithDateTime()}"
            //        }
            //    }
            //};

            if (base64image != null)
            {
                obj.contents.Last().parts.Add(new GeminiOutgoingPart
                {
                    inline_data = new GeminiOutgoingInlineData
                    {
                        mime_type = "image/jpeg",
                        data = base64image
                    }
                });
            }

            var jsonPayload = JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            string responseContent = "";
            // Initialize HttpClient
            using (HttpClient client = new HttpClient())
            {
                // Set request headers
                //client.DefaultRequestHeaders.Add("Content-Type", "application/json");

                // Make the POST request
                using (HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json"))
                {
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    // Check the response status
                    if (response.IsSuccessStatusCode)
                    {
                        responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(responseContent);

                        // deserialize the response
                        var completion = JsonSerializer.Deserialize<GeminiIncomingMessage>(responseContent);
                        return new AiResponse { ResponseText = completion.candidates[0].content.parts[0].text, Success = true };
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode}");
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(errorContent);
                    }
                }
            }


            //

            return new AiResponse { ResponseText = responseContent, Success = true };
        }
    }


    public class GeminiOutgoingMessage
    {
        public GeminiOutgoingContent[] contents { get; set; }
    }

    public class GeminiOutgoingContent
    {
        public string role { get; set; }
        public List<GeminiOutgoingPart> parts { get; set; }
    }

    public class GeminiOutgoingPart
    {
        public string text { get; set; }
        public GeminiOutgoingInlineData inline_data { get; set; }
    }

    public class GeminiOutgoingInlineData
    {
        public string mime_type { get; set; }
        public string data { get; set; }
    }

    public class GeminiIncomingMessage
    {
        public GeminiIncomingCandidate[] candidates { get; set; }
        public GeminiIncomingUsagemetadata usageMetadata { get; set; }
    }

    public class GeminiIncomingUsagemetadata
    {
        public int promptTokenCount { get; set; }
        public int candidatesTokenCount { get; set; }
        public int totalTokenCount { get; set; }
    }

    public class GeminiIncomingCandidate
    {
        public GeminiIncomingContent content { get; set; }
        public string finishReason { get; set; }
        public int index { get; set; }
        public GeminiIncomingSafetyrating[] safetyRatings { get; set; }
    }

    public class GeminiIncomingContent
    {
        public GeminiIncomingPart[] parts { get; set; }
        public string role { get; set; }
    }

    public class GeminiIncomingPart
    {
        public string text { get; set; }
    }

    public class GeminiIncomingSafetyrating
    {
        public string category { get; set; }
        public string probability { get; set; }
    }


}