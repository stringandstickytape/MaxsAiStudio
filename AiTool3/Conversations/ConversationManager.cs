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
        public BranchedConversation? Conversation { get; set; }
        public CompletionMessage? PreviousCompletion { get; set; }

        public event StringSelectedEventHandler? StringSelected;

        public ConversationManager()
        {
            
            Conversation = new BranchedConversation { ConvGuid = Guid.NewGuid().ToString() };
            Conversation.StringSelected += Conversation_StringSelected;
        }

        private void Conversation_StringSelected(string selectedString)
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
                var node = Conversation!.FindByGuid(current);
                nodes.Add(node);
                current = node.Parent;
            }

            nodes.Reverse();
            return nodes;
        }

        public void SaveConversation()
        {
            Conversation!.SaveConversation();
        }

        public async Task<string> GenerateConversationSummary(SettingsSet currentSettings)
        {
            return await Conversation!.GenerateSummary(currentSettings);
        }

        public void LoadConversation(string guid)
        {
            Conversation = BranchedConversation.LoadConversation(guid);
            Conversation.StringSelected += Conversation_StringSelected;

        }

        public async Task RegenerateSummary(Model summaryModel, bool useLocalAi, DataGridView dgv, string guid, SettingsSet currentSettings)
        {
            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), BranchedConversation.GetFilename(guid)).OrderBy(f => new FileInfo(f).LastWriteTime).ToArray();

            // get the guids from v3-conversation-{guid}.json

            foreach (string file in files)
            {
                // Load each conversation
                var patternFilename = BranchedConversation.GetFilename("ABC");
                var filenamePattern = patternFilename.Substring(0, patternFilename.IndexOf("ABC"));

                string guid2 = Path.GetFileNameWithoutExtension(file).Replace(filenamePattern, "").Replace(".json", "");
                LoadConversation(guid2);

                // Regenerate summary
                string newSummary = await GenerateConversationSummary(currentSettings);

                Debug.WriteLine(newSummary);

                // find the dgv row where column 0 == guid
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.Cells[0].Value.ToString() == guid2)
                    {
                        row.Cells[3].Value = Conversation.ToString();
                        break;  
                    }
                }
            }

            // Refresh the DataGridView
            dgv.Refresh();
        }

        public async Task<AutoSuggestForm> Autosuggest(Model model, bool useLocalAi, DataGridView dgv, bool fun = false, string userAutoSuggestPrompt = null!)
        {
            return await Conversation!.GenerateAutosuggests(model, useLocalAi, fun, userAutoSuggestPrompt);
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

        internal void CreateNewMessages()
        {
            throw new NotImplementedException();
        }

        public void AddInputAndResponseToConversation(AiResponse response, Model model, Conversation conversation, string inputText, string systemPrompt, TimeSpan elapsed, out CompletionMessage completionInput, out CompletionMessage completionResponse)
        {
                var previousCompletionGuidBeforeAwait = PreviousCompletion?.Guid;

                completionInput = new CompletionMessage(CompletionRole.User)
                {
                    Content = inputText,
                    Parent = PreviousCompletion?.Guid,
                    Engine = model.ModelName,
                    SystemPrompt = systemPrompt,
                    InputTokens = response.TokenUsage.InputTokens,
                    OutputTokens = 0,
                    Base64Image = conversation.messages.Last().base64image,
                    Base64Type = conversation.messages.Last().base64type,
                    CreatedAt = DateTime.Now,
                };
                if (PreviousCompletion != null)
                {
                    PreviousCompletion.Children!.Add(completionInput.Guid);
                }

                Conversation!.Messages.Add(completionInput);

                completionResponse = new CompletionMessage(CompletionRole.Assistant)
                {
                    Content = response.ResponseText,
                    Parent = completionInput.Guid,
                    Engine = model.ModelName,
                    SystemPrompt = systemPrompt,
                    InputTokens = 0,
                    OutputTokens = response.TokenUsage.OutputTokens,
                    TimeTaken = elapsed,
                    CreatedAt = DateTime.Now,
                };
                Conversation.Messages.Add(completionResponse);

                completionInput.Children.Add(completionResponse.Guid);
                PreviousCompletion = completionResponse;

                SaveConversation();

        }
    }
}