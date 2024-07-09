using AiTool3.ApiManagement;
using AiTool3.Interfaces;
using AiTool3.Providers;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static AiTool3.AutoSuggestForm;

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

        internal async Task<string> GenerateSummary(Model apiModel, bool useLocalAi)
        {
            string responseText = "";
            Debug.WriteLine(Title);
            if (useLocalAi)
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

                    foreach (var node in nodes.Skip(1).Take(2))
                    {
                        var nodeContent = node.Content;
                        // truncate to 500 chars if necc
                        if (nodeContent.Length > 300)
                        {
                            nodeContent = nodeContent.Substring(0, 300);
                        }
                        conversation.messages.Add(new ConversationMessage { role = node.Role == CompletionRole.User ? "user" : "assistant", content = nodeContent });
                    }
                    conversation.messages.Add(new ConversationMessage { role = "user", content = "Excluding this instruction, summarise the above conversation in ten words or fewer as a json object" });

                }
                // fetch the response from the api
                var response = await aiService.FetchResponse(apiModel, conversation, null, null, new CancellationToken(false));

                Debug.WriteLine("Summary : " + response.ResponseText);

                // if there are ```, remove everything before it
                responseText = response.ResponseText;
                if (responseText.Contains("```"))
                {
                    responseText = responseText.Substring(responseText.IndexOf("```"));
                }

                // remove ```json and ``` from the response
                responseText = responseText.Replace("```json", "").Replace("```", "");
                try
                {
                    dynamic obj = JsonConvert.DeserializeObject(responseText);

                    Title = obj.summary;
                }
                catch
                {
                    Title = "Summary failed";
                }
            }
            // jsonconvert to dynamic
            else
            {
                var msg = Messages.First(x => x.Role != CompletionRole.Root).Content;

                Title = msg.Length > 100 ? msg.Substring(0, 100) : msg;
            }

            SaveAsJson();

            return Title;
        }

        public event StringSelectedEventHandler StringSelected;

        internal async Task<string> GenerateAutosuggests(Model apiModel, bool useLocalAi, bool fun)
        {
            string responseText = "";
            Debug.WriteLine(Title);
                // instantiate the service from name
                var aiService = AiServiceResolver.GetAiService(apiModel.ServiceName);

                Conversation conversation = null;

                {
                    conversation = new Conversation();//tbSystemPrompt.Text, tbInput.Text
                    conversation.systemprompt = fun ? "you are a bot who makes fun and interesting suggestions on how a user might proceed with a conversation."
                    : "you are a bot who suggests how a user might proceed with a conversation.";
                    conversation.messages = new List<ConversationMessage>();
                    conversation.systemprompt += $" {DateTime.Now.Ticks}";
                    List<CompletionMessage> nodes = GetParentNodeList(Messages.Last().Guid);

                    Debug.WriteLine(nodes);

                    foreach (var node in nodes.Skip(1))
                    {
                        var nodeContent = node.Content;
                        // truncate to 500 chars if necc
                        if (nodeContent.Length > 1000)
                        {
                            nodeContent = nodeContent.Substring(0, 1000);
                        }
                        conversation.messages.Add(new ConversationMessage { role = node.Role == CompletionRole.User ? "user" : "assistant", content = nodeContent });
                    }
                    conversation.messages.Add(new ConversationMessage { role = "user", content = $"based on our conversation so far, give me 25 {(fun ? "fun and interesting" : "")} things I might ask you to do next, as a json array of strings." });

                }
                // fetch the response from the api
                var response = await aiService.FetchResponse(apiModel, conversation, null, null, new CancellationToken(false));

            var cost = apiModel.GetCost(response.TokenUsage);

            responseText = response.ResponseText;
                if (responseText.Contains("```"))
                {
                    responseText = responseText.Substring(responseText.IndexOf("```"));
                }

                // if there is anything after the second ```, remove it
                if (responseText.IndexOf("```", 3) > 0)
                {
                    responseText = responseText.Substring(0, responseText.IndexOf("```", 3));
                }
                // if there is anything before the first ```, remove it but not the ```
                if (responseText.IndexOf("```") > 0)
                {
                    responseText = responseText.Substring(responseText.IndexOf("```"));
                }


                // remove ```json and ``` from the response
                responseText = responseText.Replace("```json", "").Replace("```", "");

                

                try
                {
                Debug.WriteLine(responseText);
                dynamic obj = JsonConvert.DeserializeObject(responseText);
                /*
                 
[
  "Add a high-score feature to the Arkanoid clone",
  "Create a different classic arcade game, like Pac-Man or Space Invaders",
  "Implement power-ups in the Arkanoid game",
  "Design a responsive mobile version of the game",
  "Add sound effects and background music to the game",
  "Implement multiple levels with increasing difficulty",
  "Create a two-player mode for the Arkanoid clone",
  "Optimize the game's performance for smoother gameplay",
  "Add customization options for the paddle and ball",
  "Implement a level editor for users to create their own stages"
]
 */
                // dump to debug
                
                // convert obj to string array
                var suggestions = obj.ToObject<string[]>();
                
                var form = new AutoSuggestForm(suggestions);
                form.StringSelected += Form_StringSelected;
                form.Show();


                }
                catch(Exception e)
                {
                MessageBox.Show($"Something went wrong with AutoSuggest: {e.Message}");
                }


            return "";
        }

        private void Form_StringSelected(string selectedString)
        {
            // pass the selected string back to the main form
            StringSelected?.Invoke(selectedString);
        }

        public string AddNewRoot()
        {
            var m = new CompletionMessage
            {
                Guid = Guid.NewGuid().ToString(),
                Content = "Conversation Start",
                Role = CompletionRole.Root,
                Children = new List<string>(),
                CreatedAt = DateTime.Now,
            };
            Messages.Add(m);
            return m.Guid;
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

        public CompletionMessage GetRootNode()
        {
            return Messages.First(x => x.Role == CompletionRole.Root);
        }
    }
}
