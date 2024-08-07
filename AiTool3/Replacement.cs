using AiTool3.Conversations;
using System.Text.RegularExpressions;

namespace AiTool3
{


    public static class FileProcessor
    {
        public static string ApplyFindAndReplace(string originalFile, List<FindAndReplace> replacements)
        {
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
                    return null;
                }

                // Apply the replacement
                modifiedFile = regex.Replace(modifiedFile, replace);
            }

            return modifiedFile;
        }
    }
}