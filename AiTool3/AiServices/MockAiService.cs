using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.Interfaces;
using SharedClasses.Providers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AiTool3.AiServices
{
    internal class MockAiService : AiServiceBase
    {
        private readonly Random random = new Random();
        private const string LoremIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

        public override async Task<AiResponse> FetchResponse(
            ServiceProvider serviceProvider,
            Model model,
            LinearConversation conversation,
            string base64image,
            string base64ImageType,
            CancellationToken cancellationToken,
            ApiSettings apiSettings,
            bool mustNotUseEmbedding,
            List<string> toolIDs,
            bool useStreaming = false,
            bool addEmbeddings = false)
        {
            int wordCount = random.Next(10, 20);
            string[] words = LoremIpsum.Split(' ');
            StringBuilder responseBuilder = new StringBuilder();

            for (int i = 0; i < wordCount; i++)
            {
                responseBuilder.Append(words[i % words.Length]).Append(" ");
            }

            string responseText = responseBuilder.ToString().Trim();

            if (useStreaming)
            {
                await SimulateStreaming(words, wordCount, cancellationToken, responseText);
            }
            return new AiResponse
            {
                ResponseText = responseText,
                Success = true,
                TokenUsage = new TokenUsage(wordCount.ToString(), wordCount.ToString())
            };
        }

        private async Task SimulateStreaming(string[] words, int wordCount, CancellationToken cancellationToken, string responseText)
        {
            for (int i = 0; i < wordCount; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                string word = words[i % words.Length];
                OnStreamingDataReceived(word + " "); // Use the base method
                await Task.Delay(100, cancellationToken); // Simulate delay between words
            }
            OnStreamingComplete();
        }

        protected override Task<AiResponse> HandleStreamingResponse(HttpContent content, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("Should not call this in Mock");
        }

        protected override Task<AiResponse> HandleNonStreamingResponse(HttpContent content, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("Should not call this in Mock");
        }
    }
}