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
        private static readonly Dictionary<MegaBarItemType, Func<string, Action>> ItemCallbacks = new Dictionary<MegaBarItemType, Func<string, Action>>
        {
            [MegaBarItemType.Copy] = code => () => Clipboard.SetText(SnipperHelper.StripFirstAndLastLine(code)),
            [MegaBarItemType.Browser] = code => () => LaunchHelpers.LaunchHtml(SnipperHelper.StripFirstAndLastLine(code)),
            [MegaBarItemType.CSharpScript] = code => () => LaunchHelpers.LaunchCSharp(SnipperHelper.StripFirstAndLastLine(code)),
            [MegaBarItemType.Notepad] = code => () => LaunchHelpers.LaunchTxt(SnipperHelper.StripFirstAndLastLine(code)),
            [MegaBarItemType.SaveAs] = code => () =>
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog.RestoreDirectory = true;
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveFileDialog.FileName, SnipperHelper.StripFirstAndLastLine(code));
                }
            },
            [MegaBarItemType.CopyWithoutComments] = code => () =>
            {
                string codeWithoutComments = RemoveComments(SnipperHelper.StripFirstAndLastLine(code));
                Clipboard.SetText(codeWithoutComments);
            },
            [MegaBarItemType.WebView] = code => () =>
            {
                // create a new form of 256x256
                var form = new Form();
                form.Size = new Size(256, 256);
                form.StartPosition = FormStartPosition.CenterScreen;

                // create a WebView2 that fills the window
                var wvForm = new WebviewForm(code);
                wvForm.Show();
                
             },
             [MegaBarItemType.LaunchInVisualStudio] = code => () =>
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


        public static List<MegaBarItem> CreateItems(string snippetType, string snippetCode)
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
                        Callback = ItemCallbacks[itemType](snippetCode)
                    });
                }
            }

            return items;
        }
    }

}
