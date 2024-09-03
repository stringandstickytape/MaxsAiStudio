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

namespace VSIXTest
{
    public class VsixChat : WebView2
    {
        private bool isClientInitialized = false;
        private SemaphoreSlim clientInitSemaphore = new SemaphoreSlim(1, 1);

        private SimpleClient simpleClient = new SimpleClient();

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

        private DTE2 _dte;
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
                await ExecuteScriptAsync($"window.moveCaretToEnd({(shiftHeld ? "true" : "false")})");
            }
            else if (e.Key == Key.Home)
            {
                e.Handled = true;
                bool shiftHeld = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                await ExecuteScriptAsync($"window.moveCaretToStart({(shiftHeld ? "true" : "false")})");
            }
        }

        private async Task SendEndKeyPress()
        {
            // Ensure the WebView2 is fully initialized

        }

        private string ConvertKeyToJsKey(Key key)
        {
            switch (key)
            {
                case Key.End:
                    return "End";
                // Add more cases as needed for other keys
                default:
                    return key.ToString();
            }
        }


        public VsixChat() : base()
        {
            this.KeyDown += VsixChat_KeyDown;
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            Loaded += VsixChat_Loaded;
            _resourceManager = new ResourceManager(Assembly.GetExecutingAssembly());
            MessageHandler = new VsixMessageHandler(_dte, ExecuteScriptAsync);
            _shortcutManager = new ShortcutManager(_dte);
            _autocompleteManager = new AutocompleteManager(_dte);

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string extensionDataPath = Path.Combine(appDataPath, "MaxsAiStudio", "Vsix");
            Directory.CreateDirectory(extensionDataPath); // Ensure the directory exists

            _fileGroupManager = new FileGroupManager(extensionDataPath);

            simpleClient.LineReceived += SimpleClient_LineReceived;
            simpleClient.StartClient();
        }

        private List<string> GetAvailableFiles()
        {
            // Implement this method to return a list of all available files
            // This could be all files in the current solution, project, or any other source
            // For example:
            return new List<string>
            {
                "File1.cs",
                "File2.cs",
                "File3.cs",
                // ... add more files as needed
            };
        }


        private async void SimpleClient_LineReceived(object sender, string e)
        {
            var vsixMessage = JsonConvert.DeserializeObject<VsixMessage>(e);
            await MessageHandler.HandleReceivedMessage(vsixMessage);
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
                await InitialiseAsync(); 
                vsixInitialised = true;
                _fileGroupManager.CreateFileGroup("TestGroup"+DateTime.Now.ToShortTimeString(), new List<string> { "file1.txt", "file2.txt" });



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

        private async Task AddContextMenuItem(string label, string messageType)
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
            var message = JsonConvert.DeserializeObject<VsixUiMessage>(e.WebMessageAsJson);

            if (message.type == "send")
            {
                // when the user clicks send in the VSIX, we need to copy their prompt into the user prompt in the app, from where the send will pick it up...
                var userPrompt = await ExecuteScriptAsync("getUserPrompt()");
                await MessageHandler.SendVsixMessage(new VsixMessage { MessageType = "setUserPrompt", Content = userPrompt }, simpleClient);
            }

            if (message.type == "ready")
            {
                await MessageHandler.SendVsixMessage(new VsixMessage { MessageType = "vsRequestButtons" }, simpleClient);

                await AddContextMenuItem("Insert Selection", "vsInsertSelection");
                await AddContextMenuItem("Pop Window", "vsPopWindow");

            }

            if (message.type == "vsInsertSelection")
            {
                var textDocument = _dte.ActiveDocument.Object("TextDocument") as TextDocument;
                var selection = textDocument.Selection as EnvDTE.TextSelection;
                var activeDocumentFilename = _dte.ActiveDocument.Name;

                var selectedText = selection.Text;
                var formattedAsFile = $"\n{MessageFormatter.FormatFile(activeDocumentFilename, selectedText)}";

                var jsonSelectedText = JsonConvert.SerializeObject(formattedAsFile);
                await ExecuteScriptAsync($"window.insertTextAtCaret({jsonSelectedText})");
                //await _messageHandler.SendVsixMessage(new VsixMessage { MessageType = "vsInsertSelection", Content = selectedText }, simpleClient);
            }
            if (message.type == "vsPopWindow")
            {

                var files = _shortcutManager.GetAllFilesInSolution();

                files = files.Where(x => !x.Contains("\\.nuget\\")).ToList();

                await simpleClient.SendLine(JsonConvert.SerializeObject(new VsixMessage { MessageType = "vsShowFileSelector", Content = JsonConvert.SerializeObject(files) }));

            }
            if(message.type == "vsQuickButton")
            {

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                ShowQuickButtonOptionsWindow(message);
                //var matchingButton = _messageHandler.Buttons.FirstOrDefault(x => x.ButtonLabel == message.content);
                //var prompt = matchingButton?.Prompt;
                //
                ////get currently selected text in active document
                //var textDocument = _dte.ActiveDocument.Object("TextDocument") as TextDocument;
                //var selection = textDocument.Selection as TextSelection;
                //var activeDocumentFilename = _dte.ActiveDocument.Name;
                //var selectedText = selection.Text;
                //
                //if (message.content == "Commit Message")
                //{
                //    var diff = new GitDiffHelper().GetGitDiff();
                //    var formatteddiff = $"\n{MessageFormatter.FormatFile("diff", diff)}\n\n{prompt}";
                //    var jsonFormatteddiff = JsonConvert.SerializeObject(formatteddiff);
                //    await ExecuteScriptAsync($"setUserPrompt({jsonFormatteddiff})");
                //    await _messageHandler.SendVsixMessage(new VsixMessage { MessageType = "vsQuickButtonRun", Content = formatteddiff }, simpleClient);
                //}
                //else
                //{
                //    var formatted = $"\n{MessageFormatter.FormatFile(activeDocumentFilename, selectedText)}\n\n{prompt}";
                //    var jsonFormatted = JsonConvert.SerializeObject(formatted);
                //    await ExecuteScriptAsync($"setUserPrompt({jsonFormatted})");
                //    await _messageHandler.SendVsixMessage(new VsixMessage { MessageType = "vsQuickButtonRun", Content = formatted }, simpleClient);
                //}


            }



            await MessageHandler.SendVsixMessage(new VsixMessage { MessageType = "vsixui", Content = e.WebMessageAsJson }, simpleClient);
        }

        /// <summary>
        /// Displays the Quick Button Options window and sets up the message and event handling.
        /// </summary>
        /// <param name="message">The message to be displayed in the Quick Button Options window.</param>
        /// <exception cref="NotSupportedException">Thrown when the tool window cannot be created.</exception>
        /// <remarks>
        /// This method performs the following actions:
        /// 1. Finds or creates the QuickButtonOptionsWindow.
        /// 2. Sets the message in the window.
        /// 3. Subscribes to the OptionsSelected event.
        /// 4. Shows the window.
        /// </remarks>
        /// <seealso cref="QuickButtonOptionsWindow"/>
        /// <seealso cref="VsixUiMessage"/>
        public void ShowQuickButtonOptionsWindow(VsixUiMessage message)
        {
            ToolWindowPane window = VsixPackage.FindToolWindow(typeof(QuickButtonOptionsWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            var quickButtonOptionsWindow = window as QuickButtonOptionsWindow;
            quickButtonOptionsWindow.SetMessage(message);

            quickButtonOptionsWindow.OptionsControl.OptionsSelected += OptionsControl_OptionsSelected;
            quickButtonOptionsWindow.OptionsControl.FileGroupsEditorInvoked += OptionsControl_FileGroupsEditorInvoked;

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        private void OptionsControl_FileGroupsEditorInvoked(object sender, string e)
        {
            // bodged for now
            var availableFiles = _shortcutManager.GetAllFilesInSolution().Where(x => !x.Contains("\\.nuget\\")).ToList();
            
            var editWindow = new FileGroupEditWindow(_fileGroupManager.GetAllFileGroups(), availableFiles);

            bool? result = editWindow.ShowDialog();

            if (result == true)
            {
                var editedFileGroups = editWindow.EditedFileGroups;

                // User clicked Save, update the file group
                //FileGroup editedFileGroup = editWindow.EditedFileGroup;
                //
                //// Update the file group in your FileGroupManager
                //_fileGroupManager.UpdateFileGroup(
                //    editedFileGroup.Id,
                //    editedFileGroup.Name,
                //    editedFileGroup.FilePaths
                //);

            }
            else
            {
                // User clicked Cancel or closed the window, do nothing or handle as needed
            }
        }

        private async void OptionsControl_OptionsSelected(object sender, QuickButtonMessageAndOptions e)
        {
            // need to call to messagehandler here...
            var buttonLabel = e.OriginalVsixMessage.content;
            var matchingButton = MessageHandler.Buttons.FirstOrDefault(x => x.ButtonLabel == buttonLabel);
            var prompt = matchingButton?.Prompt;

            List<string> inclusions = new List<string>();

            var activeDocumentFilename = _dte?.ActiveDocument?.Name;

            foreach (var option in e.SelectedOptions)
            {
                switch (option.Option)
                {
                    case "CurrentSelection":
                        var textDocument = _dte.ActiveDocument.Object("TextDocument") as TextDocument;
                        var selection = textDocument.Selection as EnvDTE.TextSelection;
                        var selectedText = selection.Text;
                        var formatted = $"\n{MessageFormatter.FormatFile(activeDocumentFilename, selectedText)}\n";
                        inclusions.Add(formatted);
                        break;
                    case "Clipboard":
                        var clipboardText = Clipboard.GetText();
                        formatted = $"\n{MessageFormatter.FormatFile(activeDocumentFilename, clipboardText)}\n";
                        inclusions.Add(formatted);
                        break;
                    case "CurrentFile":
                        textDocument = _dte.ActiveDocument.Object("TextDocument") as TextDocument;
                        selection = textDocument.Selection as EnvDTE.TextSelection;
                        selectedText = selection.Text;
                        formatted = $"\n{MessageFormatter.FormatFile(activeDocumentFilename, selectedText)}\n";
                        inclusions.Add(formatted);
                        break;
                    case "GitDiff":
                        var diff = new GitDiffHelper().GetGitDiff();
                        formatted = $"\n{MessageFormatter.FormatFile("diff", diff)}\n";
                        inclusions.Add(formatted);
                        break;
                    case "XmlDoc":
                        var matchingMethods = new MethodFinder().FindMethods(option.Parameter);
                        var formattedMethods = matchingMethods.Select(x => MessageFormatter.FormatFile(x.FileName, x.SourceCode)).ToList();
                        inclusions.AddRange(formattedMethods);
                        break;
                    case "FileGroups":


                        break;
                }
            }

            var formattedAll = $"\n{string.Join("\n\n", inclusions)}\n\n{prompt}";
            var jsonFormattedAll = JsonConvert.SerializeObject(formattedAll);

            await ExecuteScriptAsync($"setUserPrompt({jsonFormattedAll})");
            await MessageHandler.SendVsixMessage(new VsixMessage { MessageType = "vsQuickButtonRun", Content = formattedAll }, simpleClient);

            System.Diagnostics.Debug.WriteLine("Selected Options:");
            foreach (var option in e.SelectedOptions)
            {
                System.Diagnostics.Debug.WriteLine(option);
            }


        }
    }


}