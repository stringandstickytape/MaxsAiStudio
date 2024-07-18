using AiTool3.Helpers;
using AiTool3.Topics;

namespace AiTool3
{
    public class TemplateManager
    {
        public TopicSet TemplateSet { get; set; }

        public TemplateManager()
        {
            TemplateSet = TopicSet.Load();
        }

        public void EditAndSaveTemplate(ConversationTemplate template, bool add = false, string? category = null)
        {
            TemplatesHelper.UpdateTemplates(template, add, category, new Form(), TemplateSet);
        }

        public ConversationTemplate? GetTemplateByCategoryAndName(string categoryName, string templateName)
        {
            return TemplateSet.Categories.First(t => t.Name == categoryName).Templates.First(t => t.TemplateName == templateName);
        }

        public void UpdateMenuItems(string selectedTemplateName)
        {
            foreach (var item in templateMenuItems.Values)
            {
                item.IsSelected = false;
            }
            if (templateMenuItems.TryGetValue(selectedTemplateName, out var menuItem))
            {
                menuItem.IsSelected = true;
            }
        }

        public Dictionary<string, TemplateMenuItem> templateMenuItems = new Dictionary<string, TemplateMenuItem>();
    }
}