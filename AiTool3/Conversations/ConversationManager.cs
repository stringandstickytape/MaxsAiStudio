using AiTool3.DataModels;
using AiTool3.FileAttachments;
using AiTool3.UI;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static AiTool3.AutoSuggestForm;

namespace AiTool3.Conversations
{
    public class ConversationManager
    {
        public BranchedConversation? Conversation { get; set; }
        public CompletionMessage? MostRecentCompletion { get; set; }

        public event StringSelectedEventHandler? StringSelected;

        private DataGridView _dgvConversations;

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
                var current = MostRecentCompletion?.Guid;

                while (current != null)
                {
                    var node = Conversation!.FindByGuid(current);
                    nodes.Add(node);
                    current = node.Parent;
                }

                nodes.Reverse();
                return nodes;

            }
            catch (Exception)
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

        public async Task RegenerateSummary(Model summaryModel, DataGridView dgv, string guid, SettingsSet currentSettings)
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

        public async Task<AutoSuggestForm> Autosuggest(Model model, DataGridView dgv, bool fun = false, string userAutoSuggestPrompt = null!)
        {
            return await Conversation!.GenerateAutosuggests(model, fun, userAutoSuggestPrompt);
        }

        public async Task<Conversation> PrepareConversationData(Model model, string systemPrompt, string userPrompt, FileAttachmentManager fileAttachmentManager)
        {
            var conversation = InitializeConversation(systemPrompt);
            await AddHistoricalMessages(conversation);
            AddCurrentUserMessage(conversation, userPrompt, fileAttachmentManager);
            return conversation;
        }

        private Conversation InitializeConversation(string systemPrompt)
        {
            return new Conversation(Conversation.CreationDateTime)
            {
                systemprompt = systemPrompt,
                messages = new List<ConversationMessage>()
            };
        }

        private async Task AddHistoricalMessages(Conversation conversation)
        {
            List<CompletionMessage> nodes = GetParentNodeList();

            foreach (var node in nodes)
            {
                if (node.Role == CompletionRole.Root || node.Omit)
                    continue;

                conversation.messages.Add(CreateConversationMessage(node));
            }
        }

        private ConversationMessage CreateConversationMessage(CompletionMessage node)
        {
            return new ConversationMessage
            {
                role = node.Role == CompletionRole.User ? "user" : "assistant",
                content = node.Content!,
                base64image = node.Base64Image,
                base64type = node.Base64Type
            };
        }

        private void AddCurrentUserMessage(Conversation conversation, string userPrompt, FileAttachmentManager fileAttachmentManager)
        {
            conversation.messages.Add(new ConversationMessage
            {
                role = "user",
                content = userPrompt,
                base64image = fileAttachmentManager.Base64Image,
                base64type = fileAttachmentManager.Base64ImageType
            });
        }

        public void AddInputAndResponseToConversation(AiResponse response, Model model, Conversation conversation, string inputText, string systemPrompt,  out CompletionMessage completionInput, out CompletionMessage completionResponse)
        {
            var previousCompletionGuidBeforeAwait = MostRecentCompletion?.Guid;

            completionInput = new CompletionMessage(CompletionRole.User)
            {
                Content = inputText.Replace("\r", ""),
                Parent = MostRecentCompletion?.Guid,
                Engine = model.ModelName,
                SystemPrompt = systemPrompt,
                InputTokens = response.TokenUsage.InputTokens,
                OutputTokens = 0,
                Base64Image = conversation.messages.Last().base64image,
                Base64Type = conversation.messages.Last().base64type,
                CreatedAt = DateTime.Now,
            };
            if (MostRecentCompletion != null)
            {
                MostRecentCompletion.Children!.Add(completionInput.Guid);
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
                TimeTaken = response.Duration,
                CreatedAt = DateTime.Now,
            };
            Conversation.Messages.Add(completionResponse);

            completionInput.Children.Add(completionResponse.Guid);
            MostRecentCompletion = completionResponse;

            SaveConversation();

        }

        public static readonly string ThreeTicks = new string('`', 3);

        public string AddBranch(ChatWebViewAddBranchEventArgs e)
        {
            var nodeToDuplicate = Conversation!.FindByGuid(e.Guid);

            var originalContent = e.Content;
            var findAndReplacesJson = e.FindAndReplacesJson;
            var codeBlockIndex = e.CodeBlockIndex;

            var codeBlockPattern = @$"{ThreeTicks}[\s\S]*?{ThreeTicks}";
            var matches = Regex.Matches(nodeToDuplicate.Content, codeBlockPattern, RegexOptions.Multiline);

            if (codeBlockIndex < 0 || codeBlockIndex >= matches.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(e.CodeBlockIndex), "Invalid code block index");
            }

            var targetCodeBlock = matches[codeBlockIndex];
            var codeBlockStartIndex = targetCodeBlock.Index;
            var codeBlockEndIndex = codeBlockStartIndex + targetCodeBlock.Length;

            while (codeBlockStartIndex < nodeToDuplicate.Content.Length && nodeToDuplicate.Content[codeBlockStartIndex] != '\n')
            {
                codeBlockStartIndex++;
            }

            while (codeBlockEndIndex > 0 && nodeToDuplicate.Content[codeBlockEndIndex - 1] != '\n')
            {
                codeBlockEndIndex--;
            }

            var codeBlockLength = codeBlockEndIndex - codeBlockStartIndex;

            var fnrs = JsonConvert.DeserializeObject<FindAndReplaceSet>(findAndReplacesJson);

            var processed = FineAndReplaceProcessor.ApplyFindAndReplace(originalContent, fnrs.replacements.ToList(), out string errorString);
            if (processed == null)
            {
                return null;
            }

            var newContent = nodeToDuplicate.Content.Substring(0, codeBlockStartIndex) + "\n" + processed + "\n" + nodeToDuplicate.Content.Substring(codeBlockStartIndex + codeBlockLength);
            nodeToDuplicate.Content = newContent;

            SaveConversation();

            return nodeToDuplicate.Guid;
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
            var nextLineIndex = msgContent.IndexOf('\n') + 1;
            msgContent = msgContent.Substring(nextLineIndex);

            // get the last line of previousMessage
            var lastLineIndex = previousMessage.Content.LastIndexOf('\n') + 1;
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

            if (continueMessage == null)
            {
                return;
            }

            // get that msg's first child
            var child = Conversation.FindByGuid(continueMessage.Children!.First());
            var childContent = child.Content.Substring(child.Content.IndexOf(ThreeTicks));

            //remove the first line from childContent
            childContent = childContent.Substring(childContent.IndexOf('\n') + 1);

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

        internal void InjectDepencencies(DataGridView dgvConversations)
        {
            _dgvConversations = dgvConversations;
        }

        internal void BeginNewConversation()
        {
            _dgvConversations.Enabled = true;
            Conversation = new BranchedConversation { ConvGuid = Guid.NewGuid().ToString() };
            Conversation.AddNewRoot();
            MostRecentCompletion = Conversation.Messages.First();
            
        }

        internal void AddMessagePair(CompletionMessage userMessage, CompletionMessage assistantMessage)
        {
            Conversation.Messages.AddRange(new[] { userMessage, assistantMessage });
            MostRecentCompletion = assistantMessage;
        }

        internal void GetConversationContext(out CompletionMessage? lastAssistantMessage, out CompletionMessage lastUserMessage)
        {
            lastAssistantMessage = MostRecentCompletion;
            lastUserMessage = Conversation!.FindByGuid(lastAssistantMessage!.Parent!);
            if (lastUserMessage == null)
                return;
            if (lastAssistantMessage.Role == CompletionRole.User)
                lastAssistantMessage = Conversation.FindByGuid(MostRecentCompletion!.Parent!);
        }

        internal async Task UpdateConversationSummary(SettingsSet settings)
        {
            var row = _dgvConversations.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Cells[0]?.Value?.ToString() == Conversation.ConvGuid);

            if (row != null && row.Cells[3].Value != null && string.IsNullOrWhiteSpace(row.Cells[3].Value.ToString()))
            {
                await GenerateConversationSummary(settings);
                row.Cells[3].Value = Conversation.ToString();
            }

            SaveConversation();
        }

        internal void CreateNewConversationFromUserAssistantPair(CompletionMessage? lastAssistantMessage, CompletionMessage lastUserMessage, out CompletionMessage assistantMessage, out CompletionMessage userMessage)
        {
            assistantMessage = new CompletionMessage(CompletionRole.Assistant)
            {
                Parent = null,
                Content = lastAssistantMessage.Content,
                Engine = lastAssistantMessage.Engine,

                CreatedAt = DateTime.Now,
            };
            var rootMessage = Conversation.GetRootNode();

            userMessage = new CompletionMessage(CompletionRole.User)
            {
                Parent = rootMessage.Guid,
                Content = lastUserMessage.Content,
                Engine = lastUserMessage.Engine,

                CreatedAt = DateTime.Now,
            };
            rootMessage.Children!.Add(userMessage.Guid);
            assistantMessage.Parent = userMessage.Guid;
            userMessage.Children.Add(assistantMessage.Guid);

            AddMessagePair(userMessage, assistantMessage);
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

        public string? filename { get; set; }
    }

}