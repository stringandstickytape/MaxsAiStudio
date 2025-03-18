using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using SharedClasses;
using System;
using System.IO;
using System.Windows.Input;
using VSIXTest.FileGroups;
using SharedClasses.Models;
using VSIXTest.UI;

namespace VSIXTest
{
    public class VsixChat : WebView2
    {
        private readonly SimpleClient simpleClient = new SimpleClient();
        private readonly ContentFormatter _contentFormatter;
        private readonly VsixWebViewManager _webViewManager;
        public readonly ChangesetManager _changesetManager;
        private readonly QuickButtonManager _quickButtonManager;

        private bool vsixInitialised = false;
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
        private readonly ButtonManager _buttonManager = new ButtonManager();

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

        public static readonly bool NewUi = true;
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

            _changesetManager = new ChangesetManager(_dte, MessageHandler, simpleClient);

            _messageProcessor = new VsixMessageProcessor( 
                _dte,
                MessageHandler,
                simpleClient,
                _contentFormatter,
                _shortcutManager,
                this,
                _changesetManager);
            

            _quickButtonManager = new QuickButtonManager(
                _dte,
                MessageHandler,
                simpleClient,
                _contentFormatter,
                _fileGroupManager,
                _shortcutManager,
                ExecuteScriptAsync,
                VsixPackage,
                this);  // Pass the VsixPackage
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
                    var x = await ExecuteScriptAsync($"updateSystemPrompt({JsonConvert.SerializeObject(systemPrompt)})");
                    
                    //0 window.buttonControls['Set System Prompt from Solution'].show()
                }
            }
        }

        private async void SimpleClient_LineReceived(object sender, string e)
        {
            var vsixMessage = JsonConvert.DeserializeObject<VsixMessage>(e);
            await MessageHandler.HandleReceivedMessageAsync(vsixMessage);
        }





        private async void VsixChat_Loaded(object sender, RoutedEventArgs e)
        {
            if (!vsixInitialised)
            {
                if (!VsixChat.NewUi)
                {
                    await simpleClient.StartClientAsync();
                }
                await InitialiseAsync();
                vsixInitialised = true;
            }
        }

        public async Task InitialiseAsync()
        {
            await _webViewManager.InitializeAsync();
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

        internal void ShowQuickButtonOptionsWindow(VsixUiMessage message)
        {
            _quickButtonManager.ShowQuickButtonOptionsWindow(message);
        }

        private bool _changesetPaneInitted = false;

    }
}