using SharedClasses.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VSIXTest.Embeddings.Fragmenters
{
    public class VsixLineFragmenter

    {
        public List<CodeFragment> FragmentCode(string fileContent, string filePath, int maxFragmentSize = 50)
        {
            var fragments = new List<CodeFragment>();
            var lines = fileContent.Split('\n');

            for (int i = 0; i < lines.Length; i += maxFragmentSize)
            {
                var chunkLines = lines.Skip(i).Take(maxFragmentSize);
                var content = string.Join("\n", chunkLines);

                fragments.Add(new CodeFragment
                {
                    Content = content,
                    Type = Path.GetExtension(filePath).TrimStart('.').ToUpper(),
                    FilePath = filePath,
                    LineNumber = i + 1
                });
            }

            return fragments;
        }
    }
}