using AiTool3.UI;
using System.Text.RegularExpressions;

namespace AiTool3.Conversations
{


    public static class FineAndReplaceProcessor
    {
        public static string ApplyFindAndReplace(string originalFile, List<FindAndReplace> replacements, out string errorString)
        {
            errorString = "";
            string modifiedFile = originalFile;

            foreach (var replacement in replacements)
            {
                string find = replacement.find;
                string replace = replacement.replace;

                // Escape special regex characters, but not whitespace
                string escapedFind = Regex.Replace(find, @"[.*+?^${}()|[\]\\]", @"\$&");

                // Convert the escaped find string to a regex pattern that allows flexible whitespace
                string pattern = Regex.Replace(escapedFind, @"\s+", @"\s+");

                // Create a regex that matches the 'find' string, allowing flexible whitespace
                var regex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.Compiled);

                // Check if the 'find' string exists in the file
                if (!regex.IsMatch(modifiedFile))
                {
                    Console.WriteLine($"Find string not found: \"{replacement.find}\"");
                    Console.WriteLine($"Pattern used: {pattern}");
                    MessageBox.Show($"Couldn't find the string \"{replacement.find}\" in the file. The file will not be modified.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    errorString = $"Couldn't find the string \"{replacement.find}\"";


                    return null;
                }

                // Apply the replacement
                modifiedFile = regex.Replace(modifiedFile, replace);
            }

            return modifiedFile;
        }

        internal static async Task ApplyFindAndReplaceArray(FindAndReplaceSet? fnrs, ChatWebView chatWebView)
        {
            var grouped = fnrs.replacements.GroupBy(r => r.filename);

            foreach (var group in grouped)
            {
                var originalContent = File.ReadAllText(group.Key);
                var processed = FineAndReplaceProcessor.ApplyFindAndReplace(originalContent, group.ToList(), out string errorString);
                if (processed == null)
                {
                    await chatWebView.SetUserPrompt(await chatWebView.GetUserPrompt() + $"\nError processing file {group.Key}: {errorString}");
                    break;
                }
            }

            // for each group
            foreach (var group in grouped)
            {
                var originalContent = File.ReadAllText(group.Key);
                var processed = FineAndReplaceProcessor.ApplyFindAndReplace(originalContent, group.ToList(), out string errorString);
                if (processed != null)
                {
                    File.WriteAllText(group.Key, processed);
                }
                else
                {
                    await chatWebView.SetUserPrompt(await chatWebView.GetUserPrompt() + $"\nError processing file {group.Key}: {errorString}");
                }
            }
            MessageBox.Show($"Done.");
        }
    }
}