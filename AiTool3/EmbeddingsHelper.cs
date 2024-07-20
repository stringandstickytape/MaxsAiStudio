using AiTool3.Conversations;
using AiTool3.Providers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using AiTool3.Providers.Embeddings.Fragmenters;

namespace AiTool3
{
    internal static class EmbeddingsHelper
    {
        public static void HandleSetEmbeddingsFileClick(SettingsSet currentSettings)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Embeddings JSON files (*.embeddings.json)|*.embeddings.json|All files (*.*)|*.*",
                Title = "Select Embeddings File",
                InitialDirectory = currentSettings.DefaultPath
            };

            openFileDialog.ShowDialog();

            if (string.IsNullOrEmpty(openFileDialog.FileName))
            {
                return;
            }

            currentSettings.EmbeddingsFilename = openFileDialog.FileName;
            AiTool3.SettingsSet.Save(currentSettings);
        }
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
            var embeddingHelper = new EmbeddingManager();

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

        public static async Task CreateEmbeddingsAsync(string apiKey)
        {
            // get a directory to open from the user
            var folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.ShowDialog();
            if (folderBrowserDialog.SelectedPath == "")
            {
                return;
            }

            // recursively find all cs files within that dir and subdirs
            var files = Directory.GetFiles(folderBrowserDialog.SelectedPath, "*.cs", SearchOption.AllDirectories);
            files = files.Where(files => !files.Contains(".g") && !files.Contains(".Assembly") && !files.Contains(".Designer")).ToArray();


            var htmlFiles = Directory.GetFiles(folderBrowserDialog.SelectedPath, "*.html", SearchOption.AllDirectories);
            var xmlFiles = Directory.GetFiles(folderBrowserDialog.SelectedPath, "*.xml", SearchOption.AllDirectories);
            var jsonFiles = Directory.GetFiles(folderBrowserDialog.SelectedPath, "*.json", SearchOption.AllDirectories);
            var jsFiles = Directory.GetFiles(folderBrowserDialog.SelectedPath, "*.js", SearchOption.AllDirectories);

            var csFragmenter = new CsFragmenter();
            var webCodeFragmenter = new WebCodeFragmenter();
            var lineFragmenter = new LineFragmenter();

            List<CodeFragment> fragments = new List<CodeFragment>();

            foreach (var file in jsFiles)
            {
                if (file.Contains("\\bin\\") || file.Contains("ThirdPartyJavascript") || file.Contains("JsonViewer")) continue;
                fragments.AddRange(webCodeFragmenter.FragmentJavaScriptCode(File.ReadAllText(file), file));
            }

            foreach (var file in xmlFiles)
            {
                fragments.AddRange(lineFragmenter.FragmentCode(File.ReadAllText(file), file));
            }
            foreach (var file in htmlFiles)
            {
                fragments.AddRange(webCodeFragmenter.FragmentCode(File.ReadAllText(file), file));
            }
            // remove all frags under 10 chars in length
            foreach (var file in jsonFiles)
            {
                // if the file is > 5k, skip it
                if (new FileInfo(file).Length > 5000 || file.Contains(".embeddings")) continue;

                fragments.AddRange(lineFragmenter.FragmentCode(File.ReadAllText(file), file));
            }


            // just pass json through, if it's less than 1K, else break it into chunks


            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Embeddings JSON file|*.embeddings.json",
                Title = "Save Embeddings JSON file"
            };
            saveFileDialog.ShowDialog();

            if (saveFileDialog.FileName == "")
            {
                return;
            }
            var frags = fragments.Where(x => x.Content.Length > 25).ToList();

            var embeddingInputs = frags.Select(x => @$"{x.FilePath.Split('/').Last()} line {x.LineNumber} {(string.IsNullOrEmpty(x.Class) ? "" : $", class {x.Namespace}.{x.Class}")}:

{x.Content}
").ToList();

            var embeddings = await OllamaEmbeddingsHelper.CreateEmbeddingsAsync(embeddingInputs, apiKey);

            for (var i = 0; i < frags.Count; i++)
            {
                embeddings[i].Code = frags[i].Content;
                embeddings[i].Filename = frags[i].FilePath;
                embeddings[i].LineNumber = frags[i].LineNumber;
                embeddings[i].Namespace = frags[i].Namespace;
                embeddings[i].Class = frags[i].Class;
            }

            // write the embeddings to the save file as json
            var json = System.Text.Json.JsonSerializer.Serialize(embeddings);
            File.WriteAllText(saveFileDialog.FileName, json);

            // show mb to say it's done
            MessageBox.Show("Embeddings created and saved");
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