using System.Net.Http.Json;
using System.Text.Json;

namespace DiffLibrary
{
    public partial class Form1 : Form
    {
        private TextReplacer _textReplacer = new TextReplacer();
        private string _rootPath = string.Empty;

        public Form1()
        {
            InitializeComponent();
            _rootPath = tbRootPath.Text.Trim();
        }
        private void button1_Click(object sender, EventArgs e)
        {

            var processor = new DiffLibrary.ChangesetProcessor(tbRootPath.Text.Trim());
            try
            {
                processor.ProcessChangeset(textBox1.Text);
                tbOutput.Text = processor.Log.ToString();
                Console.WriteLine("Changeset applied successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

        }
    }


}