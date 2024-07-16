using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Helpers;
using AiTool3.Providers.Embeddings;
using AiTool3.Providers.Embeddings.Fragmenters;
using AiTool3.Settings;
using AiTool3.Snippets;
using AiTool3.Topics;
using AiTool3.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AiTool3
{
    public partial class Form2
    {
        private static void AddSpecial(ToolStripMenuItem specialsMenu, string l, EventHandler q)
        {
            var reviewCodeMenuItem = CreateMenuItem(l, ref specialsMenu);
            reviewCodeMenuItem.Click += q;
        }

        private void InitialiseMenus()
        {
            var fileMenu = CreateMenu("File");

            var quitMenuItem = CreateMenuItem("Quit", ref fileMenu);

            quitMenuItem.Click += (s, e) =>
            {
                Application.Exit();
            };

            var editMenu = CreateMenu("Edit");


            // add settings option.  When chosen, invokes SettingsForm modally
            var settingsMenuItem = CreateMenuItem("Settings", ref editMenu);

            settingsMenuItem.Click += (s, e) =>
            {
                var settingsForm = new SettingsForm(CurrentSettings);
                var result = settingsForm.ShowDialog();

                if (result == DialogResult.OK)
                {
                    CurrentSettings = settingsForm.NewSettings;
                    cbUseEmbeddings.Checked = CurrentSettings.UseEmbeddings;
                    AiTool3.SettingsSet.Save(CurrentSettings);
                }
            };

            // add settings option.  When chosen, invokes SettingsForm modally
            var licensesMenuItem = CreateMenuItem("Licenses", ref editMenu);

            licensesMenuItem.Click += (s, e) =>
            {
                var licensesForm = new LicensesForm(AssemblyHelper.GetEmbeddedAssembly("AiTool3.UI.Licenses.txt"))
                    .ShowDialog();
            };


            menuBar.Items.Add(fileMenu);
            menuBar.Items.Add(editMenu);
            CreateSpecialsMenu();
            CreateTemplatesMenu();

            // add a specials menu
            
        }

        private Dictionary<string, TemplateMenuItem> templateMenuItems = new Dictionary<string, TemplateMenuItem>();
        private void CreateTemplatesMenu()
        {
            var templatesMenu = CreateMenu("Templates");

            foreach (var topic in TopicSet.Topics.OrderBy(x => x.Name))
            {
                var categoryMenuItem = CreateMenuItem(topic.Name, ref templatesMenu);

                foreach (var template in topic.Templates.Where(x => x.SystemPrompt != null).OrderBy(x => x.TemplateName))
                {
                    var templateMenuItem = (TemplateMenuItem)CreateMenuItem(template.TemplateName, ref categoryMenuItem, true);
                    templateMenuItems[template.TemplateName] = templateMenuItem;

                    templateMenuItem.Click += async (s, e) =>
                    {
                        await SelectTemplate(topic.Name, template.TemplateName);
                    };
                    templateMenuItem.EditClicked += (s, e) =>
                    {
                        EditAndSaveTemplate(template, false, topic.Name);
                    };

                }

                // at the end of each category, add a separator then an Add... option
                categoryMenuItem.DropDownItems.Add(new ToolStripSeparator());
                var addMenuItem = CreateMenuItem("Add...", ref categoryMenuItem);
                addMenuItem.Click += (s, e) =>
                {
                    // s is a ToolStripMenuItem
                    var topicName = ((ToolStripMenuItem)s!).OwnerItem!.Text;

                    var template = new ConversationTemplate("System Prompt", "Initial Prompt");
                    
                    EditAndSaveTemplate(template, true, topicName);

                    var templateMenuItem = (TemplateMenuItem)CreateMenuItem(template.TemplateName, ref categoryMenuItem, true);
                    templateMenuItem.Click += async (s, e) =>
                    {
                        await SelectTemplate(topicName, template.TemplateName);
                    };
                    templateMenuItem.EditClicked += (s, e) =>
                    {
                        EditAndSaveTemplate(template, false, topicName);
                    };

                    // add to TopicSet.Topics
                    var topic = TopicSet.Topics.First(x => x.Name == topicName);
                    topic.Templates.Add(template);
                };
            }


            templatesMenu.DropDownItems.Add(new ToolStripSeparator());
            var addMenuItem2 = CreateMenuItem("Add...", ref templatesMenu);
            addMenuItem2.Click += (s, e) =>
            {
                // request a single string from the user for category name, w ok and cancel buttons
                var form = new Form();
                var tb = new TextBox();
                var okButton = new Button();
                var cancelButton = new Button();

                form.Text = "Add Category";
                form.Size = new System.Drawing.Size(400, 150);
                form.StartPosition = FormStartPosition.CenterScreen;

                tb.Location = new System.Drawing.Point(50, 10);
                tb.Size = new System.Drawing.Size(300, 20);
                tb.TabIndex = 0;
                form.Controls.Add(tb);

                okButton.Text = "OK";
                okButton.Location = new System.Drawing.Point(50, 50);
                okButton.Size = new System.Drawing.Size(75, 23);
                okButton.DialogResult = DialogResult.OK;
                form.Controls.Add(okButton);

                cancelButton.Text = "Cancel";
                cancelButton.Location = new System.Drawing.Point(150, 50);
                    
                cancelButton.Size = new System.Drawing.Size(75, 23);
                cancelButton.DialogResult = DialogResult.Cancel;

                form.Controls.Add(cancelButton);

                form.AcceptButton = okButton;

                form.CancelButton = cancelButton;

                var result = form.ShowDialog();

                if(result == DialogResult.OK)
                {
                    var newCategoryName = tb.Text;
                    TopicSet.Topics.Add(new Topic(Guid.NewGuid().ToString(),newCategoryName));
                    TopicSet.Save();

                    // delete and recreate the templates menu
                    // remove any menu called Templates
                    

                    // find all the existing Templates named menus
                    var templatesMenus = menuBar.Items.OfType<ToolStripMenuItem>().Where(x => x.Text == "Templates").ToList();
                    foreach (var menu in templatesMenus)
                    {
                        menuBar.Items.Remove(menu);
                    }

                    templateMenuItems.Clear();
                    CreateTemplatesMenu();
                    // move it one before the end

                    



                }    
                








            };
            
            menuBar.Items.Add(templatesMenu);
        }

        private void CreateSpecialsMenu()
        {
            var menuText = "Specials";
            ToolStripMenuItem specialsMenu = CreateMenu(menuText);


            AddSpecial(specialsMenu, "Create embedding", async (s, e) =>
            {
                await CreateEmbeddingsAsync(CurrentSettings.EmbeddingKey);
            });

            AddSpecial(specialsMenu, "Pull Readme and update from latest diff", async (s, e) =>
            {
                AiResponse response = await SpecialsHelper.GetReadmeResponses((Model)cbEngine.SelectedItem!);
                var snippets = snippetManager.FindSnippets(response.ResponseText);

                try
                {
                    var code = snippets.Snippets.First().Content;
                    code = SnippetHelper.StripFirstAndLastLine(code);
                    File.WriteAllText(@"C:\Users\maxhe\source\repos\CloneTest\MaxsAiTool\README.md", code);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error writing to file: {ex.Message}");
                }
            });

            AddSpecial(specialsMenu, "Review Code", async (s, e) =>
            {
                SpecialsHelper.ReviewCode(out string userMessage);
                await chatWebView.SetUserPrompt(userMessage);
            });
            AddSpecial(specialsMenu, "Rewrite Summaries", async (s, e) =>
            {
                await ConversationManager.RegenerateSummary((Model)cbEngine.SelectedItem!, CurrentSettings.GenerateSummariesUsingLocalAi, dgvConversations, "*", CurrentSettings);
            });

            AddSpecial(specialsMenu, "Autosuggest", async (s, e) =>
            {
                var autoSuggestForm = await ConversationManager.Autosuggest((Model)cbEngine.SelectedItem!, CurrentSettings.GenerateSummariesUsingLocalAi, dgvConversations);
                autoSuggestForm.StringSelected += AutoSuggestStringSelected;
            });

            AddSpecial(specialsMenu, "Autosuggest (Fun)", async (s, e) =>
            {
                var autoSuggestForm = await ConversationManager.Autosuggest((Model)cbEngine.SelectedItem!, CurrentSettings.GenerateSummariesUsingLocalAi, dgvConversations, true);
                autoSuggestForm.StringSelected += AutoSuggestStringSelected;
            });

            AddSpecial(specialsMenu, "Autosuggest (User-Specified)", async (s, e) =>
            {
                var userInputForm = new AutoSuggestUserInput();

                var prefix = "you are a bot who makes ";
                var suffix = " suggestions on how a user might proceed with a conversation.";
                userInputForm.Controls["label1"]!.Text = prefix;
                userInputForm.Controls["label2"]!.Text = suffix;
                var result = userInputForm.ShowDialog();

                if (result == DialogResult.OK)
                {
                    var userAutoSuggestPrompt = userInputForm.Controls["tbAutoSuggestUserInput"]!.Text;

                    userAutoSuggestPrompt = $"{prefix}{userAutoSuggestPrompt}{suffix}";

                    var autoSuggestForm = await ConversationManager.Autosuggest((Model)cbEngine.SelectedItem!, CurrentSettings.GenerateSummariesUsingLocalAi, dgvConversations, true, userAutoSuggestPrompt);
                    autoSuggestForm.StringSelected += AutoSuggestStringSelected;
                }


            });

            AddSpecial(specialsMenu, "Toggle old input box visibility", (s, e) =>
            {
                splitContainer4.Panel1Collapsed = !splitContainer4.Panel1Collapsed;
            });

            AddSpecial(specialsMenu, "Toggle conversation browsers", (s, e) =>
            {
                splitContainer1.Panel1Collapsed = !splitContainer1.Panel1Collapsed;
            });
            AddSpecial(specialsMenu, "Test Snippets Code", async (s, e) =>
            {
                //var x = GetAllSnippets(ConversationManager.PreviousCompletion, ConversationManager.CurrentConversation, snippetManager);
                //
                //// create a new form
                //var f = new Form();
                //
                //// add a listbox with the snippets
                //var lb = new ListBox();
                //lb.Dock = DockStyle.Fill;
                //f.Controls.Add(lb);
                //lb.Items.AddRange(x.Select(x => x.Content).ToArray());
                //f.Show();
                SnippetHelper.ShowSnippets(GetAllSnippets(ConversationManager.PreviousCompletion, ConversationManager.CurrentConversation, snippetManager));
            });

            AddSpecial(specialsMenu, "Open-Source Licenses", (s, e) =>
            {
                ShowOpenSourceLicenses();
            });


            menuBar.Items.Add(specialsMenu);
        }

        private static async Task CreateEmbeddingsAsync(string apiKey)
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
            var jsFiles = Directory.GetFiles(folderBrowserDialog.SelectedPath, "*.js", SearchOption.AllDirectories);
            
            var csFragmenter = new CsFragmenter();
            var webCodeFragmenter = new WebCodeFragmenter();
            var xmlFragmenter = new XmlCodeFragmenter();
            List<CodeFragment> fragments = new List<CodeFragment>();

            foreach (var file in jsFiles)
            {
                if (file.Contains("\\bin\\") || file.Contains("ThirdPartyJavascript") || file.Contains("JsonViewer")) continue;
                fragments.AddRange(webCodeFragmenter.FragmentJavaScriptCode(File.ReadAllText(file), file));
            }

            foreach (var file in xmlFiles)
            {
                fragments.AddRange(xmlFragmenter.FragmentCode(File.ReadAllText(file), file));
            }
            foreach (var file in htmlFiles)
            {
                fragments.AddRange(webCodeFragmenter.FragmentCode(File.ReadAllText(file), file));
            }
            // remove all frags under 10 chars in length
            foreach (var file in files)
            {
                fragments.AddRange(csFragmenter.FragmentCode(File.ReadAllText(file), file));
            }

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
            var json = JsonSerializer.Serialize(embeddings);
            File.WriteAllText(saveFileDialog.FileName, json);

            // write ziped to file as well
            //var zipPath = saveFileDialog.FileName.Replace(".embeddings.json", ".embeddings.zip");
            //using (var archive = System.IO.Compression.ZipFile.Open(zipPath, System.IO.Compression.ZipArchiveMode.Create))
            //{
            //    var entry = archive.CreateEntry("embeddings.json");
            //    using (var stream = entry.Open())
            //    using (var writer = new StreamWriter(stream))
            //    {
            //        writer.Write(json);
            //    }
            //}



            // show mb to say it's done
            MessageBox.Show("Embeddings created and saved");
        }

        private static void ShowOpenSourceLicenses()
        {
            var licensesForm = new Form
            {
                Text = "Licenses",
                Size = new Size(400, 300),
                StartPosition = FormStartPosition.CenterParent
            };

            var textBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Text = "licenses go here lol"
            };

            var okButton = new Button
            {
                Text = "OK",
                Dock = DockStyle.Bottom
            };

            okButton.Click += (sender, e) => licensesForm.Close();

            licensesForm.Controls.Add(textBox);
            licensesForm.Controls.Add(okButton);

            licensesForm.ShowDialog();
        }

        private static ToolStripMenuItem CreateMenu(string menuText)
        {
            var menu = new ToolStripMenuItem(menuText);
            menu.BackColor = Color.Black;
            menu.ForeColor = Color.White;
            return menu;
        }

        /*
         *             var quitMenuItem = new ToolStripMenuItem("Quit");
            quitMenuItem.ForeColor = Color.White;
            quitMenuItem.BackColor = Color.Black;
        */
        private static ToolStripMenuItem CreateMenuItem(string text, ref ToolStripMenuItem dropDownItems, bool isTemplate = false)
        {
            if (isTemplate)
                return new TemplateMenuItem(text, ref dropDownItems);

            var retVal = new ToolStripMenuItem(text);
            dropDownItems.DropDownItems.Add(retVal);
            return retVal;
        }

        public static List<Snippet> GetAllSnippets(CompletionMessage currentMessage, BranchedConversation conversation, SnippetManager snippetManager)
        {
            List<Snippet> allSnippets = new List<Snippet>();
            List<CompletionMessage> parentNodes = conversation.GetParentNodeList(currentMessage.Guid);

            foreach (var node in parentNodes)
            {
                SnippetSet snippetSet = snippetManager.FindSnippets(node.Content);
                allSnippets.AddRange(snippetSet.Snippets);
            }

            return allSnippets;
        }
    }

    public class FileTexts
    {
        public string Filename { get; set; }
        public string Content { get; set; }

    }

    public class Embedding
    {
        public string Code { get; set; }
        public List<float> Value { get; set; }
        public string Filename { get; set; }
        public int LineNumber { get; set; }
        public string Namespace { get; set; }
        public string Class { get; set; }
    }
}
