using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Interfaces;
using AiTool3.Settings;
using Newtonsoft.Json;
using static AiTool3.AutoSuggestForm;

namespace AiTool3.Conversations
{
    public class ConversationManager
    {
        public BranchedConversation CurrentConversation { get; set; }
        public CompletionMessage PreviousCompletion { get; set; }

        public event StringSelectedEventHandler StringSelected;

        public ConversationManager()
        {
            
            CurrentConversation = new BranchedConversation { ConvGuid = Guid.NewGuid().ToString() };
            CurrentConversation.StringSelected += CurrentConversation_StringSelected;
        }

        private void CurrentConversation_StringSelected(string selectedString)
        {
            // pass thru
            StringSelected?.Invoke(selectedString);
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

        public async Task<string> GenerateConversationSummary(Model summaryModel, bool useLocalAi)
        {
            var retVal = await CurrentConversation.GenerateSummary(summaryModel, useLocalAi);
            SaveConversation();
            return retVal;
        }

        public void LoadConversation(string guid)
        {
            CurrentConversation = BranchedConversation.LoadConversation(guid);
            CurrentConversation.StringSelected += CurrentConversation_StringSelected;

        }

        public async Task RegenerateAllSummaries(Model summaryModel, bool useLocalAi, DataGridView dgv)
        {
            // Get all conversation files
            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "v3-conversation-*.json").OrderBy(f => new FileInfo(f).LastWriteTime).ToArray();

            // get the guids from v3-conversation-{guid}.json
            
            foreach (string file in files)
            {
                // Load each conversation
                string guid = Path.GetFileNameWithoutExtension(file).Replace("v3-conversation-", "").Replace(".json","");
                LoadConversation(guid);
                
                // Regenerate summary
                string newSummary = await GenerateConversationSummary(summaryModel, useLocalAi);

                Debug.WriteLine(newSummary);

            }

            // Refresh the DataGridView
            dgv.Refresh();
        }

        public async Task<AutoSuggestForm> Autosuggest(Model model, bool useLocalAi, DataGridView dgv, bool fun = false)
        {
            return await CurrentConversation.GenerateAutosuggests(model, useLocalAi, fun);
        }
    }
}