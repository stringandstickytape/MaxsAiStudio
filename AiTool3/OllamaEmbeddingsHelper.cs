using AiTool3.Conversations;
using AiTool3.Providers;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;

namespace AiTool3
{
    internal static class OllamaEmbeddingsHelper
    {
        public static async Task<string> AddEmbeddingsToInput(Conversation conversation, SettingsSet currentSettings, string input, bool mustNotUseEmbedding)
        {
            if (!mustNotUseEmbedding)
            {
                var embeddingText = input + " ";
                var lbom = conversation.messages.Count > 1 ? conversation.messages[conversation.messages.Count - 2].content : "";

                if (string.IsNullOrEmpty(lbom) || lbom != input)
                {
                    embeddingText += lbom + " ";
                }
                var embeddings = await GetRelatedCodeFromEmbeddings("Ollama", embeddingText, currentSettings.EmbeddingsFilename, currentSettings.EmbeddingModel);

                // Display embeddings in a modal dialog and let user select
                var selectedEmbeddings = ShowEmbeddingsSelectionDialog(embeddings);

                var lastMsg = $"{Environment.NewLine}{Environment.NewLine}" +
                    $"Here's some related content:{Environment.NewLine}" +
                    $"{string.Join(Environment.NewLine, selectedEmbeddings.Select(
                        x => $"{new string('`', 3)}{x.Filename} line {x.LineNumber}{Environment.NewLine}, class {x.Namespace}.{x.Class}" +
                        $"{x.Code}{Environment.NewLine}" +
                        $"{new string('`', 3)}"))}" +
                        $"{Environment.NewLine}{Environment.NewLine}" +
                        $"{conversation.messages.Last().content}";
                conversation.messages.Last().content = lastMsg;
                return lastMsg;
            }
            else return input;
        }

        private static List<CodeSnippet> ShowEmbeddingsSelectionDialog(List<CodeSnippet> embeddings)
        {
            var selectedEmbeddings = new List<CodeSnippet>();
            bool isMouseClick = false; // Track mouse click events

            using (var form = new Form())
            {
                form.Text = "Select Embeddings (or close window to select no embeddings)";
                form.Size = new Size(800, 600);
                form.StartPosition = FormStartPosition.CenterScreen;

                // Create a split container
                var splitContainer = new SplitContainer
                {
                    Dock = DockStyle.Fill,
                    SplitterDistance = 50
                };
                form.Controls.Add(splitContainer);

                // Create the checkedListBox for filenames
                var checkedListBox = new CheckedListBox
                {
                    Dock = DockStyle.Fill,
                    CheckOnClick = false // Disable check on click
                };
                splitContainer.Panel1.Controls.Add(checkedListBox);

                // Create the textbox for content
                var contentTextBox = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical
                };
                splitContainer.Panel2.Controls.Add(contentTextBox);

                // Populate the checkedListBox
                foreach (var snippet in embeddings)
                {
                    checkedListBox.Items.Add($"{snippet.Filename} (Line {snippet.LineNumber})", true);
                }

                // Add event handlers
                checkedListBox.MouseDown += (sender, e) => { isMouseClick = true; };
                checkedListBox.MouseUp += (sender, e) => { isMouseClick = false; };
                checkedListBox.ItemCheck += (sender, e) =>
                {
                    if (!isMouseClick)
                    {
                        e.NewValue = e.CurrentValue; // Prevent toggling on item click
                    }
                    else
                    {
                        var clickPoint = checkedListBox.PointToClient(Cursor.Position);
                        var index = checkedListBox.IndexFromPoint(clickPoint);
                        if (index != ListBox.NoMatches)
                        {
                            var itemRect = checkedListBox.GetItemRectangle(index);
                            var checkBoxWidth = SystemInformation.MenuCheckSize.Width;
                            if (clickPoint.X > itemRect.X + checkBoxWidth)
                            {
                                e.NewValue = e.CurrentValue; // Prevent toggling on item text click
                            }
                        }
                    }
                };

                // Event handler for selection change
                checkedListBox.SelectedIndexChanged += (sender, e) =>
                {
                    if (checkedListBox.SelectedIndex != -1)
                    {
                        var selectedSnippet = embeddings[checkedListBox.SelectedIndex];
                        contentTextBox.Text = $"Filename: {selectedSnippet.Filename}\r\n" +
                                              $"Line: {selectedSnippet.LineNumber}\r\n" +
                                              $"Namespace: {selectedSnippet.Namespace}\r\n" +
                                              $"Class: {selectedSnippet.Class}\r\n\r\n" +
                                              $"Code:\r\n{selectedSnippet.Code}";
                    }
                };

                // Create a panel for buttons
                var buttonPanel = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50
                };
                form.Controls.Add(buttonPanel);

                // Create "Accept Embeddings" button
                var okButton = new Button
                {
                    Text = "Accept Embeddings",
                    DialogResult = DialogResult.OK,
                    Width = 150,
                    Height = 40,
                    Location = new Point(buttonPanel.Width - 160, 5)
                };
                buttonPanel.Controls.Add(okButton);

                form.AcceptButton = okButton;

                if (form.ShowDialog() == DialogResult.OK)
                {
                    for (int i = 0; i < checkedListBox.Items.Count; i++)
                    {
                        if (checkedListBox.GetItemChecked(i))
                        {
                            selectedEmbeddings.Add(embeddings[i]);
                        }
                    }
                }
            }

            return selectedEmbeddings;
        }

        public static async Task<List<CodeSnippet>> GetRelatedCodeFromEmbeddings(string key, string input, string filename, string embeddingsModelName)
        {
            var inputEmbedding = await CreateEmbeddingsAsync(new List<string> { input }, key, embeddingsModelName);


            if (!File.Exists(filename))
            {
                MessageBox.Show("Embeddings file not found. Please check the path in settings, or use Embeddings -> Select Embedding..., and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new List<CodeSnippet>();
            }
            // Deserialize from the specified embeddings file
            var codeEmbedding = JsonConvert.DeserializeObject<List<Embedding>>(System.IO.File.ReadAllText(filename));

            var embeddingManager = new EmbeddingManager();

            var s = embeddingManager.FindSimilarCodeSnippets(inputEmbedding[0], codeEmbedding, 5);
            List<CodeSnippet> result = new List<CodeSnippet>();
            foreach (var snippet in s)
            {
                var subInputEmbedding = await CreateEmbeddingsAsync(new List<string> { snippet.Code }, key, embeddingsModelName);
                var subs = embeddingManager.FindSimilarCodeSnippets(subInputEmbedding[0], codeEmbedding, 5);
                result.AddRange(subs);
            }

            result = result.GroupBy(x => x.Code).Select(x => x.First()).ToList();
            return result;
        }

        public static async Task<List<Embedding>> CreateEmbeddingsAsync(List<string> texts, string apiKey, string embeddingsModelName, string apiUrl = "http://localhost:11434/api/embeddings")
        {
            using var client = new HttpClient();

            var embeddings = new List<Embedding>();

            foreach (var text in texts)
            {
                LocalAI.StartOllama(embeddingsModelName);
                var request = new
                {
                    model = embeddingsModelName,
                    prompt = text
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
                    Console.WriteLine($"An error occurred: {e.Message}");
                    return new List<Embedding>();
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(responseBody);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("embedding", out JsonElement embeddingArray))
                {
                    var embedding = new List<float>();

                    foreach (JsonElement value in embeddingArray.EnumerateArray())
                    {
                        embedding.Add(value.GetSingle());
                    }

                    embeddings.Add(new Embedding { Value = embedding, Code = text });
                }
            }

            return embeddings;
        }
    }
}