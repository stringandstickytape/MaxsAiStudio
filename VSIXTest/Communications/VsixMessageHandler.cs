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

        public async Task HandleReceivedMessageAsync(VsixMessage message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            switch (message.MessageType)
            {
                case "vsButtons":

                    Buttons = JsonConvert.DeserializeObject<List<SharedClasses.Models.MessagePrompt>>(message.Content);


                    var groupedButtons = Buttons.GroupBy(b => b.Category).ToList();

                    await _executeScriptAsync($@"window.clearAllButtons();");


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
                    await HandleSetUserPromptAsync(message.Content);
                    break;
                case "vsixui":
                    await HandleVsixUiAsync(message.Content);
                    break;
                case "webviewJsCall":
                    await HandleWebviewJsCallAsync(message.Content);
                    break;

                default:
                    System.Diagnostics.Debug.WriteLine($"Unknown message type: {message.MessageType}");
                    break;
            }
        }

        private async Task HandleSetUserPromptAsync(string content)
        {

            await _executeScriptAsync($"setUserPrompt('{content}')");
        }

        private async Task HandleVsixUiAsync(string content)
        {

            var uiMessage = JsonConvert.DeserializeObject<VsixUiMessage>(content);

            await _executeScriptAsync($"handleUiMessage({JsonConvert.SerializeObject(uiMessage)})");
        }

        private async Task HandleWebviewJsCallAsync(string content)
        {

            await _executeScriptAsync(content);
        }

        public async Task SendVsixMessageAsync(VsixMessage vsixMessage, SimpleClient client)
        {
            await client.SendLineAsync(JsonConvert.SerializeObject(vsixMessage));
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

                        System.Diagnostics.Debug.WriteLine($"Error processing item: {ex.Message}");
                    }
                }
            }
        }
    }
}