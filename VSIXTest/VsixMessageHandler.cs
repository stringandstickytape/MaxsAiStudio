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
using System.Threading.Tasks;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace VSIXTest
{
    public class VsixMessageHandler
    {
        private readonly DTE2 _dte;
        private readonly Func<string, Task> _executeScriptAsync;
        public VsixMessageHandler(DTE2 dte, Func<string, Task> executeScriptAsync)
        {
            _dte = dte;
            _executeScriptAsync = executeScriptAsync;
        }

        private List<SharedClasses.Models.MessagePrompt> _buttons;

        public async Task HandleReceivedMessage(VsixMessage message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            switch (message.MessageType)
            {
                case "vsButtons":
                    //deser message.Content to list of strings
                    _buttons = JsonConvert.DeserializeObject<List<SharedClasses.Models.MessagePrompt>>(message.Content);

                    // group buttons by category
                    var groupedButtons = _buttons.GroupBy(b => b.Category).ToList();

                    //foreach cat
                    foreach (var cat in groupedButtons)
                    {
                        var catButtons = cat.ToList();

                        var catButtonObjs = catButtons.Select(b => new { label = b.ButtonLabel, onClick = "console.log(\"Sub action clicked\")" }).ToList();

                        //var catButtonJson = "["+string.Join(",", catButtonObjs.Select(x => $"{{ label: \"{x.label}\", onClick: () => console.log(\"{x.label} clicked\") }}"))+"]";

                        var catButtonJson = "[" + string.Join(",", catButtonObjs.Select(x => $"{{ label: \"{x.label}\", onClick: () => window.chrome.webview.postMessage({{type: 'vsQuickButton', content: '{x.label}'}}) }}")) + "]";


                        //                 window.chrome.webview.postMessage({type: '')

                    var catButtonJson2 = "[{ label: \"Sub Action\", onClick: () => console.log(\"Sub action clicked\") }]";

                        await _executeScriptAsync($@"window.addQuickActionButton(
    ""{cat.Key}"", 
    () => console.log(""Action clicked""), 
    {catButtonJson},
    null
);");

                    }

                    foreach(var button in _buttons)
                    {

                    }
                    break;
                case "setUserPrompt":
                    await HandleSetUserPrompt(message.Content);
                    break;
                case "vsixui":
                    await HandleVsixUi(message.Content);
                    break;
                case "webviewJsCall":
                    await HandleWebviewJsCall(message.Content);
                    break;
                // Add more cases as needed
                default:
                    System.Diagnostics.Debug.WriteLine($"Unknown message type: {message.MessageType}");
                    break;
            }
        }

        private async Task HandleSetUserPrompt(string content)
        {
            // Handle setting user prompt
            await _executeScriptAsync($"setUserPrompt('{content}')");
        }

        private async Task HandleVsixUi(string content)
        {
            // Handle UI-related messages
            var uiMessage = JsonConvert.DeserializeObject<VsixUiMessage>(content);
            // Process UI message as needed
            await _executeScriptAsync($"handleUiMessage({JsonConvert.SerializeObject(uiMessage)})");
        }

        private async Task HandleWebviewJsCall(string content)
        {
            // Execute JavaScript in WebView2
            await _executeScriptAsync(content);
        }

        public async Task SendVsixMessage(VsixMessage vsixMessage, SimpleClient client)
        {
            await client.SendLine(JsonConvert.SerializeObject(vsixMessage));
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