namespace AiTool3.Embeddings.Fragmenters
{
    public class LineFragmenter

    {
        public List<CodeFragment> FragmentCode(string fileContent, string filePath, int maxFragmentSize = 100)
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