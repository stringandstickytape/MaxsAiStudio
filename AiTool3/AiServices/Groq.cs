using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AiTool3.AiServices
{
    internal class Groq : AiServiceBase
    {
        public Groq()
        {
        }

        public override async Task<AiResponse> FetchResponse(
            Model apiModel,
            Conversation conversation,
            string base64image,
            string base64ImageType,
            CancellationToken cancellationToken,
            SettingsSet currentSettings,
            bool mustNotUseEmbedding,
            List<string> toolIDs,
            bool useStreaming = false,
            bool addEmbeddings = false)
        {
            InitializeHttpClient(apiModel, currentSettings);

            // Force streaming for Groq
            useStreaming = true;

            var requestPayload = CreateRequestPayload(apiModel, conversation, useStreaming, currentSettings);

            // Add messages to request
            var messagesArray = new JArray();
            foreach (var message in conversation.messages)
            {
                messagesArray.Add(new JObject
                {
                    ["role"] = message.role,
                    ["content"] = message.content
                });
            }
            requestPayload["messages"] = messagesArray;

            // Add system prompt
            ((JArray)requestPayload["messages"]).Insert(0, new JObject
            {
                ["role"] = "system",
                ["content"] = conversation.SystemPromptWithDateTime()
            });


            if (addEmbeddings)
            {
                var lastMessage = conversation.messages.Last().content;
                var newInput = await AddEmbeddingsIfRequired(conversation, currentSettings, mustNotUseEmbedding, addEmbeddings, lastMessage);
                requestPayload["messages"].Last["content"] = newInput;
            }

            requestPayload["stream"] = useStreaming; // set stream as true regardless

            var json = JsonConvert.SerializeObject(requestPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            return await HandleResponse(apiModel, content, useStreaming, cancellationToken);
        }

        protected override JObject CreateRequestPayload(Model apiModel, Conversation conversation, bool useStreaming, SettingsSet currentSettings)
        {
            return new JObject
            {
                ["model"] = apiModel.ModelName,
                ["max_tokens"] = 4000,
            };
        }

        protected override async Task<AiResponse> HandleStreamingResponse(Model apiModel, HttpContent content, CancellationToken cancellationToken)
        {
            var response = await SendRequest(apiModel, content, cancellationToken, streamingRequest: true);

            ValidateResponse(response);

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var buffer = new byte[48];
            var decoder = Encoding.UTF8.GetDecoder();
            var sb = new StringBuilder();
            var lineSb = new StringBuilder();
            var charBuffer = new char[1024];

            while (true)
            {
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (bytesRead == 0) break;

                var charsRead = decoder.GetChars(buffer, 0, bytesRead, charBuffer, 0);

                for (int i = 0; i < charsRead; i++)
                {
                    char c = charBuffer[i];
                    lineSb.Append(c);

                    if (c == '\n')
                    {
                        ProcessLine(lineSb.ToString(), sb);
                        lineSb.Clear();
                    }
                }
            }

            // Process any remaining content
            if (lineSb.Length > 0)
            {
                ProcessLine(lineSb.ToString(), sb);
            }

            OnStreamingComplete();
            return new AiResponse { ResponseText = sb.ToString(), Success = true };
        }


        private void ProcessLine(string line, StringBuilder sb)
        {
            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6).Trim();
                if (data == "[DONE]") return;

                try
                {
                    var jsonData = JsonConvert.DeserializeObject<JObject>(data);
                    var content = jsonData["choices"]?[0]?["delta"]?["content"]?.ToString();

                    if (!string.IsNullOrEmpty(content))
                    {
                        System.Diagnostics.Debug.WriteLine(content);
                        sb.Append(content);
                        OnStreamingDataReceived(content);
                    }
                }
                catch (Exception ex)
                {
                    // Handle JSON parsing error
                    Console.WriteLine($"Error parsing JSON: {ex.Message}");
                }
            }
        }

        protected override async Task<AiResponse> HandleNonStreamingResponse(Model apiModel, HttpContent content, CancellationToken cancellationToken)
        {
            var response = await SendRequest(apiModel, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var completion = JsonConvert.DeserializeObject<JObject>(responseContent);

            var inputTokens = completion["usage"]?["prompt_tokens"]?.ToString();
            var outputTokens = completion["usage"]?["completion_tokens"]?.ToString();

            if (completion["choices"] == null)
            {
                return null;
            }

            return new AiResponse
            {
                ResponseText = completion["choices"][0]["message"]["content"].ToString(),
                Success = true,
                TokenUsage = new TokenUsage(inputTokens, outputTokens)
            };
        }

        protected override TokenUsage ExtractTokenUsage(JObject response)
        {
            var inputTokens = response["usage"]?["prompt_tokens"]?.ToString();
            var outputTokens = response["usage"]?["completion_tokens"]?.ToString();
            return new TokenUsage(inputTokens, outputTokens);
        }
    }
}