using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Helpers;
using AiTool3.Providers.Embeddings;
using AiTool3.Providers.Embeddings.Fragmenters;
using AiTool3.Settings;
using AiTool3.Snippets;
using AiTool3.Topics;
using AiTool3.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AiTool3
{
    public partial class MaxsAiStudio
    {
        private void InitialiseMenus()
        {
            var fileMenu = MenuItemHelper.CreateMenu("File");

            var quitMenuItem = MenuItemHelper.CreateMenuItem("Quit", ref fileMenu);

            quitMenuItem.Click += (s, e) =>
            {
                Application.Exit();
            };

            var editMenu = MenuItemHelper.CreateMenu("Edit");


            // add settings option.  When chosen, invokes SettingsForm modally
            var settingsMenuItem = MenuItemHelper.CreateMenuItem("Settings", ref editMenu);

            settingsMenuItem.Click += async (s, e) =>
            {
                var settingsForm = new SettingsForm(CurrentSettings);
                var result = settingsForm.ShowDialog();

                if (result == DialogResult.OK)
                {
                    CurrentSettings = settingsForm.NewSettings;
                    cbUseEmbeddings.Checked = CurrentSettings.UseEmbeddings;
                    AiTool3.SettingsSet.Save(CurrentSettings);
                    await chatWebView.UpdateSendButtonColor(CurrentSettings.UseEmbeddings);
                }
            };

            var setEmbeddingsFile = MenuItemHelper.CreateMenuItem("Set Embeddings File", ref editMenu);

            setEmbeddingsFile.Click += (s, e) =>
            {
                var openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Embeddings JSON files (*.embeddings.json)|*.embeddings.json|All files (*.*)|*.*";
                openFileDialog.Title = "Select Embeddings File";
                openFileDialog.InitialDirectory = CurrentSettings.DefaultPath;
                openFileDialog.ShowDialog();

                if (openFileDialog.FileName == "")
                {
                    return;
                }

                CurrentSettings.EmbeddingsFilename = openFileDialog.FileName;
                AiTool3.SettingsSet.Save(CurrentSettings);

            };

            // add settings option.  When chosen, invokes SettingsForm modally
            var licensesMenuItem = MenuItemHelper.CreateMenuItem("Licenses", ref editMenu);

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

        private async Task SelectNoneTemplate()
        {
            templateManager.ClearTemplate();
            await chatWebView.Clear(CurrentSettings);
            await chatWebView.UpdateSystemPrompt("");
            await chatWebView.SetUserPrompt("");

            // Update menu items
            foreach (var item in templateManager.templateMenuItems.Values)
            {
                item.IsSelected = false;
            }
            menuBar.Refresh(); // Force redraw of the menu
        }

        
        private void CreateTemplatesMenu()
        {
            templateManager.templateMenuItems.Clear();

            var templatesMenu = MenuItemHelper.CreateMenu("Templates");

            // Add "None" option at the top
            var noneMenuItem = MenuItemHelper.CreateMenuItem("None", ref templatesMenu);
            noneMenuItem.Click += async (s, e) =>
            {
                await SelectNoneTemplate();
            };

            // Add separator after "None"
            templatesMenu.DropDownItems.Add(new ToolStripSeparator());

            foreach (var category in templateManager.TemplateSet.Categories.OrderBy(x => x.Name))
            {
                var categoryMenuItem = MenuItemHelper.CreateMenuItem(category.Name, ref templatesMenu);

                foreach (var template in category.Templates.Where(x => x.SystemPrompt != null).OrderBy(x => x.TemplateName))
                {
                    var templateMenuItem = (TemplateMenuItem)MenuItemHelper.CreateMenuItem(template.TemplateName, ref categoryMenuItem, true);
                    templateManager.templateMenuItems[template.TemplateName] = templateMenuItem;

                    templateMenuItem.Click += async (s, e) =>
                    {
                        // if shift is held:
                        if (ModifierKeys == Keys.Shift)
                        {
                            if(MessageBox.Show("Are you sure you want to delete this template?", "Delete Template", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                category.Templates.Remove(template);
                                templateManager.TemplateSet.Save();
                                RecreateTemplatesMenu();
                            }
                        }
                        else await SelectTemplate(category.Name, template.TemplateName);
                    };
                    templateMenuItem.EditClicked += (s, e) =>
                    {
                        templateManager.EditAndSaveTemplate(template, false, category.Name);
                        RecreateTemplatesMenu();
                    };

                }

                // at the end of each category, add a separator then an Add... option
                categoryMenuItem.DropDownItems.Add(new ToolStripSeparator());
                var addMenuItem = MenuItemHelper.CreateMenuItem("Add...", ref categoryMenuItem);
                addMenuItem.Click += (s, e) =>
                {
                    // s is a ToolStripMenuItem
                    var templateName = ((ToolStripMenuItem)s!).OwnerItem!.Text;

                    var template = new ConversationTemplate("System Prompt", "Initial Prompt");

                    templateManager.EditAndSaveTemplate(template, true, templateName);

                    RecreateTemplatesMenu();

                };
            }


            templatesMenu.DropDownItems.Add(new ToolStripSeparator());
            var addMenuItem2 = MenuItemHelper.CreateMenuItem("Add...", ref templatesMenu);
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
                    templateManager.TemplateSet.Categories.Add(new Topic(Guid.NewGuid().ToString(), newCategoryName));
                    templateManager.TemplateSet.Save();

                    // find all the existing Templates named menus
                    RecreateTemplatesMenu();
                    

                }









            };
            
            menuBar.Items.Add(templatesMenu);
        }

        private void RecreateTemplatesMenu()
        {
            menuBar.Items.OfType<ToolStripMenuItem>().Where(x => x.Text == "Templates").ToList().ForEach(x => menuBar.Items.Remove(x));

            

            CreateTemplatesMenu();
        }

        private void CreateSpecialsMenu()
        {
            var menuText = "Specials";
            ToolStripMenuItem specialsMenu = MenuItemHelper.CreateMenu(menuText);

            MenuItemHelper.AddSpecials(specialsMenu,
                new List<LabelAndEventHander>
                {
                    new LabelAndEventHander("Create embedding", async (s, e) =>
                    {
                        await CreateEmbeddingsAsync(CurrentSettings.EmbeddingKey);
                    }),

                    new LabelAndEventHander("Pull Readme and update from latest diff", async (s, e) =>
                    {
                        AiResponse response = await SpecialsHelper.GetReadmeResponses((Model)cbSummaryEngine.SelectedItem!);
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
                    }),

                    new LabelAndEventHander("Review Code", async (s, e) =>
                    {
                        SpecialsHelper.ReviewCode(out string userMessage);
                        await chatWebView.SetUserPrompt(userMessage);
                    }),

                    new LabelAndEventHander("Rewrite Summaries", async (s, e) =>
                    {
                        await ConversationManager.RegenerateSummary((Model)cbSummaryEngine.SelectedItem!, CurrentSettings.GenerateSummariesUsingLocalAi, dgvConversations, "*", CurrentSettings);
                    }),

                    new LabelAndEventHander("Transcribe MP4", async (s, e) =>
                    {
                        var openFileDialog = new OpenFileDialog();
                        openFileDialog.Filter = "MP4 files (*.mp4)|*.mp4|All files (*.*)|*.*";

                        openFileDialog.ShowDialog();

                        if (openFileDialog.FileName == "")
                        {
                            return;
                        }
                        await _fileAttachmentManager.TranscribeMP4(openFileDialog.FileName, chatWebView);
                    }),

                    new LabelAndEventHander("Autosuggest", async (s, e) =>
                    {
                        var autoSuggestForm = await ConversationManager.Autosuggest((Model)cbSummaryEngine.SelectedItem!, CurrentSettings.GenerateSummariesUsingLocalAi, dgvConversations);
                        autoSuggestForm.StringSelected += AutoSuggestStringSelected;
                    }),

                    new LabelAndEventHander("Autosuggest (Fun)", async (s, e) =>
                    {
                        var autoSuggestForm = await ConversationManager.Autosuggest((Model)cbSummaryEngine.SelectedItem!, CurrentSettings.GenerateSummariesUsingLocalAi, dgvConversations, true);
                        autoSuggestForm.StringSelected += AutoSuggestStringSelected;
                    }),

                    new LabelAndEventHander("Autosuggest (User-Specified)", async (s, e) =>
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

                            var autoSuggestForm = await ConversationManager.Autosuggest((Model)cbSummaryEngine.SelectedItem!, CurrentSettings.GenerateSummariesUsingLocalAi, dgvConversations, true, userAutoSuggestPrompt);
                            autoSuggestForm.StringSelected += AutoSuggestStringSelected;
                        }
                    }),

                    new LabelAndEventHander("Toggle old input box visibility", (s, e) =>
                    {
                        splitContainer4.Panel1Collapsed = !splitContainer4.Panel1Collapsed;
                    }),

                    new LabelAndEventHander("Toggle conversation browsers", (s, e) =>
                    {
                        splitContainer1.Panel1Collapsed = !splitContainer1.Panel1Collapsed;
                    }),

                    new LabelAndEventHander("Test Snippets Code", async (s, e) =>
                    {
                        SnippetHelper.ShowSnippets(GetAllSnippets(ConversationManager.PreviousCompletion, ConversationManager.Conversation, snippetManager));
                    })
                }
            );

            menuBar.Items.Add(specialsMenu);
        }

        private static async Task TranscribeMP4(string openFileDialog, ChatWebView chatWebView)
        {

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

            // show mb to say it's done
            MessageBox.Show("Embeddings created and saved");
        }

        public static List<Snippet> GetAllSnippets(CompletionMessage currentMessage, BranchedConversation conversation, SnippetManager snippetManager)
        {
            var allSnippets = new List<Snippet>();
            
            foreach (var node in conversation.GetParentNodeList(currentMessage.Guid))
            {
                SnippetSet snippetSet = snippetManager.FindSnippets(node.Content);
                allSnippets.AddRange(snippetSet.Snippets);
            }

            return allSnippets;
        }
    }

}
