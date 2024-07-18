using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTool3.Helpers
{
    internal class HtmlTextExtractor
    {

        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task<string> ExtractTextFromUrlAsync(string url)
        {
            try
            {
                // Download the HTML content
                string htmlContent = await DownloadHtmlContentAsync(url);

                // Parse the HTML content
                var htmlDocument = new HtmlAgilityPack.HtmlDocument();
                htmlDocument.LoadHtml(htmlContent);

                // Extract and concatenate text
                string extractedText = ExtractTextFromHtmlDocument(htmlDocument);

                return extractedText;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error extracting text from URL: {url}", ex);
            }
        }

        private static async Task<string> DownloadHtmlContentAsync(string url)
        {
            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        private static string ExtractTextFromHtmlDocument(HtmlAgilityPack.HtmlDocument htmlDocument)
        {
            var textNodes = htmlDocument.DocumentNode.SelectNodes("//text()[not(parent::script)][not(parent::style)]");

            if (textNodes == null)
            {
                return string.Empty;
            }

            var stringBuilder = new StringBuilder();

            foreach (var textNode in textNodes)
            {
                string text = textNode.InnerText.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    stringBuilder.AppendLine(text);
                }
            }

            // remove any lines containing fewer than three tokens separated by spaces
            var lines = stringBuilder.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            stringBuilder.Clear();
            foreach (var line in lines)
            {
                if (line.Split(' ').Length >= 4)
                {
                    stringBuilder.AppendLine(line);
                }
            }


            return stringBuilder.ToString().Trim();
        }

        public static string QuoteFile(string filename, string fileContents)
        {
            return $"{MaxsAiStudio.ThreeTicks}{filename.Split('\\').Last()}{Environment.NewLine}{string.Join(Environment.NewLine, fileContents)}{Environment.NewLine}{MaxsAiStudio.ThreeTicks}{Environment.NewLine}";
        }
    }
}
