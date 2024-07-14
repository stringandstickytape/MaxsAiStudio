using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Helpers;
using AiTool3.Providers.Embeddings;
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

            var clearMenuItem = CreateMenuItem("Clear", ref editMenu);

            clearMenuItem.Click += (s, e) =>
            {
                btnClear_Click(null!, null!);
            };

            // add settings option.  When chosen, invokes SettingsForm modally
            var settingsMenuItem = CreateMenuItem("Settings", ref editMenu);

            settingsMenuItem.Click += (s, e) =>
            {
                var settingsForm = new SettingsForm(CurrentSettings);
                var result = settingsForm.ShowDialog();

                if (result == DialogResult.OK)
                {
                    CurrentSettings = settingsForm.NewSettings;
                    AiTool3.Settings.Settings.Save(CurrentSettings);
                }
            };

            menuBar.Items.Add(fileMenu);
            menuBar.Items.Add(editMenu);

            CreateTemplatesMenu();

            // add a specials menu
            CreateSpecialsMenu();
        }


        private void CreateTemplatesMenu()
        {
            var templatesMenu = CreateMenu("Templates");

            foreach (var topic in TopicSet.Topics)
            {
                var categoryMenuItem = CreateMenuItem(topic.Name, ref templatesMenu);

                foreach (var template in topic.Templates.Where(x => x.SystemPrompt != null))
                {
                    var templateMenuItem = (TemplateMenuItem)CreateMenuItem(template.TemplateName, ref categoryMenuItem,  true);
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

            menuBar.Items.Add(templatesMenu);
        }

        private void CreateSpecialsMenu()
        {
            var menuText = "Specials";
            ToolStripMenuItem specialsMenu = CreateMenu(menuText);


            AddSpecial(specialsMenu, "Create embedding", async (s, e) =>
            {
                // open a file browser and let user pick multiple files of any type



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
                
                var codeFragmenter = new CodeFragmenter();
                var htmlFragmenter = new WebCodeFragmenter();
                List<CodeFragment> fragments = new List<CodeFragment>();
                foreach (var file in files)
                {
                    var fileData = File.ReadAllText(file);
                    var frags2 = codeFragmenter.FragmentCode(fileData, file);
                    fragments.AddRange(frags2);
                }
                foreach(var file in htmlFiles)
                {
                    var fileData = File.ReadAllText(file);
                    var htmlFrags = htmlFragmenter.FragmentCode(fileData, file);
                    //fragments.AddRange(htmlFrags);
                }
                // get a .embeddings.json save file from the user
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
                var embeddings = await EmbeddingsHelper.CreateEmbeddingsAsync(frags.Select(x=>x.Content).ToList(), CurrentSettings.EmbeddingKey);

                for(var i = 0; i < frags.Count; i++)
                {
                    embeddings[i].Code = frags[i].Content;
                    embeddings[i].Filename = frags[i].FilePath;
                    embeddings[i].LineNumber = frags[i].LineNumber;
                }


                // write the embeddings to the save file as json
                var json = JsonSerializer.Serialize(embeddings);
                File.WriteAllText(saveFileDialog.FileName, json);

                // show mb to say it's done
                MessageBox.Show("Embeddings created and saved");
            });

            AddSpecial(specialsMenu, "Pull Readme and update from latest diff", async (s, e) =>
            {
                AiResponse response = await SpecialsHelper.GetReadmeResponses((Model)cbEngine.SelectedItem!);
                var snippets = snippetManager.FindSnippets(response.ResponseText);

                try
                {
                    var code = snippets.Snippets.First().Code;
                    code = SnipperHelper.StripFirstAndLastLine(code);
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
                await ConversationManager.RegenerateSummary((Model)cbEngine.SelectedItem!, CurrentSettings.GenerateSummariesUsingLocalAi, dgvConversations, "*");
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

            AddSpecial(specialsMenu, "Set Code Highlight Colours (experimental)", (s, e) =>
            {
                CSharpHighlighter.ConfigureColors();
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
                var x = GetAllSnippets(ConversationManager.PreviousCompletion, ConversationManager.CurrentConversation, snippetManager);

                // create a new form
                var f = new Form();

                // add a listbox with the snippets
                var lb = new ListBox();
                lb.Dock = DockStyle.Fill;
                f.Controls.Add(lb);
                lb.Items.AddRange(x.Select(x => x.Code).ToArray());
                f.Show();

            });

            menuBar.Items.Add(specialsMenu);
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
    }
}
