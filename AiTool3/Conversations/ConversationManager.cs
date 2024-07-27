using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Interfaces;
using AiTool3.Settings;
using AiTool3.UI;
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
            try
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
            catch (Exception e)
            {
                MessageBox.Show("Weird bug I haven't nailed yet, has fired.  Please start a new conversation :/");
                return null;
            }
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

                string guid2 = Path.GetFileNameWithoutExtension(file).Replace(Path.GetFileNameWithoutExtension(filenamePattern), "").Replace(".json", "");
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

        public async Task<Conversation> PrepareConversationData(Model model, string systemPrompt, string userPrompt, FileAttachmentManager fileAttachmentManager)
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
                Content = inputText.Replace("\r",""),
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
                Content = response.ResponseText.Replace("\r", ""),
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

        public static readonly string ThreeTicks = new string('`', 3);

        public string AddBranch(ChatWebViewAddBranchEventArgs e)
        {
            var nodeToDuplicate = Conversation!.FindByGuid(e.Guid);

            var originalContent = e.Content;
            var findAndReplacesJson = e.FindAndReplacesJson;
            var codeBlockIndex = e.CodeBlockIndex;

            // code blocks in nodeToDuplicaet are demarcated by ThreeTicks either side. Find the start character index in nodeToDuplicate.Content
            // for code block e.CodeBlockIndex, using a regex


            var codeBlockPattern = @$"{ThreeTicks}[\s\S]*?{ThreeTicks}";
            var matches = Regex.Matches(nodeToDuplicate.Content, codeBlockPattern, RegexOptions.Multiline);

            if (codeBlockIndex < 0 || codeBlockIndex >= matches.Count)
            {
                // Handle invalid codeBlockIndex
                throw new ArgumentOutOfRangeException(nameof(e.CodeBlockIndex), "Invalid code block index");
            }

            var targetCodeBlock = matches[codeBlockIndex];
            var codeBlockStartIndex = targetCodeBlock.Index;
            var codeBlockEndIndex = codeBlockStartIndex + targetCodeBlock.Length;
            // advance codeBlockStartIndex to the begining of the next line
            while (codeBlockStartIndex < nodeToDuplicate.Content.Length && nodeToDuplicate.Content[codeBlockStartIndex] != '\n')
            {
                codeBlockStartIndex++;
            }

            // reverse CodeBlockEndIndex to the beginning of the current line
            while (codeBlockEndIndex > 0 && nodeToDuplicate.Content[codeBlockEndIndex - 1] != '\n')
            {
                codeBlockEndIndex--;
            }

            // calculate codeBlockLength
            var codeBlockLength = codeBlockEndIndex - codeBlockStartIndex;

            var fnrs = JsonConvert.DeserializeObject<FindAndReplaceSet>(findAndReplacesJson);

            var processed = FileProcessor.ApplyFindAndReplace(originalContent, fnrs.replacements.ToList());
            if(processed == null)
            {
                return null;
            }
            // now take the original content, and replace the code block with the processed content
            var newContent = nodeToDuplicate.Content.Substring(0, codeBlockStartIndex) + "\n" + processed + "\n" + nodeToDuplicate.Content.Substring(codeBlockStartIndex + codeBlockLength);

            // now add a new message which we'll append to NodeToDuplicate, ostensibly from the user, sayign "incorporate those changes"
            var newMessage = new CompletionMessage(CompletionRole.User)
            {
                Content = "Incorporate those changes",
                Parent = e.SelectedMessageGuid,
                Engine = nodeToDuplicate.Engine,
                SystemPrompt = nodeToDuplicate.SystemPrompt,
                InputTokens = 0,
                OutputTokens = 0,
                Base64Image = nodeToDuplicate.Base64Image,
                Base64Type = nodeToDuplicate.Base64Type,
                CreatedAt = DateTime.Now,
            };

            Conversation!.Messages.Add(newMessage);
            nodeToDuplicate.Children!.Add(newMessage.Guid);

            // and from that message, add another, ostensibly from the assistant, quoting the new content
            var newMessage2 = new CompletionMessage(CompletionRole.Assistant)
            {
                Content = newContent,
                Parent = newMessage.Guid,
                Engine = nodeToDuplicate.Engine,
                SystemPrompt = nodeToDuplicate.SystemPrompt,
                InputTokens = 0,
                OutputTokens = 0,
                Base64Image = nodeToDuplicate.Base64Image,
                Base64Type = nodeToDuplicate.Base64Type,
                CreatedAt = DateTime.Now,
            };

            Conversation.Messages.Add(newMessage2);
            newMessage.Children.Add(newMessage2.Guid);

            SaveConversation();

            return newMessage2.Guid;
        }

        internal void MergeWithPrevious(string guidValue)
        {
            // get the message with the given guid value
            var message = Conversation!.FindByGuid(guidValue);
            // get the preceding message of the same role
            var previousMessage = Conversation.Messages.LastOrDefault(m => m.Role == message.Role && m.Guid != message.Guid);

            var msgContent = message.Content;

            // remove everything before the first three ticks
            var firstThreeTicksIndex = msgContent.IndexOf(ThreeTicks);
            msgContent = msgContent.Substring(firstThreeTicksIndex);

            // remove the next line
            var nextLineIndex = msgContent.IndexOf('\n')+1;
            msgContent = msgContent.Substring(nextLineIndex);

            // get the last line of previousMessage
            var lastLineIndex = previousMessage.Content.LastIndexOf('\n')+1;
            var lastLine = previousMessage.Content.Substring(lastLineIndex);

            // get the first line of msgContent
            var firstLine = msgContent.Substring(0, msgContent.IndexOf('\n'));
            var newContent = previousMessage.Content;
            // ignoring whitespace, is the last line of prev the same as the first line of the message?
            if (firstLine.Trim().StartsWith(lastLine.Trim()))
            {
                // remove the last line of prev
                newContent = previousMessage.Content.Substring(0, lastLineIndex);
            }
                // append the message content to the end of prev
                newContent += msgContent;
                // remove the message
                //Conversation.Messages.Remove(message);
                //SaveConversation();

                // find the parent message of message
                var parentOfCurrent = Conversation.FindByGuid(message.Parent);
                var parentOfPrevious = Conversation.FindByGuid(previousMessage.Parent);

                // remove previousMessage from the parent's children
                parentOfPrevious.Children!.Remove(previousMessage.Guid);

                // remove all three from the conversation
                Conversation.Messages.Remove(message);
                Conversation.Messages.Remove(parentOfCurrent);
                Conversation.Messages.Remove(previousMessage);

                // create a new message out of newContent, Assistant role
                var newMessage = new CompletionMessage(CompletionRole.Assistant)
                {
                    Content = newContent,
                    Parent = parentOfPrevious.Guid,
                    Engine = previousMessage.Engine,
                    SystemPrompt = previousMessage.SystemPrompt,
                    InputTokens = previousMessage.InputTokens,
                    OutputTokens = previousMessage.OutputTokens,
                    Base64Image = previousMessage.Base64Image,
                    Base64Type = previousMessage.Base64Type,
                    CreatedAt = DateTime.Now,
                };

                // add the new message to the conversation
                Conversation.Messages.Add(newMessage);

                // add the new message to the parent's children
                parentOfPrevious.Children!.Add(newMessage.Guid);

                // save the conversation
                SaveConversation();
            //}
        }

        internal void ContinueUnterminatedCodeBlock(ChatWebViewSimpleEventArgs e)
        {
            // get the message with the given guid value
            var message = Conversation!.FindByGuid(e.Guid);

            // get the child message that says "Continue", case insensitive
            var continueMessage = Conversation.FindByGuid(message.Children!.FirstOrDefault(c => Conversation.FindByGuid(c).Content.ToLower().Contains("continue")));

            if(continueMessage == null)
            {
                return;
            }

            // get that msg's first child
            var child = Conversation.FindByGuid(continueMessage.Children!.First());
            var childContent = child.Content.Substring(child.Content.IndexOf(ThreeTicks));

            //remove the first line from childContent
            childContent = childContent.Substring(childContent.IndexOf('\n')+1);

            // split message.Content into lines and get the last
            var lastMsgLine = message.Content.Split('\n').Last();

            // similarly the first line for child.Content
            var firstChildLine = childContent.Split('\n').First();

            // IGNORING whitespace, is the last line of message the same as the first line of child?
            if (lastMsgLine.Trim().StartsWith(firstChildLine.Trim()))
            {
                // remove the last line of message
                message.Content = message.Content.Substring(0, message.Content.LastIndexOf('\n'));
            }

            var joinedMessage = message.Content + childContent;

            message.Content = joinedMessage;
            message.Children.Remove(continueMessage.Guid);
            
            Conversation.Messages.Remove(continueMessage);
            Conversation.Messages.Remove(child);

            SaveConversation();


        }
    }


    public class FindAndReplaceSet
    {
        public FindAndReplace[] replacements { get; set; }
    }

    public class FindAndReplace
    {
        public string find { get; set; }
        public string replace { get; set; }
    }

}