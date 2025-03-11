using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

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

        private void button2_Click(object sender, EventArgs e)
        {
            FindMostCommonTokens(tbRootPath.Text);
        }

        public static void FindMostCommonTokens(string rootDirectory)
        {
            // Get all .ts and .tsx files recursively
            var files = Directory.GetFiles(rootDirectory, "*.ts", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(rootDirectory, "*.tsx", SearchOption.AllDirectories));

            // Dictionary to store token counts
            var tokenCounts = new Dictionary<string, int>(StringComparer.Ordinal);

            // Regex to match word characters (alphanumeric + underscore)
            var tokenRegex = new Regex(@"[\w]+", RegexOptions.Compiled);

            // Process each file
            foreach (var file in files)
            {
                try
                {
                    string content = File.ReadAllText(file);
                    var matches = tokenRegex.Matches(content);

                    foreach (Match match in matches)
                    {
                        string token = match.Value;
                        if (tokenCounts.ContainsKey(token))
                            tokenCounts[token]++;
                        else
                            tokenCounts[token] = 1;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {file}: {ex.Message}");
                }
            }

            // Get top 20 tokens by count
            var topTokens = tokenCounts
                .OrderByDescending(pair => pair.Value)
                .Take(100);

            // Display results
            System.Diagnostics.Debug.WriteLine("Top 20 most common tokens:");
            System.Diagnostics.Debug.WriteLine("Token".PadRight(30) + "Count");
            System.Diagnostics.Debug.WriteLine(new string('-', 40));

            foreach (var pair in topTokens)
            {
                System.Diagnostics.Debug.WriteLine($"{pair.Key.PadRight(30)}{pair.Value}");
            }
        }
    }
}
