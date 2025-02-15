using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using EnvDTE80;
using VSIXTest.FileGroups;
using VSIXTest.Embeddings;
using System.Threading.Tasks;

namespace VSIXTest
{
    public class ContentFormatter
    {
        private readonly DTE2 _dte;
        private readonly FileGroupManager _fileGroupManager;

        public ContentFormatter(DTE2 dte, FileGroupManager fileGroupManager)
        {
            _dte = dte;
            _fileGroupManager = fileGroupManager;
        }

        public string FormatContent(string filename, string content)
        {
            return $"\n{MessageFormatHelper.FormatFile(filename, content)}\n";
        }

        public string GetCurrentSelection()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var textDocument = _dte.ActiveDocument.Object("TextDocument") as TextDocument;
            var selection = textDocument.Selection as TextSelection;
            return selection.Text;
        }

        public string GetCurrentFileContent()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var textDocument = _dte.ActiveDocument.Object("TextDocument") as TextDocument;
            return textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
        }

        public string FormatXmlDocContent(string parameter)
        {
            var matchingMethods = new MethodFinder().FindMethods(parameter);
            return string.Join("\n\n", matchingMethods.Select(x => MessageFormatHelper.FormatFile(x.FileName, x.SourceCode)));
        }

        public string FormatFileGroupsContent()
        {
            var selectedFileGroups = _fileGroupManager.GetSelectedFileGroups();
            var filesIncluded = new HashSet<string>();
            var formattedFiles = new List<string>();

            foreach (var file in selectedFileGroups.SelectMany(group => group.FilePaths))
            {
                if (filesIncluded.Add(file))
                {
                    var fileContent = AddLineNumbers(File.ReadAllText(file));
                    formattedFiles.Add(FormatContent(file, fileContent));
                }
            }

            return string.Join("\n", formattedFiles);
        }

        public static string AddLineNumbers(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            string[] lines = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int digitCount = lines.Length.ToString().Length;

            return string.Join(Environment.NewLine,
                lines.Select((line, index) =>
                    $"{(index + 1).ToString().PadLeft(digitCount)} | {line}"));
        }

        private string GetAllOpenFilesContent()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var formattedFiles = new List<string>();
            var filesIncluded = new HashSet<string>();

            foreach (Document doc in _dte.Documents)
            {
                try
                {
                    if (doc.Object("TextDocument") is TextDocument textDoc)
                    {
                        if (filesIncluded.Add(doc.FullName))
                        {
                            var content = textDoc.StartPoint.CreateEditPoint().GetText(textDoc.EndPoint);
                            formattedFiles.Add(FormatContent(doc.FullName, AddLineNumbers(content)));
                        }
                    }
                }
                catch (Exception)
                {
                    // Skip non-text documents or those that can't be accessed
                    continue;
                }
            }

            return string.Join("\n", formattedFiles);
        }

        public async Task<string> GetContentForOptionAsync(OptionWithParameter option, string activeDocumentFilename)
        {
            switch (option.Option)
            {
                case "CurrentSelection":
                    return FormatContent(activeDocumentFilename, GetCurrentSelection());
                case "Clipboard":
                    return FormatContent(activeDocumentFilename, Clipboard.GetText());
                case "CurrentFile":
                    return FormatContent(activeDocumentFilename, AddLineNumbers(GetCurrentFileContent()));
                case "GitDiff":
                    return FormatContent("diff", new GitDiffHelper().GetGitDiff());
                case "XmlDoc":
                    return FormatXmlDocContent(option.Parameter);
                case "FileGroups":
                    return FormatFileGroupsContent();
                case "AllOpenFiles":
                    return GetAllOpenFilesContent();
                default:
                    return null;
            }
        }
    }
}