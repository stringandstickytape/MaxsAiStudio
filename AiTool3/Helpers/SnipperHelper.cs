using AiTool3.UI;

namespace AiTool3.Helpers
{
    public static class SnipperHelper
    {

        public static string StripFirstAndLastLine(string code)
        {
            return code.Substring(code.IndexOf('\n') + 1, code.LastIndexOf('\n') - code.IndexOf('\n') - 1);
        }

        public static int GetLastCharOfFirstLine(ButtonedRichTextBox richTextBox, int startIndex)
        {
            return richTextBox.Text.IndexOf('\n', startIndex);
        }
    }
}