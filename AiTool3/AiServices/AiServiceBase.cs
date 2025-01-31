using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.Embeddings;
using AiTool3.Interfaces;
using AiTool3.Tools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AiTool3.AiServices
{
    public abstract class AiServiceBase : IAiService
    {
        public ToolManager ToolManager { get; set; }
        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;

        protected HttpClient client = new HttpClient();
        protected bool clientInitialised = false;

        public string ApiKey { get; set; }
        public string ApiUrl { get; set; }
        public string AdditionalParams { get; set; }
        public string ApiModel { get; set; }

        protected virtual void InitializeHttpClient(ServiceProvider serviceProvider,
            Model model, SettingsSet currentSettings, int timeout = 100)
        {
            ApiKey = serviceProvider.ApiKey;
            ApiModel = model.ModelName;
            ApiUrl = serviceProvider.Url;
            AdditionalParams = model.AdditionalParams;


            if (clientInitialised) return;
            ConfigureHttpClientHeaders(currentSettings);

            client.Timeout = TimeSpan.FromSeconds(timeout);

            clientInitialised = true;
        }

        protected virtual void ConfigureHttpClientHeaders(SettingsSet currentSettings)
        {
            if (!string.IsNullOrEmpty(ApiKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
            }
        }

        public abstract Task<AiResponse> FetchResponse(
            ServiceProvider serviceProvider,
            Model model,
            Conversation conversation,
            string base64image,
            string base64ImageType,
            CancellationToken cancellationToken,
            SettingsSet currentSettings,
            bool mustNotUseEmbedding,
            List<string> toolIDs,
            bool useStreaming = false,
            bool addEmbeddings = false
        );

        protected virtual async Task<string> AddEmbeddingsIfRequired(
            Conversation conversation,
            SettingsSet currentSettings,
            bool mustNotUseEmbedding,
            bool addEmbeddings,
            string content)
        {
            if (!addEmbeddings) return content;
            return await OllamaEmbeddingsHelper.AddEmbeddingsToInput(
                conversation,
                currentSettings,
                content,
                mustNotUseEmbedding
            );
        }

        protected virtual JObject CreateRequestPayload(
            string modelName, 
            Conversation conversation,
            bool useStreaming,
            SettingsSet currentSettings)
        {
            return new JObject
            {
                ["model"] = modelName,
                ["stream"] = useStreaming
            };
        }

        protected virtual async Task<AiResponse> HandleResponse(
            HttpContent content,
            bool useStreaming,
            CancellationToken cancellationToken)
        {
            try
            {
                return useStreaming
                    ? await HandleStreamingResponse(content, cancellationToken)
                    : await HandleNonStreamingResponse(content, cancellationToken);
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        protected abstract Task<AiResponse> HandleStreamingResponse(
            HttpContent content,
            CancellationToken cancellationToken);

        protected abstract Task<AiResponse> HandleNonStreamingResponse(
            HttpContent content,
            CancellationToken cancellationToken);

        protected virtual void OnStreamingDataReceived(string data)
        {
            StreamingTextReceived?.Invoke(this, data);
        }

        protected virtual void OnStreamingComplete()
        {
            StreamingComplete?.Invoke(this, null);
        }

        protected virtual AiResponse HandleError(Exception ex, string additionalContext = null)
        {
            var errorMessage = new StringBuilder(ex.Message);
            if (!string.IsNullOrEmpty(additionalContext))
            {
                errorMessage.Append("\nContext: ").Append(additionalContext);
            }

            if (ex is HttpRequestException httpEx)
            {
                errorMessage.Append("\nHTTP Status: ").Append(httpEx.StatusCode);
            }

            return new AiResponse
            {
                Success = false,
                ResponseText = errorMessage.ToString(),
                TokenUsage = new TokenUsage("0", "0")
            };
        }

        protected virtual async Task<HttpResponseMessage> SendRequest(
            HttpContent content,
            CancellationToken cancellationToken,
            bool streamingRequest = false)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
            {
                Content = content
            };

            var sendOption = streamingRequest
                ? HttpCompletionOption.ResponseHeadersRead
                : HttpCompletionOption.ResponseContentRead;

            return await client.SendAsync(request, sendOption, cancellationToken);
        }

        protected virtual TokenUsage ExtractTokenUsage(JObject response)
        {
            return new TokenUsage("0", "0");  // Override in derived classes
        }

        protected virtual void ValidateResponse(HttpResponseMessage response)
        {
            //response.EnsureSuccessStatusCode();
        }

        protected virtual async Task<string> ExtractResponseText(JObject response)
        {
            return await Task.FromResult(string.Empty);  // Override in derived classes
        }

        protected virtual async Task ProcessStreamingData(
            Stream stream,
            StringBuilder responseBuilder,
            CancellationToken cancellationToken)
        {
            // Override in derived classes to implement specific streaming logic
            await Task.CompletedTask;
        }

        protected virtual JObject CreateMessageObject(ConversationMessage message)
        {
            // Override in derived classes to implement specific message format
            return new JObject();
        }

        protected virtual void AddToolsToRequest(JObject request, List<string> toolIDs)
        {
            if (toolIDs?.Any() != true) return;
            var toolRequestBuilder = new ToolRequestBuilder(ToolManager);
            toolRequestBuilder.AddToolToRequest(request, toolIDs[0], GetToolFormat());
        }

        protected virtual ToolFormat GetToolFormat()
        {
            return ToolFormat.OpenAI; // Default format
        }
    }
}
