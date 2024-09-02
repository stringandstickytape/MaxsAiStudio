
using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;


namespace VSIXTest
{

    public class MethodFinder
    {
        private string _searchString { get; set; }
        public List<MethodInfo> FindMethods(string searchString)
        {
            _searchString = searchString;
            List<MethodInfo> result = new List<MethodInfo>();
            DTE2 dte = (DTE2)System.Runtime.InteropServices.Marshal.GetActiveObject("VisualStudio.DTE");

            foreach (Project project in dte.Solution.Projects)
            {
                SearchProjectItems(project.ProjectItems, result);
            }

            return result;
        }


        private void SearchProjectItems(ProjectItems projectItems, List<MethodInfo> result)
        {
            foreach (ProjectItem item in projectItems)
            {
                if (item.FileCodeModel != null)
                {
                    SearchCodeElements(item.FileCodeModel.CodeElements, result);
                }

                if (item.ProjectItems != null)
                {
                    SearchProjectItems(item.ProjectItems, result);
                }
            }
        }

        private void SearchCodeElements(CodeElements elements, List<MethodInfo> result)
        {
            foreach (CodeElement element in elements)
            {
                if (element.Kind == vsCMElement.vsCMElementFunction)
                {
                    CodeFunction function = (CodeFunction)element;
                    string docComment = function.DocComment;

                    if (!string.IsNullOrEmpty(docComment) && docComment.IndexOf(_searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result.Add(new MethodInfo
                        {
                            Name = function.Name,
                            FileName = function.ProjectItem.FileNames[0],
                            SourceCode = GetMethodSourceCode(function)
                        });
                    }
                }

                if (element.Children != null)
                {
                    SearchCodeElements(element.Children, result);
                }
            }
        }

        private string GetMethodSourceCode(CodeFunction function)
        {
            // Get the method signature
            TextPoint signatureStart = function.GetStartPoint(vsCMPart.vsCMPartHeader);
            TextPoint signatureEnd = function.GetStartPoint(vsCMPart.vsCMPartBody);
            EditPoint signatureEditPoint = signatureStart.CreateEditPoint();
            string signature = signatureEditPoint.GetText(signatureEnd).Trim();

            // Get the method body
            TextPoint bodyStart = function.GetStartPoint(vsCMPart.vsCMPartBody);
            TextPoint bodyEnd = function.GetEndPoint(vsCMPart.vsCMPartBody);
            EditPoint bodyEditPoint = bodyStart.CreateEditPoint();
            string body = bodyEditPoint.GetText(bodyEnd).Trim();

            // Combine the signature and body
            return signature + Environment.NewLine + Environment.NewLine + body + Environment.NewLine + "}";
        }
    }


    public class MethodInfo
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public string SourceCode { get; set; }
    }
}
