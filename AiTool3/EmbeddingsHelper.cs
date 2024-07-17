using AiTool3.Conversations;
using AiTool3.Providers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace AiTool3
{
    internal static class EmbeddingsHelper
    {
        public static async Task<string> AddEmbeddingsToInput(Conversation conversation, SettingsSet currentSettings, string input)
        {
            if (currentSettings.UseEmbeddings)
            {
                var embeddingText = input+" ";
                // last but one msg
                var lbom = conversation.messages.Count > 1 ? conversation.messages[conversation.messages.Count - 2].content : "";

                if(string.IsNullOrEmpty(lbom) || lbom != input)
                {
                    embeddingText += lbom + " ";
                }
                var embeddings = await GetRelatedCodeFromEmbeddings(currentSettings.EmbeddingKey, embeddingText, currentSettings.EmbeddingsFilename);
                var lastMsg = $"{conversation.messages.Last().content}" +
                    $"{Environment.NewLine}{Environment.NewLine}" +
                    $"Here's some related content:{Environment.NewLine}" +
                    $"{string.Join(Environment.NewLine, embeddings.Select(
                        x => $"{new string('`', 3)}{x.Filename} line {x.LineNumber}{Environment.NewLine}, class {x.Namespace}.{x.Class}" +
                        $"{x.Code}{Environment.NewLine}" +
                        $"" +
                        $"{new string('`', 3)}"))}";
                conversation.messages.Last().content = lastMsg;
                return lastMsg;
            }
            else return input;
        }

        public static async Task<List<CodeSnippet>> GetRelatedCodeFromEmbeddings(string key, string input, string filename)
        {
            var inputEmbedding = await OllamaEmbeddingsHelper.CreateEmbeddingsAsync(new List<string> { input }, key);

            // deserialize from C:\Users\maxhe\source\repos\CloneTest\MaxsAiTool\AiTool3\OpenAIEmbedFragged.embeddings.json
            var codeEmbedding = JsonConvert.DeserializeObject<List<Embedding>>(System.IO.File.ReadAllText(filename));
            var embeddingHelper = new EmbeddingHelper();

            var s = embeddingHelper.FindSimilarCodeSnippets(inputEmbedding[0], codeEmbedding, 5);
            List<CodeSnippet> result = new List<CodeSnippet>();
            foreach (var snippet in s)
            {
                var subInputEmbedding = await OllamaEmbeddingsHelper.CreateEmbeddingsAsync(new List<string> { snippet.Code }, key);
                var subs = embeddingHelper.FindSimilarCodeSnippets(subInputEmbedding[0], codeEmbedding, 5);
                result.AddRange(subs);
            }

            result = result.GroupBy(x => x.Code).Select(x => x.First()).ToList();
            return result;
        }

        public static async Task<List<Embedding>> CreateEmbeddingsAsync(List<string> texts, string apiKey, string apiUrl = "https://api.openai.com/v1/embeddings")
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var request = new
            {
                model = "text-embedding-3-large", // OpenAI's default embedding model
                input = texts
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            HttpResponseMessage? response = null;
            try
            {
                response = await client.PostAsync(apiUrl, content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                // Handle or log the exception
                Console.WriteLine($"An error occurred: {e.Message}");
                return new List<Embedding>();
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(responseBody);
            JsonElement root = doc.RootElement;

            var embeddings = new List<Embedding>();
            JsonElement dataArray = root.GetProperty("data");

            foreach (JsonElement dataElement in dataArray.EnumerateArray())
            {
                JsonElement embeddingArray = dataElement.GetProperty("embedding");
                var embedding = new List<float>();

                foreach (JsonElement value in embeddingArray.EnumerateArray())
                {
                    embedding.Add(value.GetSingle());
                }

                embeddings.Add(new Embedding { Value = embedding, Code = "" });
            }

            return embeddings;
        }

        private static string GetSourceFileName(Type type)
        {
            var attributes = type.GetCustomAttributes(typeof(DebuggerDisplayAttribute), false);
            if (attributes.Length > 0)
            {
                var debuggerDisplay = (DebuggerDisplayAttribute)attributes[0];
                return debuggerDisplay.Value;
            }

            // If DebuggerDisplay attribute is not available, try to get it from a method
            var method = type.GetMethods().FirstOrDefault();
            if (method != null)
            {
                try
                {
                    var fileName = method.GetMethodBody()?.LocalVariables.FirstOrDefault()?.ToString();
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        return System.IO.Path.GetFileName(fileName);
                    }
                }
                catch
                {
                    // Ignore any exceptions and return empty string
                }
            }

            return string.Empty;
        }

    }


}