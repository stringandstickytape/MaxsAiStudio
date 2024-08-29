using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SharedClasses;

namespace VSIXTest
{
    public class VsixMessageHandler
    {
        private readonly DTE2 _dte;

        public VsixMessageHandler(DTE2 dte)
        {
            _dte = dte;
        }

        public void SendPrompt(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                // Handle filename hashtags and other processing
                var files = GetAllFilesInSolution();

                foreach (var file in files)
                {
                    message = ReplaceFileNameWithContent(message, file);
                }

                // Replace #:all-open: with contents of all open code windows
                if (message.Contains(BacktickHelper.PrependHash(":all-open:")))
                {
                    message = ReplaceAllOpenContents(message);
                }

                // replace any '<hash>:selection:' with the selected text
                if (message.Contains(BacktickHelper.PrependHash(":selection:")) && _dte.ActiveDocument != null)
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    var selection = (TextSelection)_dte.ActiveDocument.Selection;
                    var documentFilename = _dte.ActiveDocument.Name;
                    string textToInsert = "";

                    if (selection.IsEmpty)
                    {
                        // If selection is empty, get the entire text of the document
                        TextDocument textDocument = _dte.ActiveDocument.Object("TextDocument") as TextDocument;
                        EditPoint startPoint = textDocument.StartPoint.CreateEditPoint();
                        textToInsert = startPoint.GetText(textDocument.EndPoint);
                    }
                    else
                    {
                        // If there's a selection, use the selected text
                        textToInsert = selection.Text;
                    }

                    message = MessageFormatter.InsertFilenamedSelection(message, documentFilename, textToInsert);
                }

                if (message.Contains(BacktickHelper.PrependHash(":diff:")))
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    var gitDiffHelper = new GitDiffHelper();
                    var diff = gitDiffHelper.GetGitDiff();
                    message = message.Replace(BacktickHelper.PrependHash(":diff:"), diff);
                }

                var vsixOutgoingMessage = new VsixMessage { Content = message, MessageType = "prompt" };
                string jsonMessage = JsonConvert.SerializeObject(vsixOutgoingMessage);

                VSIXTestPackage.Instance.SendMessageThroughPipe(jsonMessage); // messagetype is p (for prompt)
            }
        }

        public void SendVsixMessage(VsixMessage vsixMessage)
        {
            VSIXTestPackage.Instance.SendMessageThroughPipe(JsonConvert.SerializeObject(vsixMessage));
        }

        public void SendNewConversationMessage()
        {
            VSIXTestPackage.Instance.SendMessageThroughPipe(JsonConvert.SerializeObject(new VsixMessage { MessageType = "new" }));

        }

        public void HandleDefaultMessage(string messageType)
        {
            var insertionType = messageType == "commitMsg" ? BacktickHelper.PrependHash(":diff:") : BacktickHelper.PrependHash(":selection:");

            var matchingPrompt = ButtonManager.MessagePrompts.FirstOrDefault(mp => mp.MessageType == messageType);
            if (matchingPrompt != null)
            {
                string prompt = $"{Environment.NewLine}{Environment.NewLine}{insertionType}{Environment.NewLine}{matchingPrompt.Prompt}";
                SendNewConversationMessage();
                SendPrompt(prompt);
            }
        }

        private string ReplaceFileNameWithContent(string message, string file)
        {
            string fileName = $"#{Path.GetFileName(file)}";
            if (message.IndexOf(fileName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                ProjectItem projectItem = _dte.Solution.FindProjectItem(file);
                if (projectItem != null)
                {
                    EnvDTE.Window window = projectItem.Open();
                    if (window != null)
                    {
                        try
                        {
                            message = ReplaceFileNameWithContentHelper(message, fileName, window);
                        }
                        finally
                        {
                            window.Close();
                        }
                    }
                }
            }
            return message;
        }

        private static string ReplaceFileNameWithContentHelper(string message, string fileName, EnvDTE.Window window)
        {
            TextDocument textDoc = window.Document.Object("TextDocument") as TextDocument;
            if (textDoc != null)
            {
                string fileContent = textDoc.StartPoint.CreateEditPoint().GetText(textDoc.EndPoint);
                string backticks = new string('`', 3);
                string replacement = $"\n{backticks}\n{fileContent}\n{backticks}\n";
                return ReplaceIgnoreCase(message, fileName, replacement);
            }
            return message;
        }

        private static string ReplaceIgnoreCase(string source, string oldValue, string newValue)
        {
            int index = source.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                return source;

            StringBuilder result = new StringBuilder();
            int previousIndex = 0;

            while (index >= 0)
            {
                result.Append(source, previousIndex, index - previousIndex);
                result.Append(newValue);
                index += oldValue.Length;
                previousIndex = index;
                index = source.IndexOf(oldValue, index, StringComparison.OrdinalIgnoreCase);
            }

            result.Append(source, previousIndex, source.Length - previousIndex);

            return result.ToString();
        }

        private string ReplaceAllOpenContents(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            StringBuilder allOpenContents = new StringBuilder();

            foreach (EnvDTE.Window window in _dte.Windows)
            {
                if (window.Kind == "Document" && window.Document != null)
                {
                    TextDocument textDoc = window.Document.Object("TextDocument") as TextDocument;
                    if (textDoc != null)
                    {
                        string fileName = window.Document.Name;
                        string fileContent = textDoc.StartPoint.CreateEditPoint().GetText(textDoc.EndPoint);
                        allOpenContents.AppendLine($"File: {fileName}\n{BacktickHelper.ThreeTicks}\n{fileContent}\n{BacktickHelper.ThreeTicks}\n");
                    }
                }
            }

            return message.Replace(BacktickHelper.PrependHash(":all-open:"), allOpenContents.ToString().TrimEnd());
        }

        private List<string> GetAllFilesInSolution()
        {
            var files = new List<string>();
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_dte.Solution != null)
            {
                foreach (Project project in _dte.Solution.Projects)
                {
                    GetProjectFiles(project, files);
                }
            }

            return files;
        }

        private void GetProjectFiles(Project project, List<string> files)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project == null)
                return;

            if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
            {
                if (project.ProjectItems != null)
                    foreach (ProjectItem item in project.ProjectItems)
                    {
                        if (item.SubProject != null)
                        {
                            GetProjectFiles(item.SubProject, files);
                        }
                        else
                        {
                            ProcessProjectItem(item, files);
                        }
                    }
            }
            else
            {
                if (project.ProjectItems != null)
                    foreach (ProjectItem item in project.ProjectItems)
                    {
                        ProcessProjectItem(item, files);
                    }
            }
        }

        private void ProcessProjectItem(ProjectItem item, List<string> files)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (item == null)
                return;

            if (item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder)
            {
                foreach (ProjectItem subItem in item.ProjectItems)
                {
                    ProcessProjectItem(subItem, files);
                }
            }
            else
            {
                if (item.Properties != null)
                {
                    try
                    {
                        string filePath = item.Properties.Item("FullPath").Value.ToString();
                        if (File.Exists(filePath))
                        {
                            files.Add(filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle or log the exception
                        System.Diagnostics.Debug.WriteLine($"Error processing item: {ex.Message}");
                    }
                }
            }
        }
    }
}