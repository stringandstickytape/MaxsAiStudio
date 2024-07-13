using AiTool3.ApiManagement;
using AiTool3.Helpers;
using AiTool3.Settings;
using AiTool3.Snippets;
using AiTool3.Topics;
using AiTool3.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            AddSpecial(specialsMenu, "Review Code", (s, e) =>
            {
                SpecialsHelper.ReviewCode(out string userMessage);
                rtbInput.Text = userMessage;
            });
            AddSpecial(specialsMenu, "Rewrite Summaries", async (s, e) =>
            {
                await ConversationManager.RegenerateAllSummaries((Model)cbEngine.SelectedItem!, CurrentSettings.GenerateSummariesUsingLocalAi, dgvConversations);
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
    }
}
