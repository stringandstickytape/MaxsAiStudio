﻿using AiStudio4.AiServices;
using AiStudio4.Conversations;
using AiStudio4.Core.Interfaces;
using AiStudio4.DataModels;
using SharedClasses.Providers;

namespace AiStudio4.Services.Interfaces
{
    public interface IAiService
    {
        // New method that uses the options pattern
        Task<AiResponse> FetchResponse(AiRequestOptions options);
        
        // Legacy method for backward compatibility - to be deprecated
        Task<AiResponse> FetchResponse(ServiceProvider serviceProvider,
            Model model, LinearConversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, ApiSettings apiSettings, bool mustNotUseEmbedding, List<string> toolIds, bool useStreaming = false, bool addEmbeddings = false, string customSystemPrompt = null);

        IToolService ToolService { get; set; }
        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;
        public string ChosenTool { get; set; }
    }
}