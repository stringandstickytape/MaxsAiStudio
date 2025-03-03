﻿using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.AiServices;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;
using SharedClasses.Providers;

namespace AiTool3.Embeddings
{
    internal static class OllamaEmbeddingsHelper
    {
        public static async Task<string> AddEmbeddingsToInput(LinearConversation conversation, ApiSettings apiSettings, string input, bool mustNotUseEmbedding)
        {
            if (!mustNotUseEmbedding)
            {
                var embeddingText = input + " ";
                var lbom = conversation.messages.Count > 1 ? conversation.messages[conversation.messages.Count - 2].content : "";

                if (string.IsNullOrEmpty(lbom) || lbom != input)
                {
                    embeddingText += lbom + " ";
                }
                var embeddings = await GetRelatedCodeFromEmbeddings("Ollama", embeddingText, apiSettings.EmbeddingsFilename, apiSettings.EmbeddingModel);
                embeddings = embeddings.GroupBy(x => new { x.Filename, x.LineNumber }).Select(x => x.First()).ToList();
                embeddings = embeddings.OrderBy(x => x.Filename).ToList();

                // distinct embeddings by filename and linenumber

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

        private static int _previouslySelectedIndex = -1;
        private static List<CodeSnippet> ShowEmbeddingsSelectionDialog(List<CodeSnippet> embeddings)
        {
            var selectedEmbeddings = new List<CodeSnippet>();
            bool isMouseClick = false; // Track mouse click events

            using (var form = new Form())
            {
                form.Text = $"Select Embeddings (or close window to select no embeddings) {embeddings.Sum(x => x.Code.Split('\n').Length)} lines selected";
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
                foreach (var snippet in embeddings.OrderBy(x => x.Filename))
                {
                    checkedListBox.Items.Add($"{snippet.Filename.Split('\\').Last()} (Line {snippet.LineNumber})", true);
                }

                // Add event handlers
                checkedListBox.MouseDown += (sender, e) => { isMouseClick = true; };
                checkedListBox.MouseUp += (sender, e) => { isMouseClick = false; };
                checkedListBox.ItemCheck += (sender, e) =>
                {
                    // Update the form title with the new line count
                    UpdateFormTitle(form, embeddings, checkedListBox, e.Index, e.NewValue == CheckState.Checked);
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

                    // if the click was on the checkbox and the selected index has actually changed...
                    if (isMouseClick && checkedListBox.SelectedIndex != _previouslySelectedIndex)
                    {
                        // toggle the checkbox
                        var clickPoint = checkedListBox.PointToClient(Cursor.Position);
                        var index = checkedListBox.IndexFromPoint(clickPoint);
                        if (index != ListBox.NoMatches)
                        {
                            // check the item

                            var itemRect = checkedListBox.GetItemRectangle(index);
                            var checkBoxWidth = SystemInformation.MenuCheckSize.Width;
                            if (clickPoint.X < itemRect.X + checkBoxWidth)
                            {
                                checkedListBox.SetItemChecked(index, !checkedListBox.GetItemChecked(index));
                            }
                        }
                        _previouslySelectedIndex = checkedListBox.SelectedIndex;
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
            var codeEmbedding = JsonConvert.DeserializeObject<List<Embedding>>(File.ReadAllText(filename));

            var embeddingManager = new EmbeddingManager();

            var s = embeddingManager.FindSimilarCodeSnippets(inputEmbedding[0], codeEmbedding, 25);
            List<CodeSnippet> result = new List<CodeSnippet>();
            foreach (var snippet in s)
            {
                //var subInputEmbedding = await CreateEmbeddingsAsync(new List<string> { snippet.Code }, key, embeddingsModelName);
                //var subs = embeddingManager.FindSimilarCodeSnippets(subInputEmbedding[0], codeEmbedding, 10);
                result.Add(snippet);
                //result.AddRange(subs);
            }

            result = result.GroupBy(x => x.Code).Select(x => x.First()).ToList();
            return result;
        }

        public static async Task<List<Embedding>> CreateEmbeddingsAsync(List<string> texts, string apiKey, string embeddingsModelName, string apiUrl = "http://localhost:11434/api/embed")
        {
            using var client = new HttpClient();

            var embeddings = new List<Embedding>();

            foreach (var text in texts)
            {
                LocalAI.StartOllama(embeddingsModelName);
                var request = new
                {
                    model = embeddingsModelName,
                    input = text
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

                if (root.TryGetProperty("embeddings", out JsonElement embeddingsArray) &&
                    embeddingsArray.GetArrayLength() > 0)
                {
                    var embedding = new List<float>();
                    foreach (JsonElement value in embeddingsArray[0].EnumerateArray())
                    {
                        embedding.Add(value.GetSingle());
                    }

                    embeddings.Add(new Embedding { Value = embedding, Code = text });
                }
            }

            return embeddings;
        }

        private static void UpdateFormTitle(Form form, List<CodeSnippet> embeddings, CheckedListBox checkedListBox, int changedIndex, bool isChecked)
        {
            int totalLines = 0;
            for (int i = 0; i < checkedListBox.Items.Count; i++)
            {
                if (i == changedIndex ? isChecked : checkedListBox.GetItemChecked(i))
                {
                    totalLines += embeddings[i].Code.Split('\n').Length;
                }
            }
            form.Text = $"Select Embeddings (or close window to select no embeddings) {totalLines} lines selected";
        }
    }
}