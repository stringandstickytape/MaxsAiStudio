







using AiStudio4.Core.Models;

namespace AiStudio4.InjectedDependencies
{
    public class v4BranchedConv
    {
        public string ConvId { get; set; }
        public List<v4BranchedConvMessage> Messages { get; set; } = new List<v4BranchedConvMessage>();
        public string Summary { get; set; }
        public string SystemPromptId { get; set; }

        public v4BranchedConv() { }

        public v4BranchedConv(string convId)
        {
            ConvId = string.IsNullOrWhiteSpace(convId) ? throw new ArgumentNullException(nameof(convId)) : convId;
        }        public void Save()
        {
            string path = PathHelper.GetProfileSubPath("convs", $"{ConvId}.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonConvert.SerializeObject(this));
        }

        /// <summary>
        /// New overload accepting rich <see cref="ContentBlock"/> lists instead of a plain string.
        /// Internally this flattens the blocks for legacy compatibility and then sets the full list on the message.
        /// </summary>
        internal v4BranchedConvMessage AddOrUpdateMessage(
            v4BranchedConvMessageRole role,
            string newMessageId,
            List<ContentBlock> contentBlocks,
            string parentMessageId,
            List<DataModels.Attachment> attachments = null,
            TokenCost costInfo = null)
        {
            // Flatten text blocks so existing logic continues to work for summarisation etc.
            string flattened = contentBlocks == null ? null : string.Join("\n\n", contentBlocks.Select(cb => cb.Content));

            // Re-use legacy string-based overload
            var msg = AddOrUpdateMessage(role, newMessageId, flattened, parentMessageId, attachments, costInfo);

            // Persist the full structured blocks
            msg.ContentBlocks = contentBlocks ?? new List<ContentBlock>();

            Save();

            return msg;
        }

        internal v4BranchedConvMessage AddOrUpdateMessage(
            v4BranchedConvMessageRole role,
            string newMessageId,
            string userMessage,
            string parentMessageId,
            List<DataModels.Attachment> attachments = null,
            TokenCost costInfo = null)
        {
            // 1. Make sure there is always a root system‐message
            if (!Messages.Any())
            {
                Messages.Add(new v4BranchedConvMessage
                {
                    Role = v4BranchedConvMessageRole.System,
                    ContentBlocks = new List<ContentBlock> { new ContentBlock { Content = "Conversation Root", ContentType = ContentType.Text } },
                    Id = parentMessageId       // parent of the very first real message
                });
            }

            // 2. Get existing message or create a brand-new one
            var msg = Messages.FirstOrDefault(m => m.Id == newMessageId);
            if (msg == null)
            {
                msg = new v4BranchedConvMessage { Id = newMessageId };
                Messages.Add(msg);

                msg.ParentId = parentMessageId;
                msg.Role = role;
                
                // DEBUG: Log message creation
                Console.WriteLine($"🆕 CREATED MESSAGE - Conv: {ConvId}, Role: {role}, MessageId: {newMessageId}, ParentId: {parentMessageId}");
            }
            else
            {
                // DEBUG: Log message update
                Console.WriteLine($"🔄 UPDATED MESSAGE - Conv: {ConvId}, Role: {role}, MessageId: {newMessageId}, ParentId: {parentMessageId}");
            }

            // 3. (Re-)populate / overwrite the fields
            msg.ContentBlocks = new List<ContentBlock> { new ContentBlock { Content = userMessage, ContentType = ContentType.Text } };
            msg.Attachments = attachments ?? new List<DataModels.Attachment>();
            msg.CostInfo = costInfo;

            // 4. Calculate cumulative cost if the message is from the assistant
            if (role == v4BranchedConvMessageRole.Assistant && costInfo != null)
            {
                decimal cumulativeCost = costInfo.TotalCost;
                var parent = Messages.FirstOrDefault(m => m.Id == parentMessageId);

                while (parent != null)
                {
                    if (parent.Role == v4BranchedConvMessageRole.Assistant && parent.CostInfo != null)
                        cumulativeCost += parent.CostInfo.TotalCost;

                    parent = Messages.FirstOrDefault(m => m.Id == parent.ParentId);
                }

                msg.CumulativeCost = cumulativeCost;
            }

            return msg;
        }

        public List<v4BranchedConvMessage> GetAllMessages()
        {
            return Messages;
        }

        /// <summary>
        /// Gets the sequence of messages from the root to the specified message ID.
        /// </summary>
        /// <param name="messageId">The ID of the message to trace back from.</param>
        /// <returns>A list of cloned messages representing the path from the root to the target message.</returns>
        public List<v4BranchedConvMessage> GetMessageHistory(string messageId)
        {
            var allMessages = GetAllMessages(); // Use the existing method to get all messages
            var messageMap = allMessages.ToDictionary(m => m.Id);
            var path = new List<v4BranchedConvMessage>();

            // Find the target message
            if (!messageMap.TryGetValue(messageId, out var currentMessage))
            {
                // Message not found, return empty path or handle as error?
                return path;
            }

            // Build path from message to root
            while (currentMessage != null)
            {
                // Add a clone of the message to the beginning of the path
                path.Insert(0, currentMessage.Clone()); // Use the new Clone method

                // Stop if we've reached a message with no parent or a non-existent parent
                if (string.IsNullOrEmpty(currentMessage.ParentId) || !messageMap.TryGetValue(currentMessage.ParentId, out currentMessage))
                {
                    break;
                }
            }

            return path;
        }

        public v4BranchedConvMessage CreatePlaceholder(string assistantMessageId, string parentId)
        {
            return AddOrUpdateMessage(
                v4BranchedConvMessageRole.Assistant,
                assistantMessageId,
                "", // Content is initially empty
                        parentId // Parent is the user's message
            );
        }

    }
}
