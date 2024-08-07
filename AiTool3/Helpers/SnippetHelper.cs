using AiTool3.Conversations;
using AiTool3.Snippets;
using AiTool3.UI;

namespace AiTool3.Helpers
{
    public static class SnippetHelper
    {
        public static string StripFirstAndLastLine(string code)
        {
            return code.Substring(code.IndexOf('\n') + 1, code.LastIndexOf('\n') - code.IndexOf('\n') - 1);
        }
    }
}