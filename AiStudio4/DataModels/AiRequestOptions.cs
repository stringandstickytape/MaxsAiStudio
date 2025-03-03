using AiStudio4.Conversations;
using SharedClasses.Providers;
using System;
using System.Collections.Generic;

namespace AiStudio4.DataModels
{
    public class AiRequestOptions
    {
        // Service and model information
        public ServiceProvider ServiceProvider { get; set; }
        public Model Model { get; set; }
        
        // Conversation content
        public LinearConversation Conversation { get; set; }
        
        // Cancellation support
        public CancellationToken CancellationToken { get; set; } = new CancellationToken(false);
        
        // API settings
        public ApiSettings ApiSettings { get; set; }
        
        // Image support
        public string Base64Image { get; set; }
        public string Base64ImageType { get; set; }
        
        // Tools support
        public List<string> ToolIds { get; set; } = new List<string>();
        
        // Behavior flags
        public bool UseStreaming { get; set; } = true;
        public bool AddEmbeddings { get; set; } = false;
        public bool MustNotUseEmbedding { get; set; } = true;
        
        // Custom system prompt override
        public string CustomSystemPrompt { get; set; }

        // Factory method to create from the old parameter list for backward compatibility
        public static AiRequestOptions Create(
            ServiceProvider serviceProvider,
            Model model,
            LinearConversation conversation,
            string base64image,
            string base64ImageType,
            CancellationToken cancellationToken,
            ApiSettings apiSettings,
            bool mustNotUseEmbedding,
            List<string> toolIds,
            bool useStreaming = false,
            bool addEmbeddings = false,
            string customSystemPrompt = null)
        {
            return new AiRequestOptions
            {
                ServiceProvider = serviceProvider,
                Model = model,
                Conversation = conversation,
                Base64Image = base64image,
                Base64ImageType = base64ImageType,
                CancellationToken = cancellationToken,
                ApiSettings = apiSettings,
                MustNotUseEmbedding = mustNotUseEmbedding,
                ToolIds = toolIds ?? new List<string>(),
                UseStreaming = useStreaming,
                AddEmbeddings = addEmbeddings,
                CustomSystemPrompt = customSystemPrompt
            };
        }
    }
}