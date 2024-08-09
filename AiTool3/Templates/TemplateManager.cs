using AiTool3.Helpers;
using AiTool3.Topics;

namespace AiTool3.Templates
{
    public class TemplateManager
    {
        public TopicSet TemplateSet { get; set; }

        public TemplateManager()
        {
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

        public Dictionary<string, TemplateMenuItem> templateMenuItems = new Dictionary<string, TemplateMenuItem>();
    }
}