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
    }
}