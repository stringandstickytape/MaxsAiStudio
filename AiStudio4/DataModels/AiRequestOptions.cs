using AiStudio4.Convs;
using AiStudio4.InjectedDependencies;
using SharedClasses.Providers;



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
        public float? TopP { get; set; } // Added TopP
        
        // Custom system prompt override
        public string CustomSystemPrompt { get; set; }

        /// <summary>
        /// Maximum number of tool execution iterations before stopping the loop.
        /// Default is 10 if not specified.
        /// </summary>
        public int? MaxToolIterations { get; set; }

        /// <summary>
        /// Whether to allow user interjections during tool execution.
        /// Default is true.
        /// </summary>
        public bool AllowInterjections { get; set; } = true;

        // Callbacks for streaming updates
        public Action<string> OnStreamingUpdate { get; set; }
        public Action OnStreamingComplete { get; set; }
        
        // Function to get the current assistant message ID for streaming updates
        public Func<string> GetCurrentAssistantMessageId { get; set; }

        // New callbacks for conversation updates during tool loop
        /// <summary>
        /// Called when AI generates a response that may include tool calls
        /// </summary>
        public Func<v4BranchedConvMessage, Task> OnAssistantMessageCreated { get; set; }
        
        /// <summary>
        /// Called when AI generates tool calls
        /// </summary>
        public Func<string, List<Core.Models.ContentBlock>, List<Core.Models.ToolResponseItem>, Task> OnToolCallsGenerated { get; set; }
        
        /// <summary>
        /// Called after each tool execution completes
        /// Parameters: messageId, toolName, result
        /// </summary>
        public Func<string, string, Core.Models.BuiltinToolResult, Task> OnToolExecuted { get; set; }
        
        /// <summary>
        /// Called when a user interjection occurs during tool execution
        /// </summary>
        public Func<string, string, Task> OnUserInterjection { get; set; }
        
        /// <summary>
        /// Called when a user message (like tool results) is created during tool execution
        /// </summary>
        public Func<v4BranchedConvMessage, Task> OnUserMessageCreated { get; set; }
        
        // Branched conversation context for tool loop
        public v4BranchedConv BranchedConversation { get; set; }
        public string ParentMessageId { get; set; }
        public string AssistantMessageId { get; set; }
        public string ClientId { get; set; }

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
            List<Attachment> attachments = null,
            float? topP = null) // Added topP parameter
        {
            return new AiRequestOptions
            {
                ServiceProvider = serviceProvider,
                Model = model,
                Conv = conv,
                Base64Image = base64image,
                Base64ImageType = base64ImageType,
                CancellationToken = cancellationToken,
                ApiSettings = apiSettings, // This will carry the general TopP from settings
                MustNotUseEmbedding = mustNotUseEmbedding,
                ToolIds = toolIds ?? new List<string>(),

                AddEmbeddings = addEmbeddings,
                CustomSystemPrompt = customSystemPrompt,
                Attachments = attachments ?? new List<Attachment>(),
                TopP = topP, // Set the specific TopP for this request if provided
                // Initialize callbacks to null for backward compatibility
                OnStreamingUpdate = null,
                OnStreamingComplete = null
            };
        }
    }
}
