using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using SharedClasses;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System;
using SharedClasses.Helpers;
using System.Linq;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.Threading;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Web.UI.WebControls;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Diagnostics;


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

        private DTE2 _dte;
        private readonly ResourceManager _resourceManager;
        private readonly VsixMessageHandler _messageHandler;
        private readonly ShortcutManager _shortcutManager;
        private readonly AutocompleteManager _autocompleteManager;

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
            _messageHandler = new VsixMessageHandler(_dte, ExecuteScriptAsync);
            _shortcutManager = new ShortcutManager(_dte);
            _autocompleteManager = new AutocompleteManager(_dte);

            simpleClient.LineReceived += SimpleClient_LineReceived;
            simpleClient.StartClient();
        }

        private async void SimpleClient_LineReceived(object sender, string e)
        {
            var vsixMessage = JsonConvert.DeserializeObject<VsixMessage>(e);
            await _messageHandler.HandleReceivedMessage(vsixMessage);
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

        private async void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var message = JsonConvert.DeserializeObject<VsixUiMessage>(e.WebMessageAsJson);

            if (message.type == "send")
            {
                // when the user clicks send in the VSIX, we need to copy their prompt into the user prompt in the app, from where the send will pick it up...
                var userPrompt = await ExecuteScriptAsync("getUserPrompt()");
                await _messageHandler.SendVsixMessage(new VsixMessage { MessageType = "setUserPrompt", Content = userPrompt }, simpleClient);
            }

            if (message.type == "ready")
            {
                // any vsix-specific webview setup can go here
                await ExecuteScriptAsync(@"window.addCustomContextMenuItem({
    label:'Insert Selection',
    onClick: () =>    window.chrome.webview.postMessage({
                                type: 'vsInsertSelection'
                            })
});");

                await ExecuteScriptAsync(@"window.addCustomContextMenuItem({
    label:'Pop Window',
    onClick: () =>    window.chrome.webview.postMessage({
                                type: 'vsPopWindow'
                            })
});");
            }

            if (message.type == "vsInsertSelection")
            {
                var textDocument = _dte.ActiveDocument.Object("TextDocument") as TextDocument;
                var selection = textDocument.Selection as TextSelection;
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

                simpleClient.SendLine(JsonConvert.SerializeObject(new VsixMessage { MessageType = "vsShowFileSelector", Content = JsonConvert.SerializeObject(files) }));

               // var window = new TreeViewWindow();
               // window.OnClose += (sender2, args) =>
               // {
               //     List<string> checkedItems = window.GetCheckedItems();
               //     
               //     Debug.WriteLine("TreeViewWindow is closing. Selected items:");
               //     foreach (string item in checkedItems)
               //     {
               //         Debug.WriteLine($"- {item}");
               //     }
               //
               //     // Perform any cleanup or save operations here
               //     Console.WriteLine("TreeViewWindow is closing!");
               // };
               //
               //
               //
               // List<TreeViewItem> treeViewItems = ConvertPathsToTreeViewItems(files);
               //
               // // Populate the TreeView
               // window.PopulateTreeView(treeViewItems);
               //
               // // Show the window
               // window.Show();
            }

            await _messageHandler.SendVsixMessage(new VsixMessage { MessageType = "vsixui", Content = e.WebMessageAsJson }, simpleClient);
        }

        public static List<TreeViewItem> ConvertPathsToTreeViewItems(List<string> files)
        {
            var rootItems = new List<TreeViewItem>();
            var itemsDictionary = new Dictionary<string, TreeViewItem>();
            string commonRoot = FindCommonRoot(files);

            foreach (var file in files)
            {
                var parts = file.Substring(commonRoot.Length).Split(Path.DirectorySeparatorChar);
                var currentPath = commonRoot;

                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    currentPath = Path.Combine(currentPath, part);

                    if (!itemsDictionary.TryGetValue(currentPath, out var item))
                    {
                        item = new TreeViewItem { Header = part, Tag = currentPath };
                        itemsDictionary[currentPath] = item;

                        if (i == 0 && rootItems.Count == 0)
                        {
                            rootItems.Add(item);
                        }
                        else
                        {
                            var parentPath = Path.GetDirectoryName(currentPath);
                            if (itemsDictionary.TryGetValue(parentPath, out var parentItem))
                            {
                                parentItem.Items.Add(item);
                            }
                            else
                            {
                                rootItems.Add(item);
                            }
                        }
                    }

                    if (i == parts.Length - 1)
                    {
                        // This is a file, so we can set an icon or change its style here if needed
                        // For example: item.Icon = new BitmapImage(new Uri("path_to_file_icon.png"));
                    }
                }
            }

            return rootItems;
        }

        private static string FindCommonRoot(List<string> paths)
        {
            if (paths == null || paths.Count == 0)
                return string.Empty;

            var firstPath = paths[0];
            var commonRoot = firstPath;

            for (int i = 1; i < paths.Count; i++)
            {
                var path = paths[i];
                int j;
                for (j = 0; j < commonRoot.Length && j < path.Length; j++)
                {
                    if (char.ToLower(commonRoot[j]) != char.ToLower(path[j]))
                        break;
                }
                commonRoot = commonRoot.Substring(0, j);
            }

            // Ensure the common root ends at a directory separator
            int lastSeparatorIndex = commonRoot.LastIndexOf(Path.DirectorySeparatorChar);
            if (lastSeparatorIndex >= 0)
                commonRoot = commonRoot.Substring(0, lastSeparatorIndex + 1);
            else
                commonRoot = string.Empty;

            return commonRoot;
        }
    }

}