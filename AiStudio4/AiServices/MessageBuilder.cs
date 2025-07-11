using AiStudio4.Convs;
using AiStudio4.DataModels;
using Newtonsoft.Json.Linq;

namespace AiStudio4.AiServices
{
    public enum ProviderFormat
    {
        Claude,
        Gemini,
        OpenAI
    }

    public static class MessageBuilder
    {
        public static JObject CreateMessage(LinearConvMessage message, ProviderFormat format)
        {
            return format switch
            {
                ProviderFormat.Claude => CreateClaudeMessage(message),
                ProviderFormat.Gemini => CreateGeminiMessage(message),
                ProviderFormat.OpenAI => CreateOpenAIMessage(message),
                _ => throw new ArgumentException($"Unsupported provider format: {format}")
            };
        }

        public static JArray CreateMessagesArray(LinearConv conv, ProviderFormat format)
        {
            var messagesArray = new JArray();
            foreach (var message in conv.messages)
            {
                var messageObj = CreateMessage(message, format);
                messagesArray.Add(messageObj);
            }
            return messagesArray;
        }

        public static JArray CreateAttachmentParts(List<Attachment> attachments, ProviderFormat format)
        {
            var parts = new JArray();
            if (attachments == null || !attachments.Any())
                return parts;

            foreach (var attachment in attachments)
            {
                if (attachment.Type.StartsWith("image/") || attachment.Type == "application/pdf")
                {
                    parts.Add(CreateAttachmentPart(attachment, format));
                }
            }
            return parts;
        }

        public static JObject CreateToolCallPart(string toolName, string args, ProviderFormat format)
        {
            return format switch
            {
                ProviderFormat.Claude => new JObject
                {
                    ["type"] = "tool_use",
                    ["id"] = $"tool_{Guid.NewGuid():N}"[..15],
                    ["name"] = toolName,
                    ["input"] = JObject.Parse(args)
                },
                ProviderFormat.Gemini => new JObject
                {
                    ["functionCall"] = new JObject
                    {
                        ["name"] = toolName,
                        ["args"] = JObject.Parse(args)
                    }
                },
                ProviderFormat.OpenAI => new JObject
                {
                    ["type"] = "function",
                    ["function"] = new JObject
                    {
                        ["name"] = toolName,
                        ["arguments"] = args
                    }
                },
                _ => throw new ArgumentException($"Unsupported provider format: {format}")
            };
        }

        public static JObject CreateToolResultPart(string toolName, string result, bool success, ProviderFormat format)
        {
            return format switch
            {
                ProviderFormat.Claude => new JObject
                {
                    ["type"] = "tool_result",
                    ["tool_use_id"] = $"tool_{Guid.NewGuid():N}"[..15],
                    ["content"] = result,
                    ["is_error"] = !success
                },
                ProviderFormat.Gemini => new JObject
                {
                    ["functionResponse"] = new JObject
                    {
                        ["name"] = toolName,
                        ["response"] = new JObject
                        {
                            ["content"] = result,
                            ["success"] = success
                        }
                    }
                },
                ProviderFormat.OpenAI => new JObject
                {
                    ["type"] = "tool_result",
                    ["tool_call_id"] = $"tool_{Guid.NewGuid():N}"[..15],
                    ["content"] = result
                },
                _ => throw new ArgumentException($"Unsupported provider format: {format}")
            };
        }

        public static JObject CreateUserInterjectionMessage(string interjectionText, ProviderFormat format)
        {
            return format switch
            {
                ProviderFormat.Claude => new JObject
                {
                    ["role"] = "user",
                    ["content"] = interjectionText
                },
                ProviderFormat.Gemini => new JObject
                {
                    ["role"] = "user",
                    ["parts"] = new JArray { new JObject { ["text"] = interjectionText } }
                },
                ProviderFormat.OpenAI => new JObject
                {
                    ["role"] = "user",
                    ["content"] = interjectionText
                },
                _ => throw new ArgumentException($"Unsupported provider format: {format}")
            };
        }

        private static JObject CreateClaudeMessage(LinearConvMessage message)
        {
            var contentArray = new JArray();

            // Handle legacy single image
            if (!string.IsNullOrEmpty(message.base64image))
            {
                contentArray.Add(new JObject
                {
                    ["type"] = "image",
                    ["source"] = new JObject
                    {
                        ["type"] = "base64",
                        ["media_type"] = message.base64type,
                        ["data"] = message.base64image
                    }
                });
            }

            // Handle multiple attachments
            if (message.attachments != null && message.attachments.Any())
            {
                foreach (var attachment in message.attachments)
                {
                    if (attachment.Type.StartsWith("image/") || attachment.Type == "application/pdf")
                    {
                        contentArray.Add(new JObject
                        {
                            ["type"] = attachment.Type == "application/pdf" ? "document" : "image",
                            ["source"] = new JObject
                            {
                                ["type"] = "base64",
                                ["media_type"] = attachment.Type,
                                ["data"] = attachment.Content
                            }
                        });
                    }
                }
            }

            // Handle ContentBlocks
            foreach (var block in message.contentBlocks ?? new List<ContentBlock>())
            {
                if (block.ContentType == ContentType.Text)
                {
                    contentArray.Add(new JObject
                    {
                        ["type"] = "text",
                        ["text"] = (block.Content ?? "").Replace("\r", "")
                    });
                }
                else
                {
                    // For structured content (tool calls/responses), use the properly formatted data directly
                    // AI providers have already created the correct structure with proper tool_use_ids
                    try
                    {
                        var structuredContent = JToken.Parse(block.Content ?? "{}");
                        System.Diagnostics.Debug.WriteLine($"🔧 MESSAGEBUILDER CLAUDE: Processing ContentBlock type={block.ContentType}, content preview: {block.Content?.Substring(0, Math.Min(200, block.Content.Length))}...");
                        
                        // Ensure we're adding individual content items, not nested arrays
                        if (structuredContent is JArray structuredArray)
                        {
                            foreach (var item in structuredArray)
                            {
                                if (item is JObject itemObj)
                                {
                                    var itemType = itemObj["type"]?.ToString();
                                    var toolUseId = itemObj["tool_use_id"]?.ToString() ?? itemObj["id"]?.ToString();
                                    System.Diagnostics.Debug.WriteLine($"🔧 MESSAGEBUILDER CLAUDE: Adding array item type={itemType}, tool_use_id={toolUseId}");
                                }
                                contentArray.Add(item);
                            }
                        }
                        else
                        {
                            if (structuredContent is JObject structuredObj)
                            {
                                var itemType = structuredObj["type"]?.ToString();
                                var toolUseId = structuredObj["tool_use_id"]?.ToString() ?? structuredObj["id"]?.ToString();
                                System.Diagnostics.Debug.WriteLine($"🔧 MESSAGEBUILDER CLAUDE: Adding single item type={itemType}, tool_use_id={toolUseId}");
                            }
                            contentArray.Add(structuredContent);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"🔧 MESSAGEBUILDER CLAUDE: Parse error: {ex.Message}");
                        // If parsing fails, treat as text
                        contentArray.Add(new JObject
                        {
                            ["type"] = "text",
                            ["text"] = block.Content ?? ""
                        });
                    }
                }
            }

            return new JObject
            {
                ["role"] = message.role,
                ["content"] = contentArray
            };
        }

        private static JObject CreateGeminiMessage(LinearConvMessage message)
        {
            var partArray = new JArray();

            // Handle ContentBlocks for Gemini format
            foreach (var block in message.contentBlocks ?? new List<ContentBlock>())
            {
                if (block.ContentType == ContentType.Text)
                {
                    partArray.Add(new JObject { ["text"] = block.Content ?? "" });
                }
                else
                {
                    // For structured content, use the properly formatted data directly
                    // AI providers have already created the correct structure with proper IDs
                    try
                    {
                        var structuredContent = JToken.Parse(block.Content ?? "{}");
                        // Ensure we're adding individual content items, not nested arrays
                        if (structuredContent is JArray structuredArray)
                        {
                            foreach (var item in structuredArray)
                            {
                                partArray.Add(item);
                            }
                        }
                        else
                        {
                            partArray.Add(structuredContent);
                        }
                    }
                    catch
                    {
                        // If parsing fails, treat as text
                        partArray.Add(new JObject { ["text"] = block.Content ?? "" });
                    }
                }
            }

            // Add legacy single image if present
            if (!string.IsNullOrEmpty(message.base64image))
            {
                partArray.Add(new JObject
                {
                    ["inline_data"] = new JObject
                    {
                        ["mime_type"] = message.base64type,
                        ["data"] = message.base64image
                    }
                });
            }

            // Add multiple attachments if present
            if (message.attachments != null && message.attachments.Any())
            {
                foreach (var attachment in message.attachments)
                {
                    if (attachment.Type.StartsWith("image/") || attachment.Type == "application/pdf")
                    {
                        partArray.Add(new JObject
                        {
                            ["inline_data"] = new JObject
                            {
                                ["mime_type"] = attachment.Type,
                                ["data"] = attachment.Content
                            }
                        });
                    }
                }
            }

            return new JObject
            {
                ["role"] = message.role == "assistant" ? "model" : message.role,
                ["parts"] = partArray
            };
        }

        private static JObject CreateOpenAIMessage(LinearConvMessage message)
        {
            var contentArray = new JArray();

            // Add text content from ContentBlocks
            foreach (var block in message.contentBlocks ?? new List<ContentBlock>())
            {
                if (block.ContentType == ContentType.Text)
                {
                    contentArray.Add(new JObject
                    {
                        ["type"] = "text",
                        ["text"] = block.Content ?? ""
                    });
                }
            }

            // Handle legacy single image
            if (!string.IsNullOrEmpty(message.base64image))
            {
                contentArray.Add(new JObject
                {
                    ["type"] = "image_url",
                    ["image_url"] = new JObject
                    {
                        ["url"] = $"data:{message.base64type};base64,{message.base64image}"
                    }
                });
            }

            // Handle multiple attachments
            if (message.attachments != null && message.attachments.Any())
            {
                foreach (var attachment in message.attachments)
                {
                    if (attachment.Type.StartsWith("image/"))
                    {
                        contentArray.Add(new JObject
                        {
                            ["type"] = "image_url",
                            ["image_url"] = new JObject
                            {
                                ["url"] = $"data:{attachment.Type};base64,{attachment.Content}"
                            }
                        });
                    }
                }
            }

            return new JObject
            {
                ["role"] = message.role,
                ["content"] = contentArray
            };
        }

        private static JObject CreateAttachmentPart(Attachment attachment, ProviderFormat format)
        {
            return format switch
            {
                ProviderFormat.Claude => new JObject
                {
                    ["type"] = attachment.Type == "application/pdf" ? "document" : "image",
                    ["source"] = new JObject
                    {
                        ["type"] = "base64",
                        ["media_type"] = attachment.Type,
                        ["data"] = attachment.Content
                    }
                },
                ProviderFormat.Gemini => new JObject
                {
                    ["inline_data"] = new JObject
                    {
                        ["mime_type"] = attachment.Type,
                        ["data"] = attachment.Content
                    }
                },
                ProviderFormat.OpenAI => new JObject
                {
                    ["type"] = "image_url",
                    ["image_url"] = new JObject
                    {
                        ["url"] = $"data:{attachment.Type};base64,{attachment.Content}"
                    }
                },
                _ => throw new ArgumentException($"Unsupported provider format: {format}")
            };
        }
    }
}