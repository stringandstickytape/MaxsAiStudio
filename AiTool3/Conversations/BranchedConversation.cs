using AiTool3.ApiManagement;
using AiTool3.Interfaces;
using AiTool3.Providers;
using Newtonsoft.Json;
using System.Diagnostics;

namespace AiTool3.Conversations
{
    public class BranchedConversation
    {
        public List<CompletionMessage> Messages = new List<CompletionMessage>();
        public string ConvGuid { get; set; }
        public string Title { get; set; }

        public void SaveAsJson()
        {
            // write the object out as JSON
            string json = JsonConvert.SerializeObject(this);
            var filename = $"v3-conversation-{ConvGuid}.json";

            // wait until the file isnt' locked
            while (true)
            {
                try
                {
                    File.WriteAllText(filename, json);
                    break;
                }
                catch (IOException)
                {
                    Debug.WriteLine("File is locked, waiting...");
                    Thread.Sleep(50);
                }
            }
        }

        public CompletionMessage FindByGuid(string guid)
        {
            return Messages.FirstOrDefault(cm => cm.Guid == guid);
        }

        internal async Task<string> GenerateSummary(Model apiModel)
        {
            // instantiate the service from name
            var aiService = AiServiceResolver.GetAiService(apiModel.ServiceName);

            Conversation conversation = null;

            {
                conversation = new Conversation();//tbSystemPrompt.Text, tbInput.Text
                conversation.systemprompt = "you are a bot who summarises conversations.  Summarise this conversation in six words or fewer as a json object";
                conversation.messages = new List<ConversationMessage>();
                List<CompletionMessage> nodes = GetParentNodeList(Messages.Last().Guid);

                Debug.WriteLine(nodes);

                foreach (var node in nodes.Take(2))
                {
                    var nodeContent = node.Content;
                    // truncate to 500 chars if necc
                    if (nodeContent.Length > 500)
                    {
                        nodeContent = nodeContent.Substring(0, 500);
                    }
                    conversation.messages.Add(new ConversationMessage { role = node.Role == CompletionRole.User ? "user" : "assistant", content = nodeContent });
                }
                conversation.messages.Add(new ConversationMessage { role = "user", content = "Excluding this instruction, summarise the above conversation in ten words or fewer as a json object" });

            }
            // fetch the response from the api
            var response = await aiService.FetchResponse(apiModel, conversation, null, null);

            Debug.WriteLine("Summary : " + response.ResponseText);

            // if there are ```, remove everything before it
            var responseText = response.ResponseText;
            if (responseText.Contains("```"))
            {
                responseText = responseText.Substring(responseText.IndexOf("```"));
            }

            // remove ```json and ``` from the response
            responseText = responseText.Replace("```json", "").Replace("```", "");

            // jsonconvert to dynamic
            dynamic obj = JsonConvert.DeserializeObject(responseText);
            
            Title = obj.summary;

            SaveAsJson();

            return Title;
        }

        private List<CompletionMessage> GetParentNodeList(string guid)
        {
            // starting at PreviousCompletion, walk up the tree to the root node and return a list of nodes
            var nodes = new List<CompletionMessage>();
            var current = guid;

            while (current != null)
            {
                var node = FindByGuid(current);
                nodes.Add(node);
                current = node.Parent;
            }

            nodes.Reverse();

            return nodes;
        }

        public static BranchedConversation LoadConversation(string guid)
        {
            return JsonConvert.DeserializeObject<BranchedConversation>(File.ReadAllText($"v3-conversation-{guid}.json"));
        }
    }
}
