using AiTool3.Conversations;
using AiTool3.Helpers;
using AiTool3.UI;
using Microsoft.CodeAnalysis;
using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;
using static AiTool3.UI.ButtonedRichTextBox;


namespace AiTool3.MegaBar.Items
{
    public class MegaBarItemFactory
    {
        private static readonly Dictionary<MegaBarItemType, Func<string, string, List<CompletionMessage>, Action>> ItemCallbacks = new Dictionary<MegaBarItemType, Func<string, string, List<CompletionMessage>, Action>>
        {
            [MegaBarItemType.Copy] = (code,guid,messages) => () =>
            {
                var processedCode = PrependParentIfUnterminated(guid, messages, code);
                Clipboard.SetText(SnipperHelper.StripFirstAndLastLine(processedCode));
                },

            [MegaBarItemType.Browser] = (code,guid,messages) => () => LaunchHelpers.LaunchHtml(SnipperHelper.StripFirstAndLastLine(code)),
            [MegaBarItemType.CSharpScript] = (code,guid,messages) => () => LaunchHelpers.LaunchCSharp(SnipperHelper.StripFirstAndLastLine(code)),
            [MegaBarItemType.Notepad] = (code,guid,messages) => () => LaunchHelpers.LaunchTxt(SnipperHelper.StripFirstAndLastLine(code)),
            [MegaBarItemType.SaveAs] = (code,guid,messages) => () =>
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog.RestoreDirectory = true;
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveFileDialog.FileName, SnipperHelper.StripFirstAndLastLine(code));
                }
            },
            [MegaBarItemType.CopyWithoutComments] = (code,guid,messages) => () =>
            {
                string codeWithoutComments = RemoveComments(SnipperHelper.StripFirstAndLastLine(code));
                Clipboard.SetText(codeWithoutComments);
            },
            [MegaBarItemType.WebView] = (code,guid,messages) => () =>
            {

                var processedCode = PrependParentIfUnterminated(guid, messages, code);

                // create a new form of 256x256
                var form = new Form();
                form.Size = new Size(256, 256);
                form.StartPosition = FormStartPosition.CenterScreen;

                // create a WebView2 that fills the window
                var wvForm = new WebviewForm(processedCode);
                wvForm.Show();

            },
             [MegaBarItemType.LaunchInVisualStudio] = (code,guid,messages) => () =>
             {
                 string tempFile = Path.GetTempFileName();
                 File.WriteAllText(tempFile, SnipperHelper.StripFirstAndLastLine(code));
                 string vsPath = @"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe";
                 if (File.Exists(vsPath))
                 {
                     Process.Start(vsPath, tempFile);
                 }
                 else
                 {
                     MessageBox.Show("Visual Studio not found. Please install it or update the path.");
                 }
            }
        };

        private static string PrependParentIfUnterminated(string guid, List<CompletionMessage> messages, string processedCode)
        {
            // use the guid to find the message
            var message = messages.FirstOrDefault(m => m.Guid == guid);
            var parentMessage = messages.FirstOrDefault(m => m.Guid == message.Parent);
            var parentOfParent = messages.FirstOrDefault(m => m.Guid == parentMessage.Parent);

            var snippetMgr = new Snippets.SnippetManager();
            var snippets = snippetMgr.FindSnippets(parentOfParent.Content);

            if (!string.IsNullOrWhiteSpace(snippets.UnterminatedSnippet))
            {
                // Ask the user (yes/no) whether they want to concat the previous unfinished message
                var result = MessageBox.Show("The previous message was not terminated. Do you want to include it in the snippet?", "Unfinished Snippet", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {

                    string prefixCode = snippets.UnterminatedSnippet;

                    // get the last line of unterm sip
                    var lastLine = prefixCode.Split('\n').Last();

                    // get the first line of code execept the `s
                    var firstLine = processedCode.Split('\n').Skip(1).First();



                    // if the first line contains the last line...
                    if (firstLine.Contains(lastLine) && lastLine.Length > 0)
                    {
                        // remove the last line from prefixCode
                        prefixCode = prefixCode.Substring(0, prefixCode.LastIndexOf(lastLine));
                    }

                    processedCode = $"{SnipperHelper.StripFirstAndLastLine(prefixCode)}{Environment.NewLine}{SnipperHelper.StripFirstAndLastLine(processedCode)}";
                }
            }

            return processedCode;
        }

        private static object StripFirstAndLastLine(string code)
        {
            throw new NotImplementedException();
        }

        private static string RemoveComments(string code)
        {
            // Remove single-line comments
            code = System.Text.RegularExpressions.Regex.Replace(code, @"//.*$", "", System.Text.RegularExpressions.RegexOptions.Multiline);

            // Remove multi-line comments
            code = System.Text.RegularExpressions.Regex.Replace(code, @"/\*[\s\S]*?\*/", "");

            // Remove any trailing whitespace that might be left after removing comments
            code = System.Text.RegularExpressions.Regex.Replace(code, @"\s+$", "", System.Text.RegularExpressions.RegexOptions.Multiline);

            return code;
        }


        public static List<MegaBarItem> CreateItems(string snippetType, string snippetCode, bool unterminated, string messageGuid, List<Conversations.CompletionMessage> messages)
        {
            var items = new List<MegaBarItem>();

            foreach (MegaBarItemType itemType in Enum.GetValues(typeof(MegaBarItemType)))
            {
                var attr = itemType.GetType().GetField(itemType.ToString())
                    .GetCustomAttributes(typeof(MegaBarItemInfoAttribute), false)
                    .FirstOrDefault() as MegaBarItemInfoAttribute;

                if (attr != null && (attr.SupportedTypes.Contains(snippetType) || attr.SupportedTypes.Contains("*")))
                {
                    items.Add(new MegaBarItem
                    {
                        Title = attr.Title,
                        Callback = ItemCallbacks[itemType](snippetCode, messageGuid, messages),
                        OriginatingMessage = messageGuid,
                        OriginatingConversation = messages
                    });
                }
            }

            return items;
        }
    }

}
