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
        public BranchedConversation? CurrentConversation { get; set; }
        public CompletionMessage? PreviousCompletion { get; set; }

        public event StringSelectedEventHandler? StringSelected;

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
                var node = CurrentConversation!.FindByGuid(current);
                nodes.Add(node);
                current = node.Parent;
            }

            nodes.Reverse();
            return nodes;
        }

        public void SaveConversation()
        {
            CurrentConversation!.SaveAsJson();
        }

        public async Task<string> GenerateConversationSummary(Model summaryModel, bool useLocalAi, SettingsSet currentSettings)
        {
            var retVal = await CurrentConversation!.GenerateSummary(summaryModel, useLocalAi, currentSettings);
            SaveConversation();
            return retVal;
        }

        public void LoadConversation(string guid)
        {
            CurrentConversation = BranchedConversation.LoadConversation(guid);
            CurrentConversation.StringSelected += CurrentConversation_StringSelected;

        }

        public async Task RegenerateSummary(Model summaryModel, bool useLocalAi, DataGridView dgv, string guid, SettingsSet currentSettings)
        {
            // Get all conversation files
            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), $"v3-conversation-{guid}.json").OrderBy(f => new FileInfo(f).LastWriteTime).ToArray();

            // get the guids from v3-conversation-{guid}.json

            foreach (string file in files)
            {
                // Load each conversation
                string guid2 = Path.GetFileNameWithoutExtension(file).Replace("v3-conversation-", "").Replace(".json", "");
                LoadConversation(guid2);

                // Regenerate summary
                string newSummary = await GenerateConversationSummary(summaryModel, useLocalAi, currentSettings);

                Debug.WriteLine(newSummary);

                // find the dgv row where column 0 == guid
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.Cells[0].Value.ToString() == guid2)
                    {
                        row.Cells[3].Value = newSummary;
                        break;  
                    }
                }
            }

            // Refresh the DataGridView
            dgv.Refresh();
        }

        public async Task<AutoSuggestForm> Autosuggest(Model model, bool useLocalAi, DataGridView dgv, bool fun = false, string userAutoSuggestPrompt = null!)
        {
            return await CurrentConversation!.GenerateAutosuggests(model, useLocalAi, fun, userAutoSuggestPrompt);
        }

        public  async Task<Conversation> PrepareConversationData(Model model, string systemPrompt, string userPrompt, FileAttachmentManager fileAttachmentManager)
        {
            var conversation = new Conversation
            {
                systemprompt = systemPrompt,
                messages = new List<ConversationMessage>()
            };

            List<CompletionMessage> nodes = GetParentNodeList();

            foreach (var node in nodes)
            {
                if (node.Role == CompletionRole.Root || node.Omit)
                    continue;

                conversation.messages.Add(
                    new ConversationMessage
                    {
                        role = node.Role == CompletionRole.User ? "user" : "assistant",
                        content = node.Content!,
                        base64image = node.Base64Image,
                        base64type = node.Base64Type
                    });
            }
            conversation.messages.Add(new ConversationMessage { role = "user", content = userPrompt, base64image = fileAttachmentManager.Base64Image, base64type = fileAttachmentManager.Base64ImageType });

            return conversation;
        }
    }
}