using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.Embeddings;
using AiTool3.FileAttachments;
using AiTool3.Snippets;
using AiTool3.Templates;
using AiTool3.Topics;
using AiTool3.UI;
using Newtonsoft.Json;
using SharedClasses;

namespace AiTool3.Helpers
{
    public static class MenuHelper
    {
        public static ToolStripMenuItem CreateMenu(string menuText)
        {
            var menu = new ToolStripMenuItem(menuText);
            menu.BackColor = Color.Black;
            menu.ForeColor = Color.White;
            return menu;
        }

        public static ToolStripMenuItem CreateMenuItem(string text, ref ToolStripMenuItem dropDownItems, bool isTemplate = false)
        {
            if (isTemplate)
                return new TemplateMenuItem(text, ref dropDownItems);

            var retVal = new ToolStripMenuItem(text);
            dropDownItems.DropDownItems.Add(retVal);
            return retVal;
        }

        public static void AddSpecial(ToolStripMenuItem specialsMenu, string label, EventHandler clickHandler)
        {
            var specialMenuItem = CreateMenuItem(label, ref specialsMenu);
            specialMenuItem.Click += clickHandler;
        }

        public static void AddSpecials(ToolStripMenuItem specialsMenu, List<LabelAndEventHander> specials)
        {
            foreach (var special in specials)
            {
                AddSpecial(specialsMenu, special.Label, special.Handler);
            }
        }

        public static void RemoveOldTemplateMenus(MenuStrip menuBar)
        {
            menuBar.Items.OfType<ToolStripMenuItem>().Where(x => x.Text == "Templates").ToList().ForEach(x => menuBar.Items.Remove(x));
        }

        private static async Task SelectNoneTemplate(MenuStrip menuBar, ChatWebView chatWebView, TemplateManager templateManager)
        {
            templateManager.ClearTemplate();
            await chatWebView.Clear();
            await chatWebView.UpdateSystemPrompt("");
            await chatWebView.SetUserPrompt("");

            // Update menu items
            foreach (var item in templateManager.templateMenuItems.Values)
            {
                item.IsSelected = false;
            }
            menuBar.Refresh(); // Force redraw of the menu
        }


        public static void CreateTemplatesMenu(MenuStrip menuBar, ChatWebView chatWebView, TemplateManager templateManager, SettingsSet currentSettings, MaxsAiStudio maxsAiStudioForm)
        {
            templateManager.templateMenuItems.Clear();

            var templatesMenu = CreateMenu("Templates");

            // Add "None" option at the top
            var noneMenuItem = CreateMenuItem("None", ref templatesMenu);
            noneMenuItem.Click += async (s, e) =>
            {
                await SelectNoneTemplate(menuBar, chatWebView, templateManager);
            };

            // Add separator after "None"
            templatesMenu.DropDownItems.Add(new ToolStripSeparator());

            foreach (var category in templateManager.TemplateSet.Categories.OrderBy(x => x.Name))
            {
                var categoryMenuItem = CreateMenuItem(category.Name, ref templatesMenu);

                categoryMenuItem.ToolTipText = "SHIFT-click to delete";
                categoryMenuItem.MouseDown +=
                    (s, e) => CategoryMenuItemMouseDown(menuBar, chatWebView, templateManager, currentSettings, maxsAiStudioForm, e, templatesMenu, category);

                foreach (var template in category.Templates.Where(x => x.SystemPrompt != null).OrderBy(x => x.TemplateName))
                {
                    var templateMenuItem = (TemplateMenuItem)CreateMenuItem(template.TemplateName, ref categoryMenuItem, true);
                    templateManager.templateMenuItems[template.TemplateName] = templateMenuItem;

                    templateMenuItem.Click += async (s, e) =>
                    {
                        // if shift is held:
                        if (Control.ModifierKeys == Keys.Shift)
                        {
                            if (MessageBox.Show("Are you sure you want to delete this template?", "Delete Template", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                category.Templates.Remove(template);
                                templateManager.TemplateSet.Save();
                                RecreateTemplatesMenu(menuBar, chatWebView, templateManager, currentSettings, maxsAiStudioForm);
                            }
                        }
                        else // if ctrl is held:
                        if (Control.ModifierKeys == Keys.Control)
                        {
                            templateManager.SelectTemplateByCategoryAndName(category.Name, template.TemplateName);

                            await chatWebView.UpdateSystemPrompt(template?.SystemPrompt ?? "");
                        }
                        else
                        {
                            templateManager.SelectTemplateByCategoryAndName(category.Name, template.TemplateName);

                            await maxsAiStudioForm.PopulateUiForTemplate(templateManager.CurrentTemplate!);

                            menuBar.Refresh();
                        }
                    };
                    templateMenuItem.EditClicked += (s, e) =>
                    {
                        templateManager.EditAndSaveTemplate(template, false, category.Name);
                        RecreateTemplatesMenu(menuBar, chatWebView, templateManager, currentSettings, maxsAiStudioForm);
                    };
                }

                // at the end of each category, add a separator then an Add... option
                categoryMenuItem.DropDownItems.Add(new ToolStripSeparator());
                var addMenuItem = CreateMenuItem("Add...", ref categoryMenuItem);
                addMenuItem.Click += (s, e) =>
                {
                    // s is a ToolStripMenuItem
                    var templateName = ((ToolStripMenuItem)s!).OwnerItem!.Text;

                    var template = new ConversationTemplate("System Prompt", "Initial Prompt");

                    templateManager.EditAndSaveTemplate(template, true, templateName);

                    RecreateTemplatesMenu(menuBar, chatWebView, templateManager, currentSettings, maxsAiStudioForm);

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
                form.Size = new Size(400, 150);
                form.StartPosition = FormStartPosition.CenterScreen;

                tb.Location = new Point(50, 10);
                tb.Size = new Size(300, 20);
                tb.TabIndex = 0;
                form.Controls.Add(tb);

                okButton.Text = "OK";
                okButton.Location = new Point(50, 50);
                okButton.Size = new Size(75, 23);
                okButton.DialogResult = DialogResult.OK;
                form.Controls.Add(okButton);

                cancelButton.Text = "Cancel";
                cancelButton.Location = new Point(150, 50);

                cancelButton.Size = new Size(75, 23);
                cancelButton.DialogResult = DialogResult.Cancel;

                form.Controls.Add(cancelButton);

                form.AcceptButton = okButton;

                form.CancelButton = cancelButton;

                var result = form.ShowDialog();

                if (result == DialogResult.OK)
                {
                    var newCategoryName = tb.Text;
                    templateManager.TemplateSet.Categories.Add(new Topic(Guid.NewGuid().ToString(), newCategoryName));
                    templateManager.TemplateSet.Save();

                    // find all the existing Templates named menus
                    RecreateTemplatesMenu(menuBar, chatWebView, templateManager, currentSettings, maxsAiStudioForm);
                }
            };

            menuBar.Items.Add(templatesMenu);
        }

        private static void CategoryMenuItemMouseDown(MenuStrip menuBar, ChatWebView chatWebView, TemplateManager templateManager, SettingsSet currentSettings, MaxsAiStudio maxsAiStudioForm, MouseEventArgs e, ToolStripMenuItem templatesMenu, Topic category)
        {
            if (e.Button == MouseButtons.Left && Control.ModifierKeys == Keys.Shift)
            {
                templatesMenu.DropDown.Close();
                if (MessageBox.Show($"Are you sure you want to delete the entire '{category.Name}' category and all its templates?", "Delete Category", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    templateManager.TemplateSet.Categories.Remove(category);
                    templateManager.TemplateSet.Save();
                    RecreateTemplatesMenu(menuBar, chatWebView, templateManager, currentSettings, maxsAiStudioForm);
                }
            }
        }

        private static void RecreateTemplatesMenu(MenuStrip menuBar, ChatWebView chatWebView, TemplateManager templateManager, SettingsSet currentSettings, MaxsAiStudio form)
        {
            RemoveOldTemplateMenus(menuBar);

            CreateTemplatesMenu(menuBar, chatWebView, templateManager, currentSettings, form);
        }

        public static async Task CreateSpecialsMenu(MenuStrip menuBar, SettingsSet currentSettings, ChatWebView chatWebView, SnippetManager snippetManager, DataGridView dgvConversations, ConversationManager conversationManager, Action<string> autoSuggestStringSelected, FileAttachmentManager _fileAttachmentManager, MaxsAiStudio maxsAiStudio)
        {


            var menuText = "Specials";
            ToolStripMenuItem specialsMenu = CreateMenu(menuText);

            AddSpecials(specialsMenu,
                new List<LabelAndEventHander>
                {


                    new LabelAndEventHander("Pull Readme and update from latest diff", async (s, e) =>
                        {
                        var model = await chatWebView.GetDropdownModel("summaryAI", currentSettings);
                        AiResponse response = await SpecialsHelper.GetReadmeResponses(model);
                        var snippets = snippetManager.FindSnippets(response.ResponseText);

                        try
                        {
                            var code = snippets.Snippets.First().Content;
                            code = SnippetHelper.StripFirstAndLastLine(code);
                            File.WriteAllText(@"C:\Users\maxhe\source\repos\CloneTest\MaxsAiStudio\README.md", code);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error writing to file: {ex.Message}");
                        }
                    }),

                    new LabelAndEventHander("Autosuggest", async (s, e) =>
                    {
                        var model = await chatWebView.GetDropdownModel("summaryAI", currentSettings);
                        var autoSuggestForm = await conversationManager.Autosuggest(model, dgvConversations);
                        autoSuggestForm.StringSelected += autoSuggestStringSelected;
                    }),

                    new LabelAndEventHander("Autosuggest (Fun)", async (s, e) =>
                    {
                        var model = await chatWebView.GetDropdownModel("summaryAI", currentSettings);
                        var autoSuggestForm = await conversationManager.Autosuggest(model, dgvConversations, true);
                        autoSuggestForm.StringSelected += autoSuggestStringSelected;
                    }),

                    new LabelAndEventHander("Edit Visual Studio Plugin Buttons", async (s, e) =>
                    {
                        var form = new MessagePromptEditorForm(currentSettings.MessagePrompts);
                        form.ShowDialog();

                        currentSettings.MessagePrompts = form.GetUpdatedPrompts();
                        SettingsSet.Save(currentSettings);
                        await chatWebView.SendToVsixAsync(new VsixMessage { MessageType = "vsButtons", Content = JsonConvert.SerializeObject(currentSettings.MessagePrompts) });
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

                            var model = await chatWebView.GetDropdownModel("summaryAI", currentSettings);
                            var autoSuggestForm = await conversationManager.Autosuggest(model, dgvConversations, true, userAutoSuggestPrompt);
                            autoSuggestForm.StringSelected += autoSuggestStringSelected;
                        }
                    }),

                    new LabelAndEventHander("Show Model Usage/Cost Statistics", (s, e) =>
                    {
                        ModelUsageManager.ShowUsageStatistics(currentSettings);
                    }),


                }
            );

            menuBar.Items.Add(specialsMenu);
        }

        internal static async Task CreateEmbeddingsMenu(MaxsAiStudio maxsAiStudio, MenuStrip menuBar, SettingsSet currentSettings, ChatWebView chatWebView, SnippetManager snippetManager, DataGridView dgvConversations, ConversationManager conversationManager, Action<string> autoSuggestStringSelected, FileAttachmentManager fileAttachmentManager)
        {



            var menuText = "Embeddings";
            ToolStripMenuItem embeddingsMenu = CreateMenu(menuText);

            AddSpecials(embeddingsMenu,
                new List<LabelAndEventHander>
                {
                    new LabelAndEventHander("Create Embedding...", async (s, e) =>
                    {

                        await EmbeddingsHelper.CreateEmbeddingsAsync("Ollama", maxsAiStudio);

                    }),

                    new LabelAndEventHander("Select Embedding...", async (s, e) =>
                    {
                        var openFileDialog = new OpenFileDialog();
                        openFileDialog.Filter = $"{maxsAiStudio.CurrentSettings.EmbeddingModel} Embeddings JSON file|*.{maxsAiStudio.CurrentSettings.EmbeddingModel}.embeddings.json";
                        openFileDialog.InitialDirectory = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Embeddings");
                        openFileDialog.Multiselect = false;
                        openFileDialog.ShowDialog();

                        if(openFileDialog.FileName == "")
                        {
                            return;
                        }

                        maxsAiStudio.CurrentSettings.EmbeddingsFilename = openFileDialog.FileName;
                        SettingsSet.Save(maxsAiStudio.CurrentSettings);

                    })
                }
            );

            menuBar.Items.Add(embeddingsMenu);

        }
    }

    public class LabelAndEventHander
    {
        public string Label { get; set; }
        public EventHandler Handler { get; set; }

        public LabelAndEventHander(string label, EventHandler handler)
        {
            Label = label;
            Handler = handler;
        }
    }
}
