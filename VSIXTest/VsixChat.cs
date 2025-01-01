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

namespace VSIXTest
{
    public class VsixChat : WebView2
    {
        private readonly SimpleClient simpleClient = new SimpleClient();
        private readonly ContentFormatter _contentFormatter;

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

            simpleClient.LineReceived += SimpleClient_LineReceived;


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
            var env = await CoreWebView2Environment.CreateAsync(null, "C:\\temp");
            if (this.CoreWebView2 == null)
            {
                await EnsureCoreWebView2Async(env);
            }
            WebMessageReceived += WebView_WebMessageReceived;
            CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;

            foreach (var resource in AssemblyHelper.GetResourceDetails())
            {
                CoreWebView2.AddWebResourceRequestedFilter(resource.Uri, CoreWebView2WebResourceContext.All);
            }
            NavigateToString(AssemblyHelper.GetEmbeddedResource("SharedClasses", "SharedClasses.HTML.ChatWebView2.html"));

            string[] scriptResources = new[]
                    {
                "SharedClasses.JavaScriptViewers.JsonViewer.js",
                "SharedClasses.JavaScriptViewers.ThemeEditor.js",
                "SharedClasses.JavaScriptViewers.SvgViewer.js",
                "SharedClasses.JavaScriptViewers.MermaidViewer.js",
                "SharedClasses.JavaScriptViewers.DotViewer.js",
                "SharedClasses.JavaScriptViewers.FindAndReplacer.js"
            };

            foreach (var resource in scriptResources)
            {
                await ExecuteScriptAsync(AssemblyHelper.GetEmbeddedResource("SharedClasses", resource));
            }
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

        private async Task AddContextMenuItemAsync(string label, string messageType)
        {
            string script = $@"
        window.addCustomContextMenuItem({{
            label: `{label}`,
            onClick: () => window.chrome.webview.postMessage({{
                type: `{messageType}`
            }})
        }});
    ";
            await ExecuteScriptAsync(script); 
        }


        private async void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var message = JsonConvert.DeserializeObject<VsixUiMessage>(e.WebMessageAsJson);

            switch (message.type)
            {
                case "applyNewDiff":
                    {
                        var changesetObj = JsonConvert.DeserializeObject<JObject>(message.content)["changeset"];
                        var changes = changesetObj["changes"].ToObject<JArray>();

                        var ctr = 0;
                        foreach (var change in changes)
                        {
                            ctr++;
                            var changeType = change["change_type"].ToString();
                            var path = change["path"].ToString();

                            switch (changeType)
                            {
                                case "createnewFile":
                                    {
                                        var newContent = change["newContent"].ToString();
                                        var directoryPath = Path.GetDirectoryName(path);
                                        if (!Directory.Exists(directoryPath))
                                        {
                                            Directory.CreateDirectory(directoryPath);
                                        }
                                        File.WriteAllText(path, newContent);
                                        _dte.ItemOperations.OpenFile(path);
                                    }
                                    break;

                                case "addToFile":
                                case "deleteFromFile":
                                case "modifyFile":
                                    {
                                        //var lineNumber = change["lineNumber"].Value<int>();
                                        var newContent = changeType != "deleteFromFile" ? change["newContent"]?.ToString() : "";

                                        // Open or activate the file
                                        var window = _dte.ItemOperations.OpenFile(path);
                                        window.Activate();
                                        var document = window.Document;

                                        var textDocument = document.Object() as TextDocument;
                                        var editPoint = textDocument.StartPoint.CreateEditPoint();

                                        // Move to the specified line
                                        //editPoint.MoveToLineAndOffset(lineNumber, 1);

                                        if (changeType == "deleteFromFile")
                                        {
                                            // For delete, get the number of lines to delete from hunkHeader
                                            var oldCount = change["hunkHeader"]["oldCount"].Value<int>();
                                            var deletePoint = editPoint.CreateEditPoint();
                                            deletePoint.LineDown(oldCount);
                                            editPoint.Delete(deletePoint);
                                        }
                                        else if (changeType == "modifyFile")
                                        {


                                            var fullText = editPoint.GetText(textDocument.EndPoint);

                                            string decodedOldText = "";
                                            try
                                            {
                                                decodedOldText = JsonConvert.DeserializeObject<string>($"\"{(change["oldContent"]?.ToString() ?? "")}\"");
                                            }
                                            catch
                                            {
                                                decodedOldText = change["oldContent"]?.ToString() ?? "";
                                            }

                                            string decodedNewText = "";
                                            if (changeType == "modifyFile")
                                            {
                                                try
                                                {
                                                    decodedNewText = JsonConvert.DeserializeObject<string>($"\"{(change["newContent"]?.ToString() ?? "")}\"");
                                                }
                                                catch
                                                {
                                                    decodedNewText = change["newContent"]?.ToString() ?? "";
                                                }
                                            }
                                            decodedNewText = decodedNewText.Trim();
                                            decodedOldText = decodedOldText.Trim();
                                                
                                            // Find the position of the old text
                                            int startIndex = fullText.IndexOf(decodedOldText);
                                            if(startIndex < 0)
                                            {
                                                decodedOldText = decodedOldText.Replace("\n", "\r\n");
                                                startIndex = fullText.IndexOf(decodedOldText);
                                            }

                                            if (startIndex >= 0)
                                            {
                                                // Clear the document
                                                editPoint.Delete(textDocument.EndPoint);

                                                // Split the text and insert with modifications
                                                string beforeText = fullText.Substring(0, startIndex);
                                                string afterText = fullText.Substring(startIndex + decodedOldText.Length);

                                                // Insert the modified text
                                                editPoint.Insert(beforeText);
                                                if (changeType == "modifyFile")
                                                {
                                                    editPoint.Insert(decodedNewText);
                                                }
                                                editPoint.Insert(afterText);
                                            }
                                        }
                                        else // addToFile
                                        {
                                            editPoint.Insert(newContent);
                                        }

                                        // Save the document
                                        //document.Save();
                                    }
                                    break;
                            }
                        }
                    }
                    break;
                case "setSystemPromptFromSolution":
                    await SetSolutionSystemPrompt();
                    break;
                case "QuotedStringClicked":
                    {
                        var quotedString = message.content;

                        if (File.Exists(quotedString))
                        {
                            _dte.ItemOperations.OpenFile(quotedString);
                        }
                        else
                        {
                            var textDocument = _dte.ActiveDocument.Object("TextDocument") as TextDocument;
                            var text = textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
                            var index = text.IndexOf(quotedString);

                            if (index != -1)
                            {
                                var textSelection = _dte.ActiveDocument.Selection as EnvDTE.TextSelection;
                                textSelection.MoveToAbsoluteOffset(index);
                                textSelection.SelectLine();
                            }
                            else
                            {
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
                                            var textSelection = _dte.ActiveDocument.Selection as EnvDTE.TextSelection;
                                            textSelection.MoveToAbsoluteOffset(index);
                                            textSelection.SelectLine();
                                            break;
                                        }
                                    }
                                    catch(Exception)
                                    {
                                        // oh well...
                                    }
                                }
                            }
                        }
                    }
                    break;

                case "send":
                    {
                        //var userPrompt = await ExecuteScriptAsync("getUserPrompt()");
                        //await MessageHandler.SendVsixMessageAsync(new VsixMessage { MessageType = "setUserPrompt", Content = userPrompt }, simpleClient);

                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        ShowQuickButtonOptionsWindow(message);

                    }
                    break;

                case "ready":
                    {
                        await MessageHandler.SendVsixMessageAsync(new VsixMessage { MessageType = "vsRequestButtons" }, simpleClient);
                        await AddContextMenuItemAsync("Insert Selection", "vsInsertSelection");
                        await AddContextMenuItemAsync("Pop Window", "vsPopWindow");
                        // look for a systemprompt.txt file in the root of the solution
                        await ExecuteScriptAsync("window.buttonControls['Set System Prompt from Solution'].show()");
                    }
                    break;

                case "vsInsertSelection":
                    {
                        var textDocument = _dte.ActiveDocument.Object("TextDocument") as TextDocument;
                        var activeDocumentFilename = _dte.ActiveDocument.Name;
                        var selectedText = _contentFormatter.GetCurrentSelection();
                        var formattedAsFile = _contentFormatter.FormatContent(activeDocumentFilename, selectedText);

                        var jsonSelectedText = JsonConvert.SerializeObject(formattedAsFile);
                        await ExecuteScriptAsync($"window.insertTextAtCaret({jsonSelectedText})");
                    }
                    break;

                case "vsPopWindow":
                    {
                        var solutionDetails = _shortcutManager.GetAllFilesInSolutionWithMembers();
                        var files = _shortcutManager.GetAllFilesInSolution();

                        files = files.Where(x => !x.Contains("\\.nuget\\")).ToList();

                        await simpleClient.SendLineAsync(JsonConvert.SerializeObject(new VsixMessage { MessageType = "vsShowFileSelector", Content = JsonConvert.SerializeObject(files) }));
                    }
                    break;

                case "vsQuickButton":
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        ShowQuickButtonOptionsWindow(message);
                    }
                    break;

                case "vsSelectFilesWithMembers":
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        ShowFileWithMembersSelectionWindow();
                    }
                    break;
            }

            if(message.type != "send")
                await MessageHandler.SendVsixMessageAsync(new VsixMessage { MessageType = "vsixui", Content = e.WebMessageAsJson }, simpleClient);
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
            var solutionDetails = _shortcutManager.GetAllFilesInSolutionWithMembers();
            var availableFiles = _shortcutManager.GetAllFilesInSolution().Where(x => !x.Contains("\\.nuget\\")).ToList();
            
            var editWindow = new FileGroupEditWindow(_fileGroupManager.GetAllFileGroups(), availableFiles);

            bool? result = editWindow.ShowDialog();

            if (result == true)
            {
                var editedFileGroups = editWindow.EditedFileGroups;

                _fileGroupManager.UpdateAllFileGroups(editedFileGroups);

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

            if(buttonLabel == "User Prompt"  || e.OriginalVsixMessage.type == "send" )
            {
                prompt = JsonConvert.DeserializeObject<string>(await ExecuteScriptAsync("getUserPrompt()"));
            }
            else 
            prompt = matchingButton?.Prompt;

            var inclusions = new List<string>();
            var activeDocumentFilename = _dte?.ActiveDocument?.Name;
             
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

            await MessageHandler.SendVsixMessageAsync(new VsixMessage { MessageType = "vsQuickButtonRun", Content = formattedAll }, simpleClient);
        }

        private string GetContentForOption(OptionWithParameter option, string activeDocumentFilename)
        {
            return _contentFormatter.GetContentForOption(option, activeDocumentFilename);
        }


    }
}