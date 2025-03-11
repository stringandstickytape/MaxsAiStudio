using AiStudio4.Convs;
using AiStudio4.DataModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedClasses.Providers;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace AiStudio4.AiServices
{
    internal class Ollama : AiServiceBase
    {
        public Ollama()
        {
        }

        protected override async Task<AiResponse> FetchResponseInternal(AiRequestOptions options)
        {
            InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings);
            
            // Apply custom system prompt if provided
            if (!string.IsNullOrEmpty(options.CustomSystemPrompt))
            {
                options.Conv.systemprompt = options.CustomSystemPrompt;
            }

            var requestPayload = CreateRequestPayload(ApiModel, options.Conv, options.UseStreaming, options.ApiSettings);

            // Build the prompt from the conv
            var promptBuilder = new StringBuilder();

            // Add system prompt if present
            if (!string.IsNullOrEmpty(options.Conv.systemprompt))
            {
                promptBuilder.AppendLine(options.Conv.SystemPromptWithDateTime());
                promptBuilder.AppendLine();
            }

            // Add conv messages
            foreach (var message in options.Conv.messages)
            {
                promptBuilder.AppendLine($"{message.role}: {message.content}");
                promptBuilder.AppendLine();
            }

            requestPayload["prompt"] = promptBuilder.ToString().TrimEnd();

            // Handle images if present
            if (!string.IsNullOrEmpty(options.Base64Image))
            {
                var images = new JArray { options.Base64Image };
                requestPayload["images"] = images;
            }

            if (options.ToolIds?.Any() == true)
            {
                AddToolsToRequest(requestPayload, options.ToolIds);
            }

            if (options.AddEmbeddings)
            {
                var lastMessage = options.Conv.messages.Last().content;
                var newInput = await AddEmbeddingsIfRequired(options.Conv, options.ApiSettings, options.MustNotUseEmbedding, options.AddEmbeddings, lastMessage);
                requestPayload["prompt"] = newInput;
            }

            var json = JsonConvert.SerializeObject(requestPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            return await HandleResponse(content, options.UseStreaming, options.CancellationToken);
        }

        protected override JObject CreateRequestPayload(
            string modelName,
            LinearConv conv,
            bool useStreaming,
            ApiSettings apiSettings)
        {
            return new JObject
            {
                ["model"] = modelName,
                ["stream"] = useStreaming,
                ["options"] = new JObject
                {
                    ["temperature"] = apiSettings.Temperature,
                    ["num_predict"] = 4096,
                }
            };
        }

        protected override async Task<AiResponse> HandleStreamingResponse(
            HttpContent content,
            CancellationToken cancellationToken)
        {
            using var response = await SendRequest(content, cancellationToken, true);
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
            HttpContent content,
            CancellationToken cancellationToken)
        {
            var response = await SendRequest(content, cancellationToken);
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