using Microsoft.CodeAnalysis;
using SharedClasses.Models;

namespace AiTool3.Embeddings.Fragmenters
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


}