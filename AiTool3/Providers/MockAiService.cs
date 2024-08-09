using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.Interfaces;
using AiTool3.Tools;
using System.Text;

namespace AiTool3.Providers
{
    internal class MockAiService : IAiService
    {
        public ToolManager ToolManager { get; set; }
        private readonly Random random = new Random();
        private const string LoremIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;

        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, SettingsSet currentSettings, bool mustNotUseEmbedding, List<string> toolIDs, bool useStreaming = false, bool addEmbeddings = false)
        {
            int wordCount = random.Next(10, 20);
            string[] words = LoremIpsum.Split(' ');



            StringBuilder responseBuilder = new StringBuilder();
            for (int i = 0; i < wordCount; i++)
            {
                responseBuilder.Append(words[i % words.Length]).Append(" ");
            }


            if (useStreaming)
            {
                await SimulateStreaming(words, wordCount, cancellationToken, responseBuilder.ToString().Trim());
            }
            else
            {

            }

            return new AiResponse
            {
                ResponseText = responseBuilder.ToString().Trim(),
                Success = true,
                TokenUsage = new TokenUsage(wordCount.ToString(), wordCount.ToString())
            };
        }

        private async Task SimulateStreaming(string[] words, int wordCount, CancellationToken cancellationToken, string v)
        {
            for (int i = 0; i < wordCount; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                string word = words[i % words.Length];
                StreamingTextReceived?.Invoke(this, word + " ");
                await Task.Delay(100, cancellationToken); // 3 words per second
            }

            StreamingComplete?.Invoke(this, v);
        }
    }
}