using Microsoft.CodeAnalysis;
using SharedClasses.Models;
using System.Collections.Generic;
using System.Linq;

namespace VSIXTest.Embeddings.Fragmenters
{
    public class VsixCsFragmenter
    {
        public List<CodeFragment> FragmentCode(string fileContent, string filePath)
        {

            var a = VsixRoslynHelper.ExtractMethodsUsingRoslyn(fileContent, filePath);

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