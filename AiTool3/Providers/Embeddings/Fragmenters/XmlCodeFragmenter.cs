using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AiTool3.Providers.Embeddings.Fragmenters
{
    public class XmlCodeFragmenter
    {
        private const int MaxLinesPerFragment = 100;

        public List<CodeFragment> FragmentCode(string fileContent, string filePath)
        {
            var fragments = new List<CodeFragment>();
            var lines = fileContent.Split('\n');

            for (int i = 0; i < lines.Length; i += MaxLinesPerFragment)
            {
                var chunkLines = lines.Skip(i).Take(MaxLinesPerFragment);
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