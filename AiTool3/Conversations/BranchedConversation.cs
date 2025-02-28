using AiTool3.DataModels;
using AiTool3.AiServices;
using Newtonsoft.Json;
using System.Diagnostics;
using static AiTool3.AutoSuggestForm;
using AiTool3.Tools;
using System.Text.RegularExpressions;
using SharedClasses.Providers;

namespace AiTool3.Conversations
{
    public class BranchedConversation
    {
        public List<CompletionMessage> Messages = new List<CompletionMessage>();
        public string ConvGuid { get; set; }
        public string Summary { get; set; } = "";
        public Color? HighlightColour { get; set; } = null;

        public event StringSelectedEventHandler StringSelected;

        public static string GetFilename(string guid) => $"Conversations\\v3-conversation-{guid}.json";

        public override string ToString()
        {
            return $"{Summary}";
        }


        public DateTime CreationDateTime { get; set; } = DateTime.Now;

        public void SaveConversation()
        {
            // write the object out as JSON
            string json = JsonConvert.SerializeObject(this);

            // wait until the file isnt' locked
            while (true)
            {
                try
                {
                    File.WriteAllText(GetFilename(ConvGuid), json);
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

        internal async Task<string> GenerateSummary(SettingsSet currentSettings)
        {

            var apiModel = currentSettings.GetSummaryModel() ?? currentSettings.GetModel();

            string responseText = "";
            Debug.WriteLine(Summary);
            try
            {

                var service = ServiceProvider.GetProviderForGuid(currentSettings.ServiceProviders, apiModel.ProviderGuid);

                // instantiate the service from name
                var aiService = AiServiceResolver.GetAiService(service.ServiceName, null);

                LinearConversation conversation = null;

                {
                    conversation = new LinearConversation(DateTime.Now);
                    conversation.systemprompt = "you are a bot who summarises conversations.  Summarise this conversation in six words or fewer as a json object like this, and produce no other output: {\"summary\": \"your summary text\"} ";
                    conversation.messages = new List<LinearConversationMessage>();
                    List<CompletionMessage> nodes = GetParentNodeList(Messages.Last().Guid);

                    Debug.WriteLine(nodes);

                    foreach (var node in nodes.Skip(1).Take(2))
                    {
                        var nodeContent = node.Content;
                        // truncate to 500 chars if necc
                        if (nodeContent != null && nodeContent.Length > 300)
                        {
                            nodeContent = nodeContent.Substring(0, 300);
                        }
                        conversation.messages.Add(new LinearConversationMessage { role = node.Role == CompletionRole.User ? "user" : "assistant", content = nodeContent });
                    }
                    conversation.messages.Add(new LinearConversationMessage { role = "user", content = "Excluding this instruction, summarise the above conversation in ten words or fewer as a json object like this, and produce no other output: {\"summary\": \"your summary text\"} " });

                }
                // fetch the response from the api
                var apiSettings = currentSettings.ToApiSettings();
                var response = await aiService.FetchResponse(service, apiModel, conversation, null, null, new CancellationToken(false), apiSettings, mustNotUseEmbedding: true, toolNames: null, useStreaming: false);

                Debug.WriteLine("Summary : " + response.ResponseText);

                // if there are ```, remove everything before it
                responseText = response.ResponseText;
                if (responseText != null)
                {
                    if (responseText.Contains("```"))
                    {
                        responseText = responseText.Substring(responseText.IndexOf("```"));
                    }

                    // remove ```json and ``` from the response
                    responseText = responseText.Replace("```json", "").Replace("```", "");
                    try
                    {
                        responseText = Regex.Replace(responseText, @"<think>.*?</think>", "", RegexOptions.Singleline);

                        if (responseText.StartsWith("`")) responseText = responseText.Substring(1);
                        if (responseText.EndsWith("`")) responseText = responseText.Substring(0, responseText.Length - 1);

                        dynamic obj = JsonConvert.DeserializeObject(responseText);

                        // get the first property name from obj
                        var propName = ((Newtonsoft.Json.Linq.JProperty)((Newtonsoft.Json.Linq.JContainer)obj).First).Name;

                        Summary = obj[propName];
                    }
                    catch
                    {
                        Summary = "Summary failed";
                    }
                }
                else Summary = "Summary failed";


                SaveConversation();

                return Summary;
            }
            catch (Exception)
            {
                return "Summary failed";
            }
        }

        internal async Task<AutoSuggestForm> GenerateAutosuggests(Model apiModel, SettingsSet currentSettings, bool fun, string userAutoSuggestPrompt)
        {
            AutoSuggestForm form = null;
            string responseText = "";
            Debug.WriteLine(Summary);


            var service = ServiceProvider.GetProviderForGuid(currentSettings.ServiceProviders, apiModel.ProviderGuid);

            var aiService = AiServiceResolver.GetAiService(service.ServiceName, null);

            LinearConversation conversation = null;

            string systemprompt = "";

            if (string.IsNullOrEmpty(userAutoSuggestPrompt))
                systemprompt = fun ? "you are a bot who makes fun and interesting suggestions on how a user might proceed with a conversation."
                    : "you are a bot who suggests how a user might proceed with a conversation.";
            else systemprompt = userAutoSuggestPrompt;

            {
                conversation = new LinearConversation(DateTime.Now);
                conversation.systemprompt = systemprompt;
                conversation.messages = new List<LinearConversationMessage>();
                conversation.systemprompt += $" {DateTime.Now.Ticks}";
                List<CompletionMessage> nodes = GetParentNodeList(Messages.Last().Guid);

                Debug.WriteLine(nodes);

                foreach (var node in nodes.Where(x => x.Role != CompletionRole.Root))
                {
                    var nodeContent = node.Content;

                    if (nodeContent.Length > 1000)
                    {
                        nodeContent = nodeContent.Substring(0, 1000);
                    }
                    conversation.messages.Add(new LinearConversationMessage { role = node.Role == CompletionRole.User ? "user" : "assistant", content = nodeContent });
                }
                conversation.messages.Add(new LinearConversationMessage { role = "user", content = $"based on our conversation so far, give me 25 {(fun ? "fun and interesting" : "")} things I might ask you to do next, as a json array of strings." });

            }

            // Create ApiSettings with default values since a null value was previously passed
            var apiSettings = new ApiSettings();
            var response = await aiService.FetchResponse(service, apiModel, conversation, null, null, new CancellationToken(false), apiSettings, mustNotUseEmbedding: true, toolNames: null, useStreaming: false);

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

            var regex = new System.Text.RegularExpressions.Regex(@"\[[\s\S]*\]");

            var matches = regex.Matches(responseText);
            responseText = matches[0].Value;

            try
            {
                Debug.WriteLine(responseText);
                dynamic obj = JsonConvert.DeserializeObject(responseText);



                var suggestions = obj.ToObject<string[]>();

                form = new AutoSuggestForm(suggestions);
                form.StringSelected += Form_StringSelected;
                form.Show();

            }
            catch (Exception e)
            {
                MessageBox.Show($"Something went wrong with AutoSuggest: {e.Message}");
            }


            return form;
        }

        private void Form_StringSelected(string selectedString) => StringSelected?.Invoke(selectedString);

        public string AddNewRoot()
        {
            var m = new CompletionMessage(CompletionRole.Root)
            {
                Content = "Conversation Start",
                CreatedAt = DateTime.Now,
            };
            Messages.Add(m);
            return m.Guid;
        }

        public List<CompletionMessage> GetParentNodeList(string guid)
        {
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

        public static BranchedConversation LoadConversation(string guid) => JsonConvert.DeserializeObject<BranchedConversation>(File.ReadAllText(GetFilename(guid)));

        public CompletionMessage GetRootNode() => Messages.FirstOrDefault(x => x.Role == CompletionRole.Root);

        internal static void DeleteConversation(string guid) => File.Delete(GetFilename(guid));
    }
}