using AiTool3.Snippets;
using AiTool3.UI;

namespace AiTool3.Helpers
{
    public static class SnippetHelper
    {
        public static string StripFirstLine(string code)
        {
            return code.Substring(code.IndexOf('\n') + 1);
        }

        public static string StripFirstAndLastLine(string code)
        {
            return code.Substring(code.IndexOf('\n') + 1, code.LastIndexOf('\n') - code.IndexOf('\n') - 1);
        }
        public static void ShowSnippets(List<Snippet> snippets)
        {
            Form snippetForm = new Form
            {
                Text = "Conversation Snippets",
                Size = new Size(600, 400),
                StartPosition = FormStartPosition.CenterScreen
            };

            ListView listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true
            };

            listView.Columns.Add("Snippet", -2);

            foreach (var snippet in snippets)
            {
                listView.Items.Add(new ListViewItem(snippet.Content));
            }

            listView.ItemActivate += (sender, e) =>
            {
                if (listView.SelectedItems.Count > 0)
                {
                    string selectedSnippet = listView.SelectedItems[0].Text;
                    Clipboard.SetText(selectedSnippet);
                    MessageBox.Show("Snippet copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            snippetForm.Controls.Add(listView);
            snippetForm.Show();
        }
    }
}