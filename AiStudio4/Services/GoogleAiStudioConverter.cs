// AiStudio4/Services/GoogleAiStudioConverter.cs
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AiStudio4.Services
{
    public static class GoogleAiStudioConverter
    {
        // Nested helper classes for Google AI Studio JSON Deserialization
        private class GoogleRunSettings
        {
            public float temperature { get; set; }
            // Potentially other settings
        }

        private class GoogleChunk
        {
            public string text { get; set; }
            public string role { get; set; }
            public bool? isThought { get; set; }
        }

        private class GoogleChunkedPrompt
        {
            public List<GoogleChunk> chunks { get; set; }
        }

        private class GoogleSystemInstruction
        {
            // Define if system instructions are to be processed
        }

        private class GoogleRootObject
        {
            public GoogleRunSettings runSettings { get; set; }
            public GoogleChunkedPrompt chunkedPrompt { get; set; }
            public GoogleSystemInstruction systemInstruction { get; set; }
        }

        public static v4BranchedConv ConvertToAiStudio4(string googleJsonContent, string originalFileName)
        {
            try
            {
                // 1. Deserialize googleJsonContent to GoogleRootObject
                var googleData = JsonConvert.DeserializeObject<GoogleRootObject>(googleJsonContent);
                if (googleData?.chunkedPrompt?.chunks == null)
                {
                    throw new InvalidOperationException("Invalid Google AI Studio JSON format: missing chunkedPrompt.chunks");
                }

                // 2. Create a new v4BranchedConv instance
                var aiStudioConv = new v4BranchedConv
                {
                    ConvId = $"imported_gai_{Guid.NewGuid()}",
                    Summary = Path.GetFileNameWithoutExtension(originalFileName) ?? "Imported Conversation",
                    SystemPromptId = null // Null/empty for now
                };

                // 3. Add an initial "System" root message to anchor the conversation
                var systemRootMsg = new v4BranchedConvMessage
                {
                    Id = $"msg_sysroot_{Guid.NewGuid()}",
                    Role = v4BranchedConvMessageRole.System,
                    UserMessage = $"Imported from Google AI Studio: '{originalFileName}' on {DateTime.UtcNow:g}",
                    Timestamp = DateTime.UtcNow,
                    ParentId = null
                };
                aiStudioConv.Messages.Add(systemRootMsg);
                string currentParentId = systemRootMsg.Id;

                // 4. Initialize a StringBuilder for pending thoughts
                var pendingThoughts = new StringBuilder();

                // 5. Iterate through googleData.chunkedPrompt.chunks
                foreach (var chunk in googleData.chunkedPrompt.chunks)
                {
                    if (chunk.isThought == true)
                    {
                        // If chunk.isThought == true: accumulate thoughts
                        if (pendingThoughts.Length > 0)
                        {
                            pendingThoughts.Append("\n");
                        }
                        pendingThoughts.Append(chunk.text);
                    }
                    else
                    {
                        // It's a "user" or "model" chunk
                        // Determine AiStudio4 role: User for "user", Assistant for "model"
                        v4BranchedConvMessageRole role;
                        switch (chunk.role?.ToLower())
                        {
                            case "user":
                                role = v4BranchedConvMessageRole.User;
                                break;
                            case "model":
                                role = v4BranchedConvMessageRole.Assistant;
                                break;
                            default:
                                // Skip unknown roles or log warning
                                continue;
                        }

                        // Prepare message content
                        string finalContent = chunk.text ?? string.Empty;

                        // If current chunk role is "model" and pendingThoughts.Length > 0:
                        if (role == v4BranchedConvMessageRole.Assistant && pendingThoughts.Length > 0)
                        {
                            finalContent = $"<Thought>\n{pendingThoughts.ToString().Trim()}\n</Thought>\n\n{finalContent}";
                            pendingThoughts.Clear();
                        }

                        // Create new v4BranchedConvMessage
                        var newMessage = new v4BranchedConvMessage
                        {
                            Id = $"msg_{Guid.NewGuid()}",
                            ParentId = currentParentId,
                            Role = role,
                            UserMessage = finalContent,
                            Timestamp = DateTime.UtcNow,
                            Temperature = role == v4BranchedConvMessageRole.Assistant ? googleData.runSettings?.temperature : null
                        };

                        // Add this message to our flat list
                        aiStudioConv.Messages.Add(newMessage);

                        // Update currentParentId to this message's Id
                        currentParentId = newMessage.Id;
                    }
                }

                // 6. Handle any remaining thoughts
                if (pendingThoughts.Length > 0)
                {
                    var lastAssistantMsg = aiStudioConv.Messages.LastOrDefault(m => m.Role == v4BranchedConvMessageRole.Assistant);
                    if (lastAssistantMsg != null)
                    {
                        lastAssistantMsg.UserMessage += $"\n\n<Thought>\n{pendingThoughts.ToString().Trim()}\n</Thought>";
                    }
                    else
                    {
                        // Create a new Assistant message just for these thoughts
                        var thoughtsMessage = new v4BranchedConvMessage
                        {
                            Id = $"msg_{Guid.NewGuid()}",
                            ParentId = currentParentId,
                            Role = v4BranchedConvMessageRole.Assistant,
                            UserMessage = $"<Thought>\n{pendingThoughts.ToString().Trim()}\n</Thought>",
                            Timestamp = DateTime.UtcNow
                        };
                        aiStudioConv.Messages.Add(thoughtsMessage);
                    }
                }

                // 7. Return the populated aiStudioConv
                return aiStudioConv;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse Google AI Studio JSON: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to convert Google AI Studio format: {ex.Message}", ex);
            }
        }
    }
}