// AiServices\Veo.cs
using AiStudio4.Convs;
using AiStudio4.Core.Models;
using AiStudio4.DataModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedClasses.Providers;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace AiStudio4.AiServices
{
    internal class Veo : AiServiceBase
    {
        public ToolResponse ToolResponseSet { get; set; } = new ToolResponse { Tools = new List<ToolResponseItem>() };

        public Veo() { }

        protected override void ConfigureHttpClientHeaders(ApiSettings apiSettings)
        {
            // Veo uses key as URL parameter, not as Authorization header
        }

        protected override JObject CreateRequestPayload(string modelName, LinearConv conv, bool useStreaming, ApiSettings apiSettings)
        {
            // Not used for Veo
            return new JObject();
        }

        protected override JObject CreateMessageObject(LinearConvMessage message)
        {
            // Not used for Veo
            return new JObject();
        }

        protected override ToolFormat GetToolFormat() => ToolFormat.Gemini; // Placeholder

        protected override async Task<AiResponse> FetchResponseInternal(AiRequestOptions options, bool forceNoTools = false)
        {
            // Only support video generation, not chat
            // Expect prompt in options.Conv.messages[0].content
            string prompt = options.Conv?.messages?[0]?.content ?? options.CustomSystemPrompt;
            if (string.IsNullOrWhiteSpace(prompt))
                return new AiResponse { Success = false, ResponseText = "No prompt provided for Veo video generation." };

            InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings, 1800);
            string url = $"{ApiUrl}{ApiModel}:predictLongRunning?key={ApiKey}";

            var requestBody = new JObject
            {
                ["instances"] = new JArray
                {
                    new JObject { ["prompt"] = prompt }
                },
                ["parameters"] = new JObject
                {
                    ["aspectRatio"] = "16:9",
                    ["personGeneration"] = "dont_allow"
                }
            };

            var jsonPayload = JsonConvert.SerializeObject(requestBody);
            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync(url, content, options.CancellationToken);
            }
            catch (Exception ex)
            {
                return new AiResponse { Success = false, ResponseText = $"Veo API request failed: {ex.Message}" };
            }

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                return new AiResponse { Success = false, ResponseText = $"Veo API error: {errorContent}" };
            }

            // Parse operation name from response
            string responseContent = await response.Content.ReadAsStringAsync();
            var respObj = JsonConvert.DeserializeObject<JObject>(responseContent);
            string opName = respObj["name"]?.ToString();
            if (string.IsNullOrEmpty(opName))
                return new AiResponse { Success = false, ResponseText = "Veo did not return an operation name." };

            // Poll for operation completion
            string opUrl = $"{ApiUrl}operations/{opName}?key={ApiKey}";
            JObject opResult = null;
            int maxPolls = 60; // Wait up to ~3 minutes
            for (int i = 0; i < maxPolls; i++)
            {
                await Task.Delay(3000, options.CancellationToken);
                var opResp = await client.GetAsync(opUrl, options.CancellationToken);
                string opRespContent = await opResp.Content.ReadAsStringAsync();
                opResult = JsonConvert.DeserializeObject<JObject>(opRespContent);
                if (opResult!=null && opResult["done"]?.ToObject<bool>() == true)
                    break;
            }

            if (opResult == null || opResult["done"]?.ToObject<bool>() != true)
                return new AiResponse { Success = false, ResponseText = "Veo video generation timed out or did not complete." };

            // Extract video URL
            string videoUrl = opResult["response"]?["videoUri"]?.ToString();
            if (string.IsNullOrEmpty(videoUrl))
                return new AiResponse { Success = false, ResponseText = "Veo did not return a video URL." };

            // Optionally, download the video and return as attachment (base64)
            byte[] videoBytes = null;
            try
            {
                videoBytes = await client.GetByteArrayAsync(videoUrl);
            }
            catch (Exception ex)
            {
                // If download fails, just return the URL
                return new AiResponse
                {
                    Success = true,
                    ResponseText = $"Veo video generated: {videoUrl}",
                    Attachments = new List<Attachment>
                    {
                        new Attachment
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = "veo_video.mp4",
                            Type = "video/mp4",
                            Content = videoUrl, // Provide URL if download fails
                            Size = 0
                        }
                    }
                };
            }

            string base64Video = Convert.ToBase64String(videoBytes);
            return new AiResponse
            {
                Success = true,
                ResponseText = "Veo video generated successfully.",
                Attachments = new List<Attachment>
                {
                    new Attachment
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "veo_video.mp4",
                        Type = "video/mp4",
                        Content = base64Video,
                        Size = videoBytes.Length
                    }
                }
            };
        }

        protected override async Task<AiResponse> HandleStreamingResponse(HttpContent content, CancellationToken cancellationToken, Action<string> onStreamingUpdate, Action onStreamingComplete)
        {
            // Streaming not supported for Veo
            return new AiResponse { Success = false, ResponseText = "Streaming not supported for Veo." };
        }

        protected override async Task<AiResponse> HandleNonStreamingResponse(HttpContent content, CancellationToken cancellationToken, Action<string> onStreamingUpdate, Action onStreamingComplete)
        {
            // Not used for Veo
            return new AiResponse { Success = false, ResponseText = "Non-streaming not supported for Veo." };
        }

        protected override TokenUsage ExtractTokenUsage(JObject response)
        {
            // Not applicable for Veo
            return new TokenUsage("0", "0");
        }
    }
}