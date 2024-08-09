using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.Providers;
using Newtonsoft.Json;

namespace AiTool3.Helpers
{
    public static class SpecialsHelper
    {

        public static async Task<AiResponse> GetReadmeResponses(Model model)
        {
            var httpClient = new HttpClient();

            var diff = await httpClient.GetStringAsync("https://github.com/stringandstickytape/MaxsAiTool/commit/main.diff");
            var readme = await httpClient.GetStringAsync("https://raw.githubusercontent.com/stringandstickytape/MaxsAiTool/main/README.md");

            httpClient.DefaultRequestHeaders.Add("User-Agent", "Other");
            var jsonText = await httpClient.GetStringAsync("https://api.github.com/repos/stringandstickytape/MaxsAiTool/commits/main");

            dynamic commitDetails = JsonConvert.DeserializeObject(jsonText)!;

            var commitMessage = commitDetails.commit.message.ToString();

            var userMessage = $"Random number: {DateTime.Now.Ticks}\nHere's a commit message, diff, and readme.  Update the readme content to reflect new and changed features, as described by the diff and commit message.  Don't change formatting or whitespace. Note that something as minor as \"Fade effect when output text box content changes\" is too trivial to be worth mentioning.  If you see anything that unimportant to the user, you must remove it. You absolutely must give me back the complete updated version, surrounded by ``` . {Environment.NewLine}```commitmessage{Environment.NewLine}{commitMessage}{Environment.NewLine}```{Environment.NewLine}{Environment.NewLine}```diff{Environment.NewLine}{diff}{Environment.NewLine}```{Environment.NewLine}{Environment.NewLine}```readme.md{Environment.NewLine}{readme}{Environment.NewLine}";
            var aiService = AiServiceResolver.GetAiService(model.ServiceName, null);
            var conversation = new Conversation { systemprompt = "Update the readme and give me the complete file.  DO NOT list your changes.", messages = new List<ConversationMessage> { new ConversationMessage { role = "user", content = userMessage } } };
            var response = await aiService!.FetchResponse(model, conversation, null!, null!, new CancellationToken(false), null, mustNotUseEmbedding: true, toolNames: null, useStreaming: false);

            return response;
        }
    }
}