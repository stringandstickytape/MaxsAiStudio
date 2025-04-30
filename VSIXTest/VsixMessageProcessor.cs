using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedClasses;
using SharedClasses.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using VSIXTest.UI;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace VSIXTest
{
    public class VsixMessageProcessor
    {
        private readonly DTE2 _dte;
        private readonly VsixMessageHandler _messageHandler;
        private readonly SimpleClient _simpleClient;
        private readonly ContentFormatter _contentFormatter;
        private readonly ShortcutManager _shortcutManager;
        private readonly VsixChat _vsixChat; 
        private readonly ChangesetManager _changesetManager;
        private bool _changesetPaneInitted = false;

        public VsixMessageProcessor(
            DTE2 dte,
            VsixMessageHandler messageHandler,
            SimpleClient simpleClient,
            ContentFormatter contentFormatter,
            ShortcutManager shortcutManager,
            VsixChat vsixChat,
            ChangesetManager changesetManager) 
        {
            _dte = dte;
            _messageHandler = messageHandler;
            _simpleClient = simpleClient;
            _contentFormatter = contentFormatter;
            _shortcutManager = shortcutManager;
            _vsixChat = vsixChat;
            _changesetManager = changesetManager;
        }

        private async Task HandleSendAsync(VsixUiMessage message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _vsixChat.ShowQuickButtonOptionsWindow(message);
        }

        private async Task HandleReadyAsync()
        {
            await _messageHandler.SendVsixMessageAsync(
                new VsixMessage { MessageType = "vsRequestButtons" },
                _simpleClient);

            await _vsixChat.AddContextMenuItemAsync("Insert Selection", "vsInsertSelection");
            await _vsixChat.AddContextMenuItemAsync("Pop Window", "vsPopWindow");
            await _vsixChat.ExecuteScriptAsync("window.buttonControls['Set System Prompt from Solution'].show()");
            await _vsixChat.ExecuteScriptAsync("window.buttonControls['Attach'].hide() ");
            await _vsixChat.ExecuteScriptAsync("window.buttonControls['Theme'].hide()  ");
            await _vsixChat.ExecuteScriptAsync("window.buttonControls['Tools'].hide()  ");


            await _vsixChat.ExecuteScriptAsync("document.querySelectorAll('div.options-bar').forEach(el => el.style.display = 'none')");
        }

        public async Task ProcessMessageAsync(VsixUiMessage message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            switch (message.type)
            {
                case "storeLoaded":
                    await _vsixChat.ExecuteScriptAsync($"localStorage.isVisualStudio = 'true'");
                    break;
                case "applyNewDiff":
                    await _changesetManager.HandleNewChangesetAsync(message.content);
                    break;
                case "setSystemPromptFromSolution":
                    await HandleSetSystemPromptFromSolutionAsync();
                    break;

                case "QuotedStringClicked":
                    await HandleQuotedStringClickedAsync(message);
                    break;

                case "send":
                    await HandleSendAsync(message);
                    break;

                case "ready":
                    await HandleReadyAsync();
                    break;

                case "vsInsertSelection":
                    await HandleInsertSelectionAsync();
                    break;

                case "vsPopWindow":
                    await HandlePopWindowAsync();
                    break;

                case "vsQuickButton":
                    await HandleQuickButtonAsync(message);
                    break;

                case "vsSelectFilesWithMembers":
                    await HandleSelectFilesWithMembersAsync();
                    break;
            }

            if (message.type != "send")
            {
                await _messageHandler.SendVsixMessageAsync(
                    new VsixMessage
                    {
                        MessageType = "vsixui",
                        Content = JsonConvert.SerializeObject(message)
                    },
                    _simpleClient);
            }
        }


        private async Task HandleSetSystemPromptFromSolutionAsync()
        {
            var solutionPath = _dte.Solution.FullName;

            if (!string.IsNullOrWhiteSpace(solutionPath))
            {
                var systemPromptFilePath = Path.Combine(Path.GetDirectoryName(solutionPath), "systemprompt.txt");
                if (File.Exists(systemPromptFilePath))
                {
                    var systemPrompt = File.ReadAllText(systemPromptFilePath);
                    await _vsixChat.ExecuteScriptAsync($"updateSystemPrompt({JsonConvert.SerializeObject(systemPrompt)})");
                }
            }
        }

        private async Task HandleQuotedStringClickedAsync(VsixUiMessage message)
        {
            var quotedString = message.content;

            if (File.Exists(quotedString))
            {
                _dte.ItemOperations.OpenFile(quotedString);
                return;
            }

            await NavigateToQuotedStringInDocuments(quotedString);
        }


        private async Task HandleInsertSelectionAsync()
        {
            var activeDocumentFilename = _dte.ActiveDocument.Name;
            var selectedText = _contentFormatter.GetCurrentSelection();
            var formattedAsFile = _contentFormatter.FormatContent(activeDocumentFilename, selectedText);

            var jsonSelectedText = JsonConvert.SerializeObject(formattedAsFile);
            await _vsixChat.ExecuteScriptAsync($"window.insertTextAtCaret({jsonSelectedText})");
        }

        private async Task HandlePopWindowAsync()
        {
            var files = _shortcutManager.GetAllFilesInSolution()
                .Where(x => !x.Contains("\\.nuget\\"))
                .ToList();

            await _simpleClient.SendLineAsync(
                JsonConvert.SerializeObject(
                    new VsixMessage
                    {
                        MessageType = "vsShowFileSelector",
                        Content = JsonConvert.SerializeObject(files)
                    }));
        }

        private async Task HandleQuickButtonAsync(VsixUiMessage message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var buttonLabel = message.content;
            var matchingButton = _messageHandler.Buttons.FirstOrDefault(x => x.ButtonLabel == buttonLabel);

            if (!string.IsNullOrEmpty(matchingButton?.Prompt))
            {
                await _vsixChat.ExecuteScriptAsync($"setUserPrompt({JsonConvert.SerializeObject(matchingButton?.Prompt)})");
            }

            VsixDebugLog.Instance.Log($"Show quick button options window: {message}");
        }

        private async Task HandleSelectFilesWithMembersAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var solutionDetails = _shortcutManager.GetAllFilesInSolutionWithMembers();
            var selectionWindow = new FileWithMembersSelectionWindow(solutionDetails);
            selectionWindow.Show();
        }



        private async Task NavigateToQuotedStringInDocuments(string quotedString)
        {
            var textDocument = _dte.ActiveDocument.Object("TextDocument") as TextDocument;
            var text = textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
            var index = text.IndexOf(quotedString);

            if (index != -1)
            {
                var textSelection = _dte.ActiveDocument.Selection as TextSelection;
                textSelection.MoveToAbsoluteOffset(index);
                textSelection.SelectLine();
                return;
            }

            foreach (Document doc in _dte.Documents)
            {
                try
                {
                    textDocument = doc.Object("TextDocument") as TextDocument;
                    text = textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
                    index = text.IndexOf(quotedString);
                    if (index != -1)
                    {
                        doc.Activate();
                        var textSelection = _dte.ActiveDocument.Selection as TextSelection;
                        textSelection.MoveToAbsoluteOffset(index);
                        textSelection.SelectLine();
                        break;
                    }
                }
                catch (Exception)
                {
                    // Continue to next document if there's an error
                }
            }
        }
    }
}