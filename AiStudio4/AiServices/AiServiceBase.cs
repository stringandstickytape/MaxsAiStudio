using AiStudio4.Convs;
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
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
        public IMcpService McpService { get; set; }
        
        
        

        public string ChosenTool { get; set; } = null;

        public ToolResponse ToolResponseSet { get; set; } = null;

        protected HttpClient client = new HttpClient();
        protected bool clientInitialised = false;

        public string ApiKey { get; set; }
        public string ApiUrl { get; set; }
        public string AdditionalParams { get; set; }
        public string ApiModel { get; set; }

        private bool _isInitialized { get; set; } = false;

        protected virtual void InitializeHttpClient(ServiceProvider serviceProvider,
            Model model, ApiSettings apiSettings, int timeout = 300)
        {
            if (!clientInitialised)
            {
                
                ApiKey = serviceProvider.ApiKey;
                ApiModel = model.ModelName;
                ApiUrl = serviceProvider.Url;
                AdditionalParams = model.AdditionalParams ?? "";

                if (clientInitialised && client.DefaultRequestHeaders.Authorization?.Parameter == ApiKey && client.DefaultRequestHeaders.Authorization?.Scheme == "Bearer")
                {
                    
                    
                    return;
                }

                ConfigureHttpClientHeaders(apiSettings); 

                client.Timeout = TimeSpan.FromSeconds(timeout);
                clientInitialised = true;
            }
        }

        protected virtual void ConfigureHttpClientHeaders(ApiSettings apiSettings)
        {
            if (!string.IsNullOrEmpty(ApiKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
            }
        }

        
        public virtual async Task<AiResponse> FetchResponse(AiRequestOptions options, bool forceNoTools = false)
        {
            
            return await FetchResponseInternal(options, forceNoTools);
        }

        
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
            bool addEmbeddings = false,
            string customSystemPrompt = null)
        {
            
            var options = AiRequestOptions.Create(
                serviceProvider, model, conv, base64image, base64ImageType,
                cancellationToken, apiSettings, mustNotUseEmbedding, toolIDs,
                addEmbeddings, customSystemPrompt);
            
            
            options.OnStreamingUpdate = null;
            options.OnStreamingComplete = null;
            
            return FetchResponse(options);
        }
        
        
        protected abstract Task<AiResponse> FetchResponseInternal(AiRequestOptions options, bool forceNoTools = false);

        protected virtual async Task<string> AddEmbeddingsIfRequired(
            LinearConv conv,
            ApiSettings apiSettings,
            bool mustNotUseEmbedding,
            bool addEmbeddings,
            string content)
        {
            
            return content;
            
            
            
            
            
            
        }
        
        protected virtual JArray CreateAttachmentsArray(List<Attachment> attachments)
        {
            var result = new JArray();
            
            if (attachments == null || !attachments.Any())
                return result;
                
            foreach (var attachment in attachments)
            {
                if (attachment.Type.StartsWith("image/") || attachment.Type == "application/pdf")
                {
                    result.Add(new JObject
                    {
                        ["type"] = "image_url",
                        ["image_url"] = new JObject
                        {
                            ["url"] = $"data:{attachment.Type};base64,{attachment.Content}"
                        }
                    });
                }
                
            }
            
            return result;
        }

        protected abstract JObject CreateRequestPayload(string modelName, LinearConv conv, ApiSettings apiSettings);

        protected virtual async Task<AiResponse> HandleResponse(
            AiRequestOptions options,
            HttpContent content)
        {
            return await HandleStreamingResponse(content, options.CancellationToken, options.OnStreamingUpdate, options.OnStreamingComplete);
        }

        protected abstract Task<AiResponse> HandleStreamingResponse(
            HttpContent content,
            CancellationToken cancellationToken,
            Action<string> onStreamingUpdate, 
            Action onStreamingComplete);

        protected virtual async Task<HttpResponseMessage> SendRequest(
            HttpContent content,
            CancellationToken cancellationToken)
        {
            var sendOption = HttpCompletionOption.ResponseHeadersRead;
            var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
            {
                Content = content
            };

            var response = await client.SendAsync(request, sendOption, cancellationToken);
            response.EnsureSuccessStatusCode();
            return response;
        }

        protected virtual TokenUsage ExtractTokenUsage(JObject response)
        {
            return new TokenUsage("0", "0");  
        }

        protected virtual void ValidateResponse(HttpResponseMessage response)
        {
            
        }

        protected virtual async Task<string> ExtractResponseText(JObject response)
        {
            return await Task.FromResult(string.Empty);  
        }

        protected virtual async Task ProcessStreamingData(
            Stream stream,
            StringBuilder responseBuilder,
            CancellationToken cancellationToken)
        {
            
            await Task.CompletedTask;
        }

        protected virtual JObject CreateMessageObject(LinearConvMessage message)
        {
            
            return new JObject();
        }

        protected virtual async Task AddToolsToRequestAsync(JObject request, List<string> toolIDs)
        {
            var toolRequestBuilder = new ToolRequestBuilder(ToolService, McpService);
            
            
            foreach (var toolID in toolIDs)
            {
                await toolRequestBuilder.AddToolToRequestAsync(request, toolID, GetToolFormat());
            }

            await toolRequestBuilder.AddMcpServiceToolsToRequestAsync(request, GetToolFormat());
        }

        protected virtual ToolFormat GetToolFormat()
        {
            return ToolFormat.OpenAI; 
        }
    }
}
