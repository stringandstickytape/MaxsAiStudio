using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using SharedClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSIXTest.FileGroups;
using SharedClasses.Models;
using VSIXTest.Embeddings;

namespace VSIXTest.UI
{
    public class QuickButtonManager
    {
        private readonly DTE2 _dte;
        private readonly VsixMessageHandler _messageHandler;
        private readonly SimpleClient _simpleClient;
        private readonly ContentFormatter _contentFormatter;
        private readonly FileGroupManager _fileGroupManager;
        private readonly ShortcutManager _shortcutManager;
        private readonly Func<string, Task<string>> _executeScriptAsync;
        private readonly VSIXTestPackage _package;
        private readonly VsixChat _vsixChat;

        private QuickButtonOptionsWindow QuickButtonOptionsWindow { get; set; }

        public QuickButtonManager(
            DTE2 dte,
            VsixMessageHandler messageHandler,
            SimpleClient simpleClient,
            ContentFormatter contentFormatter,
            FileGroupManager fileGroupManager,
            ShortcutManager shortcutManager,
            Func<string, Task<string>> executeScriptAsync,
            VSIXTestPackage package,
            VsixChat vsixChat)
        {
            _dte = dte;
            _messageHandler = messageHandler;
            _simpleClient = simpleClient;
            _contentFormatter = contentFormatter;
            _fileGroupManager = fileGroupManager;
            _shortcutManager = shortcutManager;
            _executeScriptAsync = executeScriptAsync;
            _package = package;
            this._vsixChat = vsixChat;
        }

        public void ShowQuickButtonOptionsWindow(VsixUiMessage message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ToolWindowPane window;
            window = _package.FindToolWindow(typeof(QuickButtonOptionsWindow), 0, true);  // Use _package instead
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            QuickButtonOptionsWindow = window as QuickButtonOptionsWindow;
            QuickButtonOptionsWindow.SetMessage(message);
            if (!QuickButtonOptionsWindow.EventsAttached)
            {
                QuickButtonOptionsWindow.OptionsControl.OptionsSelected += OptionsControl_OptionsSelected;
                QuickButtonOptionsWindow.OptionsControl.FileGroupsEditorInvoked += OptionsControl_FileGroupsEditorInvoked;
                QuickButtonOptionsWindow.EventsAttached = true;
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        private void OptionsControl_FileGroupsEditorInvoked(object sender, string e)
        {
            var availableFiles = _shortcutManager.GetAllFilesInSolution()
                .Where(x => !x.Contains("\\.nuget\\"))
                .ToList();
            var solutionName = _dte?.Solution?.FullName;
            _fileGroupManager.DeselectAllFileGroups();
            var editWindow = new FileGroupEditWindow(_fileGroupManager.GetAllFileGroups(solutionName), availableFiles);

            bool? result = editWindow.ShowDialog();

            if (result == true)
            {
                var editedFileGroups = editWindow.EditedFileGroups;
                _fileGroupManager.UpdateAllFileGroups(editedFileGroups, _dte?.Solution?.FullName);

                var selectedFileGroups = string.Join(", ", editedFileGroups.Where(x => x.Selected).Select(x => x.Name));
                QuickButtonOptionsWindow.OptionsControl.txtFileGroups.Text = selectedFileGroups;
            }
        }

        private async void OptionsControl_OptionsSelected(object sender, QuickButtonMessageAndOptions e)
        {
            var buttonLabel = e.OriginalVsixMessage.content;
            var matchingButton = _messageHandler.Buttons?.FirstOrDefault(x => x.ButtonLabel == buttonLabel);

            var prompt = await GetPromptAsync(matchingButton);
            var inclusions = await GetInclusionsAsync(e.SelectedOptions);

            var formattedAll = $"\n{string.Join("\n\n", inclusions)}\n\n{prompt}";

            if (e.SelectedOptions.Any(x => x.Option == "Embeddings"))
            {
                //await VsixEmbeddingsHelper.CreateEmbeddingsAsync(_dte);

                formattedAll = await VsixEmbeddingsHelper.GetEmbeddingsAsync(_dte, formattedAll);
            }


            var jsonFormattedAll = JsonConvert.SerializeObject(formattedAll);

            await _executeScriptAsync($"setUserPrompt({jsonFormattedAll})");

            if (e.SelectedOptions.Any(x => x.Option == "Embeddings")) return;

                var systemPrompt = await _executeScriptAsync($"getSystemPrompt()");

            await _messageHandler.SendVsixMessageAsync(
                new VsixMessage
                {
                    MessageType = "setSystemPrompt",
                    Content = systemPrompt
                },
                _simpleClient);

            var tool = GetToolType(matchingButton, e.ResponseType);



            if (VsixChat.NewUi)
            {

                //string jsonMessage = JsonConvert.SerializeObject(new
                //{
                //    type = "setPrompt",
                //    content = formattedAll
                //});
                //// Execute JavaScript that posts the message to the page
                //await _vsixChat.CoreWebView2.ExecuteScriptAsync(
                //    $"window.postMessage({jsonMessage}, '*');"
                //);

                await _vsixChat.CoreWebView2.ExecuteScriptAsync(
                    $"window.appendToPrompt({jsonFormattedAll});"
                );
            }
            else
            {
                await _messageHandler.SendVsixMessageAsync(
                new VsixMessage
                {
                    MessageType = "vsQuickButtonRun",
                    Content = formattedAll,
                    Tool = tool
                },
                _simpleClient);
            }
        }

        private async Task<string> GetPromptAsync(MessagePrompt matchingButton)
        {
            if (string.IsNullOrEmpty(matchingButton?.Prompt))
            {
                return JsonConvert.DeserializeObject<string>(
                    await _executeScriptAsync("getUserPrompt()"));
            }
            return matchingButton.Prompt;
        }

        private async Task<List<string>> GetInclusionsAsync(List<OptionWithParameter> options)
        {
            var inclusions = new List<string>();
            var activeDocumentFilename = _dte?.ActiveDocument?.FullName;

            foreach (var option in options)
            {
                string content = await _contentFormatter.GetContentForOptionAsync(option, activeDocumentFilename);
                if (!string.IsNullOrEmpty(content))
                {
                    inclusions.Add(content);
                }
            }


            return inclusions;
        }

        private string GetToolType(MessagePrompt matchingButton, string responseType)
        {
            return responseType == "FileChanges" ? "DiffChange11" : matchingButton?.Tool;
        }
    }
}