using VSIXTest.Embeddings.Fragmenters;
using SharedClasses.Git;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;
using EnvDTE80;
using System.Collections.Generic;
using SharedClasses.Models;
using EnvDTE;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Text.Json;
using Microsoft.VisualStudio.Shell.Interop;
using System.Diagnostics;

namespace VSIXTest.Embeddings
{
    internal static class VsixEmbeddingsHelper
    {
        public static async Task CreateEmbeddingsAsync(DTE2 dte)
        {
            var shortcutManager = new ShortcutManager(dte);

            var allFiles = shortcutManager.GetAllFilesInSolution();

            allFiles = allFiles.Where(x => !x.EndsWith(".min.js")
            && !x.EndsWith(".png")).ToList();

            var lineFragmenter = new VsixLineFragmenter();

            var fragments = new List<CodeFragment>();

            foreach (var file in allFiles)
            {
                var content = File.ReadAllText(file);
                fragments.AddRange(lineFragmenter.FragmentCode(content, file));
            }


            var embeddingInputs = fragments.Select(x => $"{x.FilePath.Split('/').Last()} line {x.LineNumber} {(string.IsNullOrEmpty(x.Class) ? "" : $", class {x.Namespace}.{x.Class}")}:\n\n\n{x.Content}\n").ToList();

            using (var client = new HttpClient())
            {

                var embeddings = new List<VsixEmbedding>();

                var ctr = 0;

                foreach (var text in embeddingInputs)
                {
                    embeddings.AddRange(await GetEmbeddingsForText(text));
                    ctr++;
                    if(ctr%10==0)
                        Debug.WriteLine($"Embeddings for {ctr} code snippets created {ctr*100/embeddingInputs.Count}");
                }

                var solutionDir = Path.GetDirectoryName(dte.Solution.FullName);
                var embeddingsFilePath = Path.Combine(solutionDir, "code_embeddings.json");

                // Serialize and save the embeddings to file
                var jsonSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                var jsonString = JsonConvert.SerializeObject(embeddings, jsonSettings);
                File.WriteAllText(embeddingsFilePath, jsonString);
            }
        }

        private static async Task<List<VsixEmbedding>> GetEmbeddingsForText(string text)
        {
            var embeddings = new List<VsixEmbedding>();
            using (var client = new HttpClient())
            {
                //LocalAI.StartOllama(embeddingsModelName);
                var request = new
                {
                    model = "snowflake-arctic-embed2",
                    input = text
                };

                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                try
                {
                    response = await client.PostAsync("http://localhost:11434/api/embed", content);
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"An error occurred: {e.Message}");
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(responseBody))
                {
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("embeddings", out JsonElement embeddingsArray) &&
                        embeddingsArray.GetArrayLength() > 0)
                    {
                        var embedding = new List<float>();
                        foreach (JsonElement value in embeddingsArray[0].EnumerateArray())
                        {
                            embedding.Add(value.GetSingle());
                        }

                        embeddings.Add(new VsixEmbedding { Value = embedding, Code = text });
                    }
                }
            }

            return embeddings;

        }

        public static async Task<List<CodeSnippet>> GetRelatedCodeFromEmbeddings(DTE2 dte, string input)
        {
            var solutionDir = Path.GetDirectoryName(dte.Solution.FullName);
            var embeddingsFilePath = Path.Combine(solutionDir, "code_embeddings.json");

            if (!File.Exists(embeddingsFilePath))
            {
                MessageBox.Show("Embeddings file not found. Please check the path in settings, or use Embeddings -> Select Embedding..., and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new List<CodeSnippet>();
            }
            // Deserialize from the specified embeddings file
            var codeEmbedding = JsonConvert.DeserializeObject<List<VsixEmbedding>>(File.ReadAllText(embeddingsFilePath));

            var embeddingManager = new EmbeddingManager();

            var inputEmbeddings = await GetEmbeddingsForText(input);
            var s = embeddingManager.FindSimilarCodeSnippets(inputEmbeddings[0], codeEmbedding, 25);
            //List<CodeSnippet> result = new List<CodeSnippet>();
            //foreach (var snippet in s)
            //{
            //    //var subInputEmbedding = await CreateEmbeddingsAsync(new List<string> { snippet.Code }, key, embeddingsModelName);
            //    //var subs = embeddingManager.FindSimilarCodeSnippets(subInputEmbedding[0], codeEmbedding, 10);
            //    result.Add(snippet);
            //    //result.AddRange(subs);
            //}

            //result = result.GroupBy(x => x.Code).Select(x => x.First()).ToList();
            return null;// result;
        }

        internal static async Task GetEmbeddingsAsync(DTE2 dte, string prompt)
        {
            var embeddings = await GetRelatedCodeFromEmbeddings(dte, prompt);
            //embeddings = embeddings.GroupBy(x => new { x.Filename, x.LineNumber }).Select(x => x.First()).ToList();
            //embeddings = embeddings.OrderBy(x => x.Filename).ToList();

            // distinct embeddings by filename and linenumber

            // Display embeddings in a modal dialog and let user select
            //var selectedEmbeddings = ShowEmbeddingsSelectionDialog(embeddings);
            //
            //var lastMsg = $"{Environment.NewLine}{Environment.NewLine}" +
            //    $"Here's some related content:{Environment.NewLine}" +
            //    $"{string.Join(Environment.NewLine, selectedEmbeddings.Select(
            //        x => $"{new string('`', 3)}{x.Filename} line {x.LineNumber}{Environment.NewLine}, class {x.Namespace}.{x.Class}" +
            //        $"{x.Code}{Environment.NewLine}" +
            //        $"{new string('`', 3)}"))}" +
            //        $"{Environment.NewLine}{Environment.NewLine}" +
            //        $"{conversation.messages.Last().content}";
            //conversation.messages.Last().content = lastMsg;
            return;

        }
    }
}