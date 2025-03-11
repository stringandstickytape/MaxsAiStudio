using AiStudio4.Convs;
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Tools;
using AiStudio4.DataModels;
using AiStudio4.Services.Interfaces;
using Newtonsoft.Json.Linq;
using SharedClasses.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AiStudio4.AiServices
{
    public abstract class AiServiceBase : IAiService
    {
        public IToolService ToolService { get; set; }
        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;

        public string ChosenTool { get; set; } = null;

        protected HttpClient client = new HttpClient();
        protected bool clientInitialised = false;

        public string ApiKey { get; set; }
        public string ApiUrl { get; set; }
        public string AdditionalParams { get; set; }
        public string ApiModel { get; set; }

        protected virtual void InitializeHttpClient(ServiceProvider serviceProvider,
            Model model, ApiSettings apiSettings, int timeout = 100)
        {
            ApiKey = serviceProvider.ApiKey;
            ApiModel = model.ModelName;
            ApiUrl = serviceProvider.Url;
            AdditionalParams = model.AdditionalParams ?? "";


            if (clientInitialised) return;
            ConfigureHttpClientHeaders(apiSettings);

            client.Timeout = TimeSpan.FromSeconds(timeout);

            clientInitialised = true;
        }

        protected virtual void ConfigureHttpClientHeaders(ApiSettings apiSettings)
        {
            if (!string.IsNullOrEmpty(ApiKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
            }
        }

        // New implementation using the options pattern
        public virtual async Task<AiResponse> FetchResponse(AiRequestOptions options)
        {
            // Default implementation that calls the legacy method to be overridden by derived classes
            return await FetchResponseInternal(options);
        }

        // Legacy method for backward compatibility
        public Task<AiResponse> FetchResponse(
            ServiceProvider serviceProvider,
            Model model,
            LinearConv conv,
            string base64image,
            string base64ImageType,
            CancellationToken cancellationToken,
            ApiSettings apiSettings,
            bool mustNotUseEmbedding,
            List<string> toolIDs,
            bool useStreaming = false,
            bool addEmbeddings = false,
            string customSystemPrompt = null)
        {
            // Convert to options and call the new method
            var options = AiRequestOptions.Create(
                serviceProvider, model, conv, base64image, base64ImageType,
                cancellationToken, apiSettings, mustNotUseEmbedding, toolIDs,
                useStreaming, addEmbeddings, customSystemPrompt);
            
            return FetchResponse(options);
        }
        
        // Internal method to be implemented by derived classes
        protected abstract Task<AiResponse> FetchResponseInternal(AiRequestOptions options);

        protected virtual async Task<string> AddEmbeddingsIfRequired(
            LinearConv conv,
            ApiSettings apiSettings,
            bool mustNotUseEmbedding,
            bool addEmbeddings,
            string content)
        {
            //if (!addEmbeddings) 
            return content;
            //return await OllamaEmbeddingsHelper.AddEmbeddingsToInput(
            //    conv,
            //    apiSettings,
            //    content,
            //    mustNotUseEmbedding
            //);
        }

        protected virtual JObject CreateRequestPayload(
            string modelName,
            LinearConv conv,
            bool useStreaming,
            ApiSettings apiSettings)
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

        protected virtual JObject CreateMessageObject(LinearConvMessage message)
        {
            // Override in derived classes to implement specific message format
            return new JObject();
        }

        protected virtual void AddToolsToRequest(JObject request, List<string> toolIDs)
        {
            if (toolIDs?.Any() != true) return;
            var toolRequestBuilder = new ToolRequestBuilder(ToolService);
            
            // Add each tool to the request
            foreach (var toolID in toolIDs)
            {
                toolRequestBuilder.AddToolToRequest(request, toolID, GetToolFormat());
            }
        }

        protected virtual ToolFormat GetToolFormat()
        {
            return ToolFormat.OpenAI; // Default format
        }
    }
}