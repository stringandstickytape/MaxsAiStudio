
namespace AiTool3.UI
{
    internal class DotViewer
    {
        internal static void View(string plantUmlString)
        {
            var form = new Form();
            form.Size = new Size(256, 256);
            form.StartPosition = FormStartPosition.CenterScreen;

            // create a WebView2 that fills the window
            var replHtml = html.Replace("DATAGOESHERE", plantUmlString);
            var wvForm = new WebviewForm(replHtml);
            wvForm.Show();
        }

        private static string html = @"const dotString = `
    digraph G {
        A -> B;
        B -> C;
        C -> A;
    }
`;

renderDotString(dotString);
";
    }
}