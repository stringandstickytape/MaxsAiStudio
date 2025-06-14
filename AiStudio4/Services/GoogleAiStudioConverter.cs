// AiStudio4/Services/GoogleAiStudioConverter.cs

using AiStudio4.DataModels;








using SharedClasses;


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

        private class GoogleDriveDocument
        {
            public string id { get; set; }
        }

        private class GoogleChunk
        {
            public string text { get; set; }
            public string role { get; set; }
            public bool? isThought { get; set; }
            public GoogleDriveDocument driveDocument { get; set; }
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

        public static async Task<v4BranchedConv> ConvertToAiStudio4Async(string googleJsonContent, string originalFileName, Core.Interfaces.IGoogleDriveService googleDriveService = null)
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
                    ContentBlocks = new List<ContentBlock>
                    {
                        new ContentBlock
                        {
                            Content = $"Imported from Google AI Studio: '{originalFileName}'  on {DateTime.UtcNow:g}",
                            ContentType = ContentType.Text
                        }
                    },
                    Timestamp = DateTime.UtcNow,
                    ParentId = null
                };
                aiStudioConv.Messages.Add(systemRootMsg);
                string currentParentId = systemRootMsg.Id;

                // 4. Initialize a StringBuilder for pending thoughts
                var pendingThoughts = new StringBuilder();

                // 5. Collect attachment file IDs and process chunks
                var pendingAttachments = new List<string>();
                
                // 6. Iterate through googleData.chunkedPrompt.chunks
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
                    else if (chunk.driveDocument != null && !string.IsNullOrEmpty(chunk.driveDocument.id))
                    {
                        // This is an attachment chunk - collect the file ID
                        pendingAttachments.Add(chunk.driveDocument.id);
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

                        if (finalContent == "") continue;

                        // If current chunk role is "model" and pendingThoughts.Length > 0:
                        if (role == v4BranchedConvMessageRole.Assistant && pendingThoughts.Length > 0)
                        {
                            finalContent = $"\n<Thought>\n{pendingThoughts.ToString().Trim()}\n</Thought>\n\n{finalContent}";
                            pendingThoughts.Clear();
                        }

                        // Create new v4BranchedConvMessage
                        var newMessage = new v4BranchedConvMessage
                        {
                            Id = $"msg_{Guid.NewGuid()}",
                            ParentId = currentParentId,
                            Role = role,
                            ContentBlocks = new List<ContentBlock> { new ContentBlock { Content = finalContent, ContentType = ContentType.Text } },
                            Timestamp = DateTime.UtcNow,
                            Temperature = role == v4BranchedConvMessageRole.Assistant ? googleData.runSettings?.temperature : null
                        };

                        // Process any pending attachments for this message (typically user messages)
                        if (pendingAttachments.Count > 0 && googleDriveService != null)
                        {
                            var attachments = await ProcessAttachmentsAsync(pendingAttachments, googleDriveService);

                            foreach(var attachment in attachments)
                            {
                                newMessage.ContentBlocks.Add(new ContentBlock { Content = $"\n\n{BacktickHelper.ThreeTicks}{Path.GetExtension(attachment.Name).Replace(".", "")}\n{attachment.Content}\n```\n", ContentType = ContentType.Text });
                            }

                            pendingAttachments.Clear();
                        }

                        // Add this message to our flat list
                        aiStudioConv.Messages.Add(newMessage);

                        // Update currentParentId to this message's Id
                        currentParentId = newMessage.Id;
                    }
                }

                // 7. Handle any remaining thoughts
                if (pendingThoughts.Length > 0)
                {
                    var lastAssistantMsg = aiStudioConv.Messages.LastOrDefault(m => m.Role == v4BranchedConvMessageRole.Assistant);
                    if (lastAssistantMsg != null)
                    {
                        if (lastAssistantMsg.ContentBlocks == null)
                        {
                            lastAssistantMsg.ContentBlocks = new List<ContentBlock>();
                        }
                        lastAssistantMsg.ContentBlocks.Add(new ContentBlock { Content = $"\n\n<Thought>\n{pendingThoughts.ToString().Trim()}\n</Thought>", ContentType = ContentType.Text }) ;
                    }
                    else
                    {
                        // Create a new Assistant message just for these thoughts
                        var thoughtsMessage = new v4BranchedConvMessage
                        {
                            Id = $"msg_{Guid.NewGuid()}",
                            ParentId = currentParentId,
                            Role = v4BranchedConvMessageRole.Assistant,
                            ContentBlocks = new List<ContentBlock> { new ContentBlock { Content = $"<Thought>\n{pendingThoughts.ToString().Trim()}\n</Thought>", ContentType = ContentType.Text } },
                            Timestamp = DateTime.UtcNow
                        };
                        aiStudioConv.Messages.Add(thoughtsMessage);
                    }
                }

                // 8. Return the populated aiStudioConv
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

        private static async Task<List<Attachment>> ProcessAttachmentsAsync(List<string> fileIds, Core.Interfaces.IGoogleDriveService googleDriveService)
        {
            var attachments = new List<Attachment>();
            
            foreach (var fileId in fileIds)
            {
                try
                {
                    // Download file content from Google Drive
                    var fileContent = await googleDriveService.DownloadFileContentAsync(fileId);
                    
                    // Create attachment object.  This is pretty ropey.
                    var attachment = new Attachment
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = fileContent.Item1,
                        Type = DetermineFileType(fileContent.Item2),
                        Content = fileContent.Item2,
                        Size = System.Text.Encoding.UTF8.GetByteCount(fileContent.Item2),
                        TextContent = IsTextFile(fileContent.Item2) ? fileContent.Item2 : null,
                        LastModified = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };
                    
                    attachments.Add(attachment);
                }
                catch (Exception ex)
                {
                    // Log error but continue processing other attachments
                    // Create a placeholder attachment to indicate the error
                    var errorAttachment = new Attachment
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = $"failed_attachment_{fileId}",
                        Type = "error",
                        Content =$"Failed to download attachment: {ex.Message}",
                        Size = 0,
                        TextContent = $"Error downloading attachment with ID {fileId}: {ex.Message}",
                        LastModified = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };
                    
                    attachments.Add(errorAttachment);
                }
            }
            
            return attachments;
        }
        
        private static string DetermineFileType(string content)
        {
            // Simple heuristic to determine file type based on content
            if (string.IsNullOrEmpty(content))
                return "unknown";
                
            // Check for common text file patterns
            if (content.StartsWith("{") && content.TrimEnd().EndsWith("}"))
                return "application/json";
            if (content.StartsWith("<") && content.Contains(">"))
                return "text/html";
            if (content.Contains("\n") || content.Length < 1000) // Assume short content or multi-line is text
                return "text/plain";
                
            return "application/octet-stream";
        }
        
        private static bool IsTextFile(string content)
        {
            // Simple check to determine if content is text-based
            if (string.IsNullOrEmpty(content))
                return false;
                
            // Check for binary content indicators
            foreach (char c in content.Take(Math.Min(1000, content.Length)))
            {
                if (c == 0 || (c < 32 && c != '\t' && c != '\n' && c != '\r'))
                    return false;
            }
            
            return true;
        }

        // Keep the original synchronous method for backward compatibility
        public static v4BranchedConv ConvertToAiStudio4(string googleJsonContent, string originalFileName)
        {
            return ConvertToAiStudio4Async(googleJsonContent, originalFileName, null).GetAwaiter().GetResult();
        }
    }
}
