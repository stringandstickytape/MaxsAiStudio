using AiTool3.Embeddings.Fragmenters;
using AiTool3.ExtensionMethods;

namespace AiTool3.Embeddings
{
    internal static class EmbeddingsHelper
    {


        public static async Task CreateEmbeddingsAsync(string apiKey, MaxsAiStudio maxsAiStudio)
        {
            // get a directory to open from the user
            var folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Select a root directory to generate embeddings from.  Respects .gitignore; accepts only .json, .cs, .html, .js, .xml, .jsx for now.";
            folderBrowserDialog.UseDescriptionForTitle = true;
            folderBrowserDialog.ShowDialog();
            if (folderBrowserDialog.SelectedPath == "")
            {
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = $"{maxsAiStudio.CurrentSettings.EmbeddingModel} Embeddings JSON file|*.{maxsAiStudio.CurrentSettings.EmbeddingModel}.embeddings.json",
                Title = "Save Embeddings JSON file",
                InitialDirectory = Path.Combine(Environment.CurrentDirectory, "Embeddings")
            };
            saveFileDialog.ShowDialog();

            if (saveFileDialog.FileName == "")
            {
                return;
            }

            maxsAiStudio.ShowWorking("Generating Embeddings", maxsAiStudio.CurrentSettings.SoftwareToyMode);

            string gitignore = null;
            GitIgnoreFilterManager gitIgnoreFilterManager = new GitIgnoreFilterManager("");
            // check for a .gitignore
            if (File.Exists(Path.Combine(folderBrowserDialog.SelectedPath, ".gitignore")))
            {
                gitignore = File.ReadAllText(Path.Combine(folderBrowserDialog.SelectedPath, ".gitignore"));
                gitIgnoreFilterManager = new GitIgnoreFilterManager(gitignore);
            }

            // recursively find all cs files within that dir and subdirs
            var files = Directory.GetFiles(folderBrowserDialog.SelectedPath, "*.cs", SearchOption.AllDirectories);



            if (gitignore != null)
            {
                files = gitIgnoreFilterManager.FilterNonIgnoredPaths(files.ToList()).ToArray();
            }


            //files = files.Where(files => !files.Contains(".g") && !files.Contains(".Assembly") && !files.Contains(".Designer")).ToArray();


            var htmlFiles = gitIgnoreFilterManager.FilterNonIgnoredPaths(Directory.GetFiles(folderBrowserDialog.SelectedPath, "*.html", SearchOption.AllDirectories).ToList());
            var xmlFiles = gitIgnoreFilterManager.FilterNonIgnoredPaths(Directory.GetFiles(folderBrowserDialog.SelectedPath, "*.xml", SearchOption.AllDirectories).ToList());

            var x = Directory.GetFiles(folderBrowserDialog.SelectedPath, "*.json", SearchOption.AllDirectories).ToList();
            var jsonFiles = gitIgnoreFilterManager.FilterNonIgnoredPaths(x);
            var jsFiles = gitIgnoreFilterManager.FilterNonIgnoredPaths(Directory.GetFiles(folderBrowserDialog.SelectedPath, "*.js", SearchOption.AllDirectories)
                .Union(Directory.GetFiles(folderBrowserDialog.SelectedPath, "*.jsx", SearchOption.AllDirectories))
                .Where(x => !x.Contains(".min.js")).ToList());

            var csFragmenter = new CsFragmenter();
            var webCodeFragmenter = new WebCodeFragmenter();
            var lineFragmenter = new LineFragmenter();

            List<CodeFragment> fragments = new List<CodeFragment>();

            foreach (var file in jsFiles)
            {
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
            foreach (var file in files)
            {
                fragments.AddRange(csFragmenter.FragmentCode(File.ReadAllText(file), file));
            }
            // remove all frags under 10 chars in length
            foreach (var file in jsonFiles)
            {
                // if the file is > 5k, skip it.  Also ignore our own embeddings files :)
                if (new FileInfo(file).Length > 5000 || file.Contains(".embeddings.json")) continue;

                fragments.AddRange(lineFragmenter.FragmentCode(File.ReadAllText(file), file));
            }


            // just pass json through, if it's less than 1K, else break it into chunks



            var frags = fragments.Where(x => x.Content.Length > 25).ToList();

            var embeddingInputs = frags.Select(x => @$"{x.FilePath.Split('/').Last()} line {x.LineNumber} {(string.IsNullOrEmpty(x.Class) ? "" : $", class {x.Namespace}.{x.Class}")}:

{x.Content}
").ToList();

            var embeddings = await OllamaEmbeddingsHelper.CreateEmbeddingsAsync(embeddingInputs, apiKey, maxsAiStudio.CurrentSettings.EmbeddingModel);

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

            maxsAiStudio.HideWorking();
        }
    }
}