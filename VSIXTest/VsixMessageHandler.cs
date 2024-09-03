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
        private readonly Func<string, Task> _executeScriptAsync;
        public VsixMessageHandler(Func<string, Task> executeScriptAsync)
        {
            _executeScriptAsync = executeScriptAsync;
        }

        public List<SharedClasses.Models.MessagePrompt> Buttons;

        public async Task HandleReceivedMessage(VsixMessage message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            switch (message.MessageType)
            {
                case "vsButtons":
                    //deser message.Content to list of strings
                    Buttons = JsonConvert.DeserializeObject<List<SharedClasses.Models.MessagePrompt>>(message.Content);

                    // group buttons by category
                    var groupedButtons = Buttons.GroupBy(b => b.Category).ToList();

                    await _executeScriptAsync($@"window.clearAllButtons();");

                    //foreach cat
                    foreach (var cat in groupedButtons)
                    {
                        var catButtons = cat.ToList();

                        var catButtonObjs = catButtons.Select(b => new { label = b.ButtonLabel, onClick = "console.log(\"Sub action clicked\")" }).ToList();

                        var catButtonJson = "[" + string.Join(",", catButtonObjs.Select(x => $"{{ label: \"{x.label}\", onClick: () => window.chrome.webview.postMessage({{type: 'vsQuickButton', content: '{x.label}'}}) }}")) + "]";

                        await _executeScriptAsync($@"window.addQuickActionButton(
    ""{cat.Key}"", 
    () => console.log(""Action clicked""), 
    {catButtonJson},
    null
);");

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