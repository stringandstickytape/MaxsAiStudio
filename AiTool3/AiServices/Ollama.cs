using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.Interfaces;
using AiTool3.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AiTool3.AiServices
{
    internal class Ollama : AiServiceBase
    {
        public Ollama()
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

            var requestPayload = CreateRequestPayload(apiModel, conversation, useStreaming, currentSettings);

            // Build the prompt from the conversation
            var promptBuilder = new StringBuilder();

            // Add system prompt if present
            if (!string.IsNullOrEmpty(conversation.systemprompt))
            {
                promptBuilder.AppendLine(conversation.SystemPromptWithDateTime());
                promptBuilder.AppendLine();
            }

            // Add conversation messages
            foreach (var message in conversation.messages)
            {
                promptBuilder.AppendLine($"{message.role}: {message.content}");
                promptBuilder.AppendLine();
            }

            requestPayload["prompt"] = promptBuilder.ToString().TrimEnd();

            // Handle images if present
            if (!string.IsNullOrEmpty(base64image))
            {
                var images = new JArray { base64image };
                requestPayload["images"] = images;
            }

            if (toolIDs?.Any() == true)
            {
                AddToolsToRequest(requestPayload, toolIDs);
            }

            if (addEmbeddings)
            {
                var lastMessage = conversation.messages.Last().content;
                var newInput = await AddEmbeddingsIfRequired(conversation, currentSettings, mustNotUseEmbedding, addEmbeddings, lastMessage);
                requestPayload["prompt"] = newInput;
            }

            var json = JsonConvert.SerializeObject(requestPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            return await HandleResponse(apiModel, content, useStreaming, cancellationToken);
        }

        protected override JObject CreateRequestPayload(
            Model apiModel,
            Conversation conversation,
            bool useStreaming,
            SettingsSet currentSettings)
        {
            return new JObject
            {
                ["model"] = apiModel.ModelName,
                ["stream"] = useStreaming,
                ["options"] = new JObject
                {
                    ["temperature"] = currentSettings.Temperature,
                    ["num_predict"] = 4096,
                }
            };
        }

        protected override async Task<AiResponse> HandleStreamingResponse(
            Model apiModel,
            HttpContent content,
            CancellationToken cancellationToken)
        {
            using var response = await SendRequest(apiModel, content, cancellationToken, true);
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            var responseBuilder = new StringBuilder();
            int promptEvalCount = 0;
            int evalCount = 0;

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrEmpty(line)) continue;

                try
                {
                    var chunk = JsonConvert.DeserializeObject<JObject>(line);

                    if (chunk["response"] != null)
                    {
                        var text = chunk["response"].ToString();
                        responseBuilder.Append(text);
                        OnStreamingDataReceived(text);
                    }

                    if (chunk["done"]?.Value<bool>() == true)
                    {
                        promptEvalCount = chunk["prompt_eval_count"]?.Value<int>() ?? 0;
                        evalCount = chunk["eval_count"]?.Value<int>() ?? 0;
                    }
                }
                catch (JsonReaderException)
                {
                    // Handle JSON parsing errors
                    continue;
                }
            }

            OnStreamingComplete();

            return new AiResponse
            {
                ResponseText = responseBuilder.ToString(),
                Success = true,
                TokenUsage = new TokenUsage(
                    promptEvalCount.ToString(),
                    evalCount.ToString()
                )
            };
        }

        protected override async Task<AiResponse> HandleNonStreamingResponse(
            Model apiModel,
            HttpContent content,
            CancellationToken cancellationToken)
        {
            var response = await SendRequest(apiModel, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonConvert.DeserializeObject<JObject>(responseContent);

            return new AiResponse
            {
                ResponseText = result["response"]?.ToString(),
                Success = true,
                TokenUsage = new TokenUsage(
                    result["prompt_eval_count"]?.ToString() ?? "0",
                    result["eval_count"]?.ToString() ?? "0"
                )
            };
        }

        protected override TokenUsage ExtractTokenUsage(JObject response)
        {
            return new TokenUsage(
                response["prompt_eval_count"]?.ToString() ?? "0",
                response["eval_count"]?.ToString() ?? "0"
            );
        }

        protected override ToolFormat GetToolFormat()
        {
            return ToolFormat.Ollama;
        }
    }
}