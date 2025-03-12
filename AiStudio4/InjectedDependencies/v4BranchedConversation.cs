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

        internal v4BranchedConvMessage AddNewMessage(v4BranchedConvMessageRole role, string newMessageId, string userMessage, string parentMessageId)
        {
            var newMessage = new v4BranchedConvMessage
            {
                Role = role,
                UserMessage = userMessage ?? string.Empty,
                Id = newMessageId,
                ParentId = parentMessageId,
            };

            // If no parent is specified and no messages exist, create a system message as the root
            if (string.IsNullOrEmpty(parentMessageId) && !Messages.Any())
            {
                var systemRoot = new v4BranchedConvMessage
                {
                    Role = v4BranchedConvMessageRole.System,
                    UserMessage = "Conv Root",
                    Id = $"system_{Guid.NewGuid()}"
                };
                
                // Add system root to messages
                Messages.Add(systemRoot);
                
                // Set the parent of the new message to the system root
                newMessage.ParentId = systemRoot.Id;
            }
            
            // Add the new message to our flat list
            Messages.Add(newMessage);
            
            return newMessage;
        }

        public List<v4BranchedConvMessage> GetAllMessages()
        {
            return Messages;
        }
    }
}