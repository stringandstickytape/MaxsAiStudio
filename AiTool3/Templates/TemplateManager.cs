using AiTool3.Helpers;
using AiTool3.Topics;
using Newtonsoft.Json;

namespace AiTool3.Templates
{
    public class TemplateManager
    {
        public TopicSet TemplateSet { get; set; }

        public TemplateManager()
        {
            DirectoryHelper.CreateSubdirectories();
            TemplateSet = TopicSet.Load();
        }
        public ConversationTemplate? CurrentTemplate { get; set; }

        public void EditAndSaveTemplate(ConversationTemplate template, bool add = false, string? category = null)
        {
            TemplatesHelper.UpdateTemplates(template, add, category, new Form(), TemplateSet);
        }

        public void SelectTemplateByCategoryAndName(string categoryName, string templateName)
        {
            CurrentTemplate = TemplateSet.Categories.First(t => t.Name == categoryName).Templates.First(t => t.TemplateName == templateName);
            UpdateMenuItems();
        }

        public void UpdateMenuItems()
        {
            foreach (var item in templateMenuItems.Values)
            {
                item.IsSelected = false;
            }
            if (templateMenuItems.TryGetValue(CurrentTemplate.TemplateName, out var menuItem))
            {
                menuItem.IsSelected = true;
            }
        }

        public void ClearTemplate() => CurrentTemplate = null;

        internal bool ImportTemplate(string jsonContent)
        {
            bool updateMenu = false;

            var importTemplate = JsonConvert.DeserializeObject<TemplateImport>(jsonContent);

            var template = new ConversationTemplate(importTemplate.systemPrompt, importTemplate.initialUserPrompt);

            if (template != null)
            {
                var categoryForm = new Form();
                categoryForm.Text = "Select Category";
                categoryForm.Size = new Size(300, 150);
                categoryForm.StartPosition = FormStartPosition.CenterScreen;

                var comboBox = new ComboBox();
                comboBox.Dock = DockStyle.Top;
                comboBox.Items.AddRange(TemplateSet.Categories.Select(c => c.Name).ToArray());

                var okButton = new Button();
                okButton.Text = "OK";
                okButton.DialogResult = DialogResult.OK;
                okButton.Dock = DockStyle.Bottom;

                categoryForm.Controls.Add(comboBox);
                categoryForm.Controls.Add(okButton);

                if (categoryForm.ShowDialog() == DialogResult.OK)
                {
                    var selectedCategory = comboBox.SelectedItem?.ToString();
                    if (!string.IsNullOrEmpty(selectedCategory))
                    {
                        EditAndSaveTemplate(template, true, selectedCategory);
                        updateMenu = true;
                    }
                }
            }

            return updateMenu;
        }

        public Dictionary<string, TemplateMenuItem> templateMenuItems = new Dictionary<string, TemplateMenuItem>();
    }
}