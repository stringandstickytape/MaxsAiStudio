using Microsoft.CodeAnalysis;

namespace AiTool3.Providers.Embeddings.Fragmenters
{
    public class CsFragmenter
    {
        public List<CodeFragment> FragmentCode(string fileContent, string filePath)
        {

            var a = RoslynHelper.ExtractMethodsUsingRoslyn(fileContent, filePath);

            return a.Select(x => new CodeFragment
            {
                Content = x.SourceCode,
                Type = "Method",
                FilePath = x.SourceFileName,
                LineNumber = x.StartLineNumber,
                Class = x.ClassName,
                Namespace = x.Namespace
            }).ToList();

        }
    }

    public class CodeFragment
    {
        public string Content { get; set; }
        public string Type { get; set; }
        public string FilePath { get; set; }
        public int LineNumber { get; set; }

        public string Class { get; set; }
        public string Namespace { get; set; }
    }
}