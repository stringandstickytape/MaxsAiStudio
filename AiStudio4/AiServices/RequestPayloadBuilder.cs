using AiStudio4.Convs;
using AiStudio4.DataModels;
using Newtonsoft.Json.Linq;
using SharedClasses.Providers;
using System.Linq;

namespace AiStudio4.AiServices
{
    public class RequestPayloadBuilder
    {
        private readonly ProviderFormat _format;
        private JObject _payload;
        private string _model;
        private LinearConv _conv;
        private ApiSettings _apiSettings;

        private RequestPayloadBuilder(ProviderFormat format)
        {
            _format = format;
            _payload = new JObject();
        }

        public static RequestPayloadBuilder Create(ProviderFormat format)
        {
            return new RequestPayloadBuilder(format);
        }

        public RequestPayloadBuilder WithModel(string model)
        {
            _model = model;
            return this;
        }

        public RequestPayloadBuilder WithConversation(LinearConv conv)
        {
            _conv = conv;
            return this;
        }

        public RequestPayloadBuilder WithApiSettings(ApiSettings apiSettings)
        {
            _apiSettings = apiSettings;
            return this;
        }

        public RequestPayloadBuilder WithSystemPrompt(string systemPrompt)
        {
            if (string.IsNullOrWhiteSpace(systemPrompt))
                return this;

            switch (_format)
            {
                case ProviderFormat.Claude:
                    _payload["system"] = systemPrompt;
                    break;
                case ProviderFormat.Gemini:
                    // Gemini handles system prompts differently in generation config
                    break;
                case ProviderFormat.OpenAI:
                    // OpenAI includes system message in the messages array
                    break;
            }
            return this;
        }

        public RequestPayloadBuilder WithGenerationConfig()
        {
            if (_apiSettings == null)
                return this;

            switch (_format)
            {
                case ProviderFormat.Claude:
                    ConfigureClaudeGeneration();
                    break;
                case ProviderFormat.Gemini:
                    ConfigureGeminiGeneration();
                    break;
                case ProviderFormat.OpenAI:
                    ConfigureOpenAIGeneration();
                    break;
            }
            return this;
        }

        public RequestPayloadBuilder WithMessages()
        {
            if (_conv?.messages == null)
                return this;

            var messagesArray = MessageBuilder.CreateMessagesArray(_conv, _format);
            
            switch (_format)
            {
                case ProviderFormat.Claude:
                    _payload["messages"] = messagesArray;
                    System.Diagnostics.Debug.WriteLine($"🔧 REQUESTPAYLOADBUILDER: Created Claude messages array with {messagesArray.Count} messages");
                    for (int i = 0; i < messagesArray.Count; i++)
                    {
                        var msg = messagesArray[i];
                        var role = msg["role"]?.ToString();
                        var content = msg["content"] as JArray;
                        System.Diagnostics.Debug.WriteLine($"🔧 REQUESTPAYLOADBUILDER: Message {i}: role={role}, content_blocks={content?.Count}");
                        if (content != null)
                        {
                            for (int j = 0; j < content.Count; j++)
                            {
                                var block = content[j];
                                var type = block["type"]?.ToString();
                                var toolUseId = block["tool_use_id"]?.ToString() ?? block["id"]?.ToString();
                                System.Diagnostics.Debug.WriteLine($"🔧 REQUESTPAYLOADBUILDER: Content {j}: type={type}, tool_use_id={toolUseId}");
                            }
                        }
                    }
                    break;
                case ProviderFormat.Gemini:
                    _payload["contents"] = messagesArray;
                    break;
                case ProviderFormat.OpenAI:
                    _payload["messages"] = messagesArray;
                    break;
            }
            return this;
        }

        public RequestPayloadBuilder WithPromptCaching(bool usePromptCaching)
        {
            if (!usePromptCaching || _conv?.messages == null)
                return this;

            switch (_format)
            {
                case ProviderFormat.Claude:
                    ApplyClaudePromptCaching();
                    break;
                // Other providers may implement caching differently
            }
            return this;
        }

        public RequestPayloadBuilder WithTopP(float? topP)
        {
            if (!topP.HasValue || topP.Value <= 0.0f || topP.Value > 1.0f)
                return this;

            switch (_format)
            {
                case ProviderFormat.Claude:
                    _payload["top_p"] = topP.Value;
                    break;
                case ProviderFormat.Gemini:
                    EnsureGenerationConfig();
                    ((JObject)_payload["generationConfig"])["topP"] = topP.Value;
                    break;
                case ProviderFormat.OpenAI:
                    _payload["top_p"] = topP.Value;
                    break;
            }
            return this;
        }

        public RequestPayloadBuilder WithOneOffPreFill(string prefill)
        {
            if (string.IsNullOrWhiteSpace(prefill))
                return this;

            switch (_format)
            {
                case ProviderFormat.Claude:
                    EnsureMessagesArray();
                    ((JArray)_payload["messages"]).Add(new JObject
                    {
                        ["role"] = "assistant",
                        ["content"] = new JArray { new JObject { ["type"] = "text", ["text"] = prefill.Trim() } }
                    });
                    break;
                // Other providers may handle prefill differently
            }
            return this;
        }

        public JObject Build()
        {
            return _payload;
        }

        private void ConfigureClaudeGeneration()
        {
            _payload["model"] = _model;
            _payload["max_tokens"] = (_model == "claude-3-7-sonnet-20250219" || _model == "claude-3-7-sonnet-latest") ? 64000 : 8192;
            _payload["stream"] = true;
            _payload["temperature"] = _apiSettings.Temperature;
        }

        private void ConfigureGeminiGeneration()
        {
            var isImageGenModel = _model == "gemini-2.0-flash-exp-image-generation";
            
            var generationConfig = new JObject
            {
                ["temperature"] = _apiSettings.Temperature
            };

            if (isImageGenModel)
            {
                generationConfig["responseModalities"] = new JArray { "Text", "Image" };
            }
            else
            {
                // Add system instruction for non-image generation models
                if (_conv != null && !string.IsNullOrWhiteSpace(_conv.systemprompt))
                {
                    _payload["system_instruction"] = new JObject
                    {
                        ["parts"] = new JObject
                        {
                            ["text"] = _conv.SystemPromptWithDateTime()
                        }
                    };
                }
            }

            _payload["generationConfig"] = generationConfig;
        }

        private void ConfigureOpenAIGeneration()
        {
            _payload["model"] = _model;
            _payload["temperature"] = _apiSettings.Temperature;
            _payload["stream"] = true;
        }

        private void ApplyClaudePromptCaching()
        {
            if (!(_payload["messages"] is JArray messages))
                return;

            int userMessageCount = 0;
            int totalUserMessages = messages.Count(m => m["role"]?.ToString() == "user");

            foreach (var message in messages)
            {
                if (message["role"]?.ToString() == "user")
                {
                    userMessageCount++;
                    // Add cache_control to the last 4 user messages
                    if ((totalUserMessages - userMessageCount) < 4)
                    {
                        var content = message["content"] as JArray;
                        if (content != null && content.Count > 0)
                        {
                            // Add cache_control to the last content block (any type)
                            // Content is now guaranteed to be a flat array
                            var lastContentItem = content.Last();
                            if (lastContentItem is JObject contentObj)
                            {
                                contentObj["cache_control"] = new JObject { ["type"] = "ephemeral" };
                            }
                        }
                    }
                }
            }
        }

        private void EnsureMessagesArray()
        {
            if (_payload["messages"] == null)
            {
                _payload["messages"] = new JArray();
            }
        }

        private void EnsureGenerationConfig()
        {
            if (_payload["generationConfig"] == null)
            {
                _payload["generationConfig"] = new JObject();
            }
        }

        public RequestPayloadBuilder WithTools(List<string> ToolIds)
        {
            if (_payload["tools"] == null)
            {
                _payload["tools"] = new JArray();
            }

            foreach (var toolId in ToolIds)
                ((JArray)_payload["tools"]).Add(toolId);

            return this;
        }
    }
}