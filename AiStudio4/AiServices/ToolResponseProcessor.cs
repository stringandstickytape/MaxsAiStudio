using AiStudio4.Convs;
using AiStudio4.Core.Models;
using AiStudio4.DataModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiStudio4.AiServices
{
    public abstract class ToolResponseProcessor
    {
        protected readonly ProviderFormat _format;

        protected ToolResponseProcessor(ProviderFormat format)
        {
            _format = format;
        }

        public virtual LinearConvMessage CreateAssistantMessage(AiResponse response)
        {
            var assistantContent = new JArray();

            // Add any text content first
            var textContent = response.ContentBlocks?.FirstOrDefault(c => c.ContentType == ContentType.Text)?.Content;
            if (!string.IsNullOrEmpty(textContent))
            {
                assistantContent.Add(CreateTextContentPart(textContent));
            }

            // Add tool use blocks
            foreach (var toolCall in response.ToolResponseSet.Tools)
            {
                assistantContent.Add(MessageBuilder.CreateToolCallPart(toolCall.ToolName, toolCall.ResponseText, _format));
            }

            return new LinearConvMessage
            {
                role = GetAssistantRole(),
                contentBlocks = new List<ContentBlock>
                {
                    new ContentBlock
                    {
                        ContentType = ContentType.Text,
                        Content = assistantContent.ToString()
                    }
                }
            };
        }

        public virtual List<LinearConvMessage> CreateToolResultMessage(List<ContentBlock> toolResultBlocks)
        {
            // Use ContentBlocks directly - AI providers have already created properly formatted tool results
            var message = new LinearConvMessage
            {
                role = "user",
                contentBlocks = toolResultBlocks
            };
            return new List<LinearConvMessage> { message };
        }

        public virtual LinearConvMessage CreateUserInterjectionMessage(string interjectionText)
        {
            return new LinearConvMessage
            {
                role = "user",
                contentBlocks = new List<ContentBlock>
                {
                    new ContentBlock
                    {
                        ContentType = ContentType.Text,
                        Content = _format == ProviderFormat.Gemini 
                            ? new JArray { new JObject { ["text"] = interjectionText } }.ToString()
                            : interjectionText
                    }
                }
            };
        }

        protected virtual JObject CreateTextContentPart(string text)
        {
            return _format switch
            {
                ProviderFormat.Claude => new JObject { ["type"] = "text", ["text"] = text },
                ProviderFormat.Gemini => new JObject { ["text"] = text },
                ProviderFormat.OpenAI => new JObject { ["type"] = "text", ["text"] = text },
                _ => throw new ArgumentException($"Unsupported provider format: {_format}")
            };
        }

        protected virtual string GetAssistantRole()
        {
            return _format switch
            {
                ProviderFormat.Gemini => "model",
                _ => "assistant"
            };
        }
    }

    public class ClaudeToolResponseProcessor : ToolResponseProcessor
    {
        public ClaudeToolResponseProcessor() : base(ProviderFormat.Claude) { }
    }

    public class GeminiToolResponseProcessor : ToolResponseProcessor
    {
        public GeminiToolResponseProcessor() : base(ProviderFormat.Gemini) { }
    }

    public class OpenAIToolResponseProcessor : ToolResponseProcessor
    {
        public OpenAIToolResponseProcessor() : base(ProviderFormat.OpenAI) { }
    }
}