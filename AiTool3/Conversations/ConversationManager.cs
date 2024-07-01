using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Interfaces;
using AiTool3.Settings;
using Newtonsoft.Json;

namespace AiTool3.Conversations
{
    public class ConversationManager
    {
        public BranchedConversation CurrentConversation { get; set; }
        public CompletionMessage PreviousCompletion { get; set; }

        public ConversationManager()
        {
            CurrentConversation = new BranchedConversation { ConvGuid = Guid.NewGuid().ToString() };


        }

        public List<CompletionMessage> GetParentNodeList()
        {
            var nodes = new List<CompletionMessage>();
            var current = PreviousCompletion?.Guid;

            while (current != null)
            {
                var node = CurrentConversation.FindByGuid(current);
                nodes.Add(node);
                current = node.Parent;
            }

            nodes.Reverse();
            return nodes;
        }

        public void SaveConversation()
        {
            CurrentConversation.SaveAsJson();
        }

        public async Task<string> GenerateConversationSummary(Model summaryModel)
        {
            return await CurrentConversation.GenerateSummary(summaryModel);
        }

        public void LoadConversation(string guid)
        {
            CurrentConversation = BranchedConversation.LoadConversation(guid);
        }


    }
}