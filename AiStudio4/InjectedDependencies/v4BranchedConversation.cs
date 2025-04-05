using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        }

        public void Save()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4", "convs", $"{ConvId}.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonConvert.SerializeObject(this));
        }

        internal v4BranchedConvMessage AddNewMessage(v4BranchedConvMessageRole role, string newMessageId, string userMessage, string parentMessageId, List<DataModels.Attachment> attachments = null)
        {
            var newMessage = new v4BranchedConvMessage
            {
                Role = role,
                UserMessage = userMessage ?? string.Empty,
                Id = newMessageId,
                ParentId = parentMessageId,
                Attachments = attachments ?? new List<DataModels.Attachment>(),
            };

            // If no messages exist, create a system message as the root
            if (!Messages.Any())
            {
                var systemRoot = new v4BranchedConvMessage
                {
                    Role = v4BranchedConvMessageRole.System,
                    UserMessage = "Conversation Root",
                    Id = newMessage.ParentId
                };
                
                // Add system root to messages
                Messages.Add(systemRoot);
            }
            
            // Add the new message to our flat list
            Messages.Add(newMessage);
            
            return newMessage;
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
    }
}