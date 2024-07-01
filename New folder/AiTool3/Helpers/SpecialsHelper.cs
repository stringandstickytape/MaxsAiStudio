using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Providers;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace AiTool3.Helpers
{
    public static class SpecialsHelper
    {

        public static void GetReadmeResponses(Model model, out AiResponse response, out AiResponse response2)
        {
            var diff = new WebClient().DownloadString("https://github.com/stringandstickytape/MaxsAiTool/commit/main.diff");
            var readme = new WebClient().DownloadString("https://raw.githubusercontent.com/stringandstickytape/MaxsAiTool/main/README.md");

            // pull commit details json from https://api.github.com/repos/stringandstickytape/MaxsAiTool/commits/main
            // include a User-Agent
            var client = new WebClient();
            client.Headers.Add("User-Agent: Other");
            var jsonText = client.DownloadString("https://api.github.com/repos/stringandstickytape/MaxsAiTool/commits/main");


            // dyn jsonconv
            dynamic commitDetails = JsonConvert.DeserializeObject(jsonText);

            var commitMessage = commitDetails.commit.message.ToString();

            // get AI to compare them

            var userMessage = $"Random number: {DateTime.Now.Ticks}\nHere's a commit message, diff, and readme.  Update the readme content to reflect new and changed features, as described by the diff and commit message.  Don't change formatting or whitespace. Note that something as minor as \"Fade effect when output text box content changes\" is too trivial to be worth mentioning.  If you see anything that unimportant to the user, you must remove it. Give me back the complete updated version, surrounded by ``` . {Environment.NewLine}```commitmessage{Environment.NewLine}{commitMessage}{Environment.NewLine}```{Environment.NewLine}{Environment.NewLine}```diff{Environment.NewLine}{diff}{Environment.NewLine}```{Environment.NewLine}{Environment.NewLine}```readme.md{Environment.NewLine}{readme}{Environment.NewLine}";
            var aiService = AiServiceResolver.GetAiService(model.ServiceName);
            var conversation = new Conversation { systemprompt = "Update the readme", messages = new List<ConversationMessage> { new ConversationMessage { role = "user", content = userMessage } } };
            response = aiService.FetchResponse(model, conversation, null, null).Result;

            // add the response to the conversation
            conversation.messages.Add(new ConversationMessage { role = "assistant", content = response.ResponseText });

            // add the next user message: "now give me a full list of the changes you made"
            conversation.messages.Add(new ConversationMessage { role = "user", content = "Now give me a full, detailed bullet list of the changes you made" });

            // and fetch a second response
            response2 = aiService.FetchResponse(model, conversation, null, null).Result;
        }

        public static void ReviewCode(Model model, out string userMessage)
        {
            var path = Directory.GetCurrentDirectory();
            while (!Directory.Exists(Path.Combine(path, "MaxsAiTool")))
            {
                path = Path.GetDirectoryName(path);
            }

            // recurse downwards through all subdirectories finding all the CS files
            var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);

            StringBuilder sb = new StringBuilder();

            foreach (var file in files)
            {
                if (file.Contains(".g") || file.Contains(".Assembly"))
                    continue;
                sb.AppendLine($"```{file}");
                sb.Append(File.ReadAllText(file));
                sb.AppendLine($"");
                sb.AppendLine($"```");
                sb.AppendLine();

            };

            // get AI to compare them

            userMessage = $"{sb.ToString()}{Environment.NewLine}Review this C# code and spot the bugs please.";

        }
    }
}