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

        public virtual LinearConvMessage CreateToolResultMessage(List<ContentBlock> toolResultBlocks)
        {
            var toolResults = new JArray();

            foreach (var block in toolResultBlocks)
            {
                if (block.ContentType == ContentType.ToolResponse)
                {
                    var toolData = JsonConvert.DeserializeObject<dynamic>(block.Content);
                    var toolName = toolData.toolName?.ToString();
                    var result = toolData.result?.ToString();
                    var success = (bool)(toolData.success ?? false);

                    toolResults.Add(MessageBuilder.CreateToolResultPart(toolName, result, success, _format));
                }
            }

            return new LinearConvMessage
            {
                role = "user",
                contentBlocks = new List<ContentBlock>
                {
                    new ContentBlock
                    {
                        ContentType = ContentType.ToolResponse,
                        Content = toolResults.ToString()
                    }
                }
            };
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