using AiStudio4.Convs;
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
        
        // Conv content
        public LinearConv Conv { get; set; }
        
        // Cancellation support
        public CancellationToken CancellationToken { get; set; } = new CancellationToken(false);
        
        // API settings
        public ApiSettings ApiSettings { get; set; }
        
        // Attachment support
        public string Base64Image { get; set; } // Kept for backward compatibility
        public string Base64ImageType { get; set; } // Kept for backward compatibility
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
        
        // Tools support
        public List<string> ToolIds { get; set; } = new List<string>();
        
        // Behavior flags

        public bool AddEmbeddings { get; set; } = false;
        public bool MustNotUseEmbedding { get; set; } = true;
        
        // Custom system prompt override
        public string CustomSystemPrompt { get; set; }

        // Callbacks for streaming updates
        public Action<string> OnStreamingUpdate { get; set; }
        public Action OnStreamingComplete { get; set; }

        // Factory method to create from the old parameter list for backward compatibility
        public static AiRequestOptions Create(
            ServiceProvider serviceProvider,
            Model model,
            LinearConv conv,
            string base64image,
            string base64ImageType,
            CancellationToken cancellationToken,
            ApiSettings apiSettings,
            bool mustNotUseEmbedding,
            List<string> toolIds,
            bool addEmbeddings = false,
            string customSystemPrompt = null,
            List<Attachment> attachments = null)
        {
            return new AiRequestOptions
            {
                ServiceProvider = serviceProvider,
                Model = model,
                Conv = conv,
                Base64Image = base64image,
                Base64ImageType = base64ImageType,
                CancellationToken = cancellationToken,
                ApiSettings = apiSettings,
                MustNotUseEmbedding = mustNotUseEmbedding,
                ToolIds = toolIds ?? new List<string>(),

                AddEmbeddings = addEmbeddings,
                CustomSystemPrompt = customSystemPrompt,
                Attachments = attachments ?? new List<Attachment>(),
                // Initialize callbacks to null for backward compatibility
                OnStreamingUpdate = null,
                OnStreamingComplete = null
            };
        }
    }
}