using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SharedClasses;
using System;
using SharedClasses.Helpers;
using System.Linq;
using System.IO;
using System.Threading;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell.Interop;
using System.Windows.Documents;
using System.Collections.Generic;
using VSIXTest.FileGroups;
using Microsoft.VisualStudio.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using SharedClasses.Models;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.TextManager.Interop;
using VSIXTest.UI;

namespace VSIXTest
{
    public class VsixChat : WebView2
    {
        private readonly SimpleClient simpleClient = new SimpleClient();
        private readonly ContentFormatter _contentFormatter;
        private readonly VsixWebViewManager _webViewManager;

        private static VsixChat _instance;
        public static VsixChat Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new VsixChat();
                }
                return _instance;
            }
        }

        public static VSIXTestPackage VsixPackage { get; set; }

        private readonly DTE2 _dte;
        private readonly ResourceManager _resourceManager;
        public readonly VsixMessageHandler MessageHandler;
        private readonly ShortcutManager _shortcutManager;
        private readonly AutocompleteManager _autocompleteManager;
        private readonly FileGroupManager _fileGroupManager;
        private readonly VsixMessageProcessor _messageProcessor;

        private Changeset CurrentChangeset { get; set; }

        private async void VsixChat_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.End)
            {
                e.Handled = true;

                bool shiftHeld = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                bool ctrlHeld = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
                bool altHeld = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);

                // Call the JavaScript function, passing modifier key states
                await ExecuteScriptAsync($"window.moveCaretToEnd({shiftHeld.ToString().ToLower()}, {ctrlHeld.ToString().ToLower()}, {altHeld.ToString().ToLower()})");
            }
            else if (e.Key == Key.Home)
            {
                e.Handled = true;
                bool shiftHeld = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                await ExecuteScriptAsync($"window.moveCaretToStart({(shiftHeld ? "true" : "false")})");
            }
        }

        public VsixChat() : base()
        {
            this.KeyDown += VsixChat_KeyDown;
            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            Loaded += VsixChat_Loaded;
            _resourceManager = new ResourceManager(Assembly.GetExecutingAssembly());
            MessageHandler = new VsixMessageHandler(ExecuteScriptAsync);
            _shortcutManager = new ShortcutManager(_dte);
            _autocompleteManager = new AutocompleteManager(_dte);

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string extensionDataPath = Path.Combine(appDataPath, "MaxsAiStudio", "Vsix");
            Directory.CreateDirectory(extensionDataPath); // Ensure the directory exists

            _fileGroupManager = new FileGroupManager(extensionDataPath);
            _contentFormatter = new ContentFormatter(_dte, _fileGroupManager);
            _webViewManager = new VsixWebViewManager(this, new ButtonManager());

            simpleClient.LineReceived += SimpleClient_LineReceived;
            WebMessageReceived += WebView_WebMessageReceived;

            _messageProcessor = new VsixMessageProcessor(
                _dte,
                MessageHandler,
                simpleClient,
                _contentFormatter,
                _shortcutManager,
                this);
                    }

        public async void RunTestCompletion()
        {
            await MessageHandler.SendVsixMessageAsync(new VsixMessage { MessageType = "vsRunCompletion", JsonObject = JsonConvert.SerializeObject("lol"), Content = "Test" }, simpleClient);
        }
        public async void ContinueTestCompletion(string guid)
        {
            await MessageHandler.SendVsixMessageAsync(new VsixMessage { MessageType = "vsContinueCompletion", JsonObject = JsonConvert.SerializeObject(guid), Content = "Test" }, simpleClient);
        }

        public async Task SetSolutionSystemPrompt()
        {
            var solutionPath = _dte.Solution.FullName;

            if (!string.IsNullOrWhiteSpace(solutionPath))
            {
                var systemPromptFilePath = Path.Combine(Path.GetDirectoryName(solutionPath), "systemprompt.txt");
                if (systemPromptFilePath != null)
                {
                    // set the system prompt in the webview
                    var systemPrompt = File.ReadAllText(systemPromptFilePath);
                    var x = await ExecuteScriptAsync($"updateSystemPrompt({JsonConvert.SerializeObject(systemPrompt)})");//0 window.buttonControls['Set System Prompt from Solution'].show()
                }
            }
        }

        private async void SimpleClient_LineReceived(object sender, string e)
        {
            var vsixMessage = JsonConvert.DeserializeObject<VsixMessage>(e);
            await MessageHandler.HandleReceivedMessageAsync(vsixMessage);
        }

        private readonly ButtonManager _buttonManager = new ButtonManager();

        private async void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            await CoreWebView2.ExecuteScriptAsync(_buttonManager.GenerateButtonScript());
        }

        private bool vsixInitialised = false;

        private async void VsixChat_Loaded(object sender, RoutedEventArgs e)
        {
            if (!vsixInitialised) 
            {
                await simpleClient.StartClientAsync();
                await InitialiseAsync(); 
                vsixInitialised = true;
            }
        }

        public async Task InitialiseAsync()
        {
            await _webViewManager.InitializeAsync();
        }


        private void CoreWebView2_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            ReturnCoreWebView2Request(e, CoreWebView2);
        }

        private static void ReturnCoreWebView2Request(CoreWebView2WebResourceRequestedEventArgs e, CoreWebView2 coreWebView2)
        {
            var rd = AssemblyHelper.GetResourceDetails();
            var matching = rd.Where(x => e.Request.Uri == x.Uri).ToList();


            AssemblyHelper.GetResourceDetails().Where(x => e.Request.Uri.Equals(x.Uri, StringComparison.OrdinalIgnoreCase)).ToList().ForEach
                // (x => ReturnResourceToWebView(e, x.ResourceName, x.MimeType));
                (x =>
                {
                    var assembly = Assembly.GetExecutingAssembly();

                    // if resourcename doesn't exist in that assembly...
                    if (!assembly.GetManifestResourceNames().Contains(x.ResourceName))
                    {
                        assembly = Assembly.Load("SharedClasses");
                    }



                    using (Stream stream = assembly.GetManifestResourceStream(x.ResourceName))
                    {
                        if (stream != null)
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                string content = reader.ReadToEnd();
                                var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
                                var response = coreWebView2.Environment.CreateWebResourceResponse(memoryStream, 200, "OK", $"Content-Type: {x.MimeType}");
                                e.Response = response;
                                e.Response.Headers.AppendHeader("Access-Control-Allow-Origin", "*");
                                return;
                            }
                        }
                        throw new Exception("Probably forgot to embed the resource :(");
                    }
                }
                );
        }

        public async Task AddContextMenuItemAsync(string label, string messageType)
        {
            await _webViewManager.AddContextMenuItemAsync(label, messageType);
        }


        private async void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var message = JsonConvert.DeserializeObject<VsixUiMessage>(e.WebMessageAsJson);
            await _messageProcessor.ProcessMessageAsync(message);
        }

        private bool _changesetPaneInitted = false;

 
        private async void ShowChangesetPopup(List<Change> changes)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var window = await VSIXTestPackage.Instance.FindToolWindowAsync(
                    typeof(ChangesetReviewPane), 
                    0, 
                    true, 
                    VSIXTestPackage.Instance.DisposalToken) as ChangesetReviewPane;

                if (window?.Frame == null)
                    throw new NotSupportedException("Cannot create changeset review window");

                if (!_changesetPaneInitted)
                {
                    _changesetPaneInitted = true;
                    window.ChangeApplied += ChangesetReviewWindow_ChangeApplied;
                    window.RunMerge += ChangesetReviewWindow_RunMerge;
                }
                window.Initialize(changes);

                IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing changeset window: {ex}");
            }
        }

        private async void ChangesetReviewWindow_RunMerge(object sender, RunMergeEventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (e.Changes != null)
                {
                    await RunMergeAsync(e.Changes);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying change 2: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ChangesetReviewWindow_ChangeApplied(object sender, ChangeAppliedEventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (e.Change != null)
                {
                    await ApplyChangeAsync(e.Change);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying change 2: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RunMergeAsync(List<Change> changes)
        {
            await MessageHandler.SendVsixMessageAsync(new VsixMessage { MessageType = "vsRunMerge", Content = JsonConvert.SerializeObject(changes) }, simpleClient);
        }

        private async Task ApplyChangeAsync(Change change)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var changeType = change.ChangeType;
            var path = change.Path;
             
            try
            {
                switch (changeType)
                {
                    case "createnewFile":
                        {
                            string deserNewContent = change.NewContent;

                            try
                            {
                                deserNewContent = JsonConvert.DeserializeObject<string>($"\"{(change.NewContent ?? "")}\"");
                            }
                            catch (Exception e)
                            {
                                // Handle exception if needed
                            }

                            var directoryPath = Path.GetDirectoryName(path);

                            if (!Directory.Exists(directoryPath))
                            {
                                Directory.CreateDirectory(directoryPath);
                            }

                            // Create empty file first
                            File.WriteAllText(path, string.Empty);

                            // Open the file in VS
                            var window = _dte.ItemOperations.OpenFile(path, EnvDTE.Constants.vsViewKindCode);

                            if (window == null)
                            {
                                Debug.WriteLine($"Path not found: {path}");
                                throw new Exception($"Path not found: {path}");
                            }

                            await Task.Yield(); // Give VS a chance to complete the file opening

                            window.Activate();
                            var document = window.Document;

                            if (document == null)
                            {
                                Debug.WriteLine("Document is null");
                                throw new Exception($"Document is null");
                            }

                            var textDocument = document.Object() as TextDocument;
                            if (textDocument == null)
                            {
                                Debug.WriteLine("TextDocument is null");
                                throw new Exception($"TextDocument is null");
                            }

                            // Get the edit point and insert the content
                            var editPoint = textDocument.StartPoint.CreateEditPoint();
                            editPoint.Insert(deserNewContent);

                            // Optionally, move to the beginning of the document
                            textDocument.Selection.MoveToPoint(textDocument.StartPoint);

                            await Task.Yield(); // Give VS a chance to process the edit

                            // If you want to save the file
                            document.Save();
                        }
                        break;

                    case "addToFile":
                    case "deleteFromFile":
                    case "modifyFile":
                        {
                            var lineNumber = change.LineNumber;
                            var window = _dte.ItemOperations.OpenFile(path, EnvDTE.Constants.vsViewKindCode);

                            if (window == null)
                            {
                                Debug.WriteLine($"Path not found: {path}");
                                throw new Exception($"Path not found: {path}");
                            }

                            await Task.Yield(); // Give VS a chance to complete the file opening

                            window.Activate();
                            var document = window.Document;

                            if (document == null)
                            {
                                
                                Debug.WriteLine("Document is null");
                                throw new Exception($"Document is null");
                            }

                            var textDocument = document.Object() as TextDocument;
                            if (textDocument == null)
                            {
                                Debug.WriteLine("TextDocument is null");
                                throw new Exception($"TextDocument is null");
                            }

                            var editPoint = textDocument.StartPoint.CreateEditPoint();
                            var fullText = editPoint.GetText(textDocument.EndPoint);

                            var outp = new TextReplacer().ReplaceTextAtHint(fullText, change.OldContent, change.NewContent, lineNumber);

                            editPoint.StartOfDocument();
                            editPoint.Delete(textDocument.EndPoint);
                            editPoint.Insert(outp);

                            var line = textDocument.StartPoint.CreateEditPoint();
                            line.LineDown(lineNumber - 1);
                            textDocument.Selection.MoveToPoint(line);

                            await Task.Yield(); // Give VS a chance to process the edit
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying change 3: {ex}");
                MessageBox.Show($"Error applying change 4: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ShowFileWithMembersSelectionWindow()
        {
            var solutionDetails = _shortcutManager.GetAllFilesInSolutionWithMembers();

            var selectionWindow = new FileWithMembersSelectionWindow(solutionDetails);
            selectionWindow.Show();
        }


        QuickButtonOptionsWindow QuickButtonOptionsWindow { get; set; }

        public void ShowQuickButtonOptionsWindow(VsixUiMessage message)
        {
            ToolWindowPane window;
            window = VsixPackage.FindToolWindow(typeof(QuickButtonOptionsWindow), 0, true);
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

            ThreadHelper.ThrowIfNotOnUIThread();

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        private void OptionsControl_FileGroupsEditorInvoked(object sender, string e)
        {
            // bodged for now
            var availableFiles = _shortcutManager.GetAllFilesInSolution().Where(x => !x.Contains("\\.nuget\\")).ToList();
            var solutionName = _dte?.Solution?.FullName;
            _fileGroupManager.DeselectAllFileGroups();
            var editWindow = new FileGroupEditWindow(_fileGroupManager.GetAllFileGroups(solutionName), availableFiles);

            bool? result = editWindow.ShowDialog();

            if (result == true)
            {
                var editedFileGroups = editWindow.EditedFileGroups;

                _fileGroupManager.UpdateAllFileGroups(editedFileGroups, _dte?.Solution?.FullName);

                // get the names of all the selected filegroups
                var selectedFileGroups =string.Join(", ",  editedFileGroups.Where(x => x.Selected).Select(x => x.Name));

                // get the txtFileGroups control from the options control
                QuickButtonOptionsWindow.OptionsControl.txtFileGroups.Text = selectedFileGroups;

            }
        }

        private async void OptionsControl_OptionsSelected(object sender, QuickButtonMessageAndOptions e)
        {
            var buttonLabel = e.OriginalVsixMessage.content;
            var matchingButton = MessageHandler.Buttons.FirstOrDefault(x => x.ButtonLabel == buttonLabel);

            var prompt = "";

            if (string.IsNullOrEmpty(matchingButton?.Prompt))
            {
                prompt = JsonConvert.DeserializeObject<string>(await ExecuteScriptAsync("getUserPrompt()"));
            }
            else
                prompt = matchingButton?.Prompt;

            var inclusions = new List<string>();
            var activeDocumentFilename = _dte?.ActiveDocument?.FullName;

            foreach (var option in e.SelectedOptions)
            {
                string content = GetContentForOption(option, activeDocumentFilename);
                if (!string.IsNullOrEmpty(content))
                {
                    inclusions.Add(content);
                }
            }

            var formattedAll = $"\n{string.Join("\n\n", inclusions)}\n\n{prompt}";
            var jsonFormattedAll = JsonConvert.SerializeObject(formattedAll);

            await ExecuteScriptAsync($"setUserPrompt({jsonFormattedAll})");

            var systemPrompt = await ExecuteScriptAsync($"getSystemPrompt()");

            await MessageHandler.SendVsixMessageAsync(new VsixMessage { MessageType = "setSystemPrompt", Content = systemPrompt }, simpleClient);

            var tool = matchingButton?.Tool;

            // Check if File Changes radio button is selected
            if (e.ResponseType == "FileChanges")
            {
                tool = "DiffChange11";
            }

            await MessageHandler.SendVsixMessageAsync(new VsixMessage { MessageType = "vsQuickButtonRun", Content = formattedAll, Tool = tool }, simpleClient);
        }

        private string GetContentForOption(OptionWithParameter option, string activeDocumentFilename)
        {
            return _contentFormatter.GetContentForOption(option, activeDocumentFilename);
        }


    }
}