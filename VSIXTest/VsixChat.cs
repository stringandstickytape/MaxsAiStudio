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

namespace VSIXTest
{
    public class VsixChat : WebView2
    {
        private DTE2 _dte;
        private readonly ResourceManager _resourceManager;
        private readonly VsixMessageHandler _messageHandler;
        private readonly ShortcutManager _shortcutManager;
        private readonly AutocompleteManager _autocompleteManager;

        public VsixChat() : base()
        {
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            Loaded += VsixChat_Loaded;
            _resourceManager = new ResourceManager(Assembly.GetExecutingAssembly());
            _messageHandler = new VsixMessageHandler(_dte);
            _shortcutManager = new ShortcutManager(_dte);
            _autocompleteManager = new AutocompleteManager(_dte);
        }

        private readonly ButtonManager _buttonManager = new ButtonManager();

        private async void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            string script = _buttonManager.GenerateButtonScript();
            await CoreWebView2.ExecuteScriptAsync(script);
        }

        private async void VsixChat_Loaded(object sender, RoutedEventArgs e)
        {
            await InitialiseAsync();
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
            // vsixui click handler
            // deser to VsixUiMessage
            var message = JsonConvert.DeserializeObject<VsixUiMessage>(e.WebMessageAsJson);

            if(message.type == "send")
            {
                // get the current user prompt
                var userPrompt = await ExecuteScriptAsync("getUserPrompt()");
                _messageHandler.SendVsixMessage(new VsixMessage { MessageType = "setUserPrompt", Content = userPrompt });
            }
            _messageHandler.SendVsixMessage(new VsixMessage { MessageType = "vsixui", Content = e.WebMessageAsJson });
            //var message = JsonConvert.DeserializeObject<dynamic>(e.WebMessageAsJson);
            //string messageType = (string)message.type;
            //
            //switch (messageType)
            //{
            //
            //    case "sendMessage":
            //        _messageHandler.SendPrompt((string)message.message);
            //        break;
            //    case "getShortcuts":
            //        await ShowShortcuts((string)message.token);
            //        break;
            //    case "newChat":
            //        _messageHandler.SendNewConversationMessage();
            //        break;
            //    default:
            //        _messageHandler.HandleDefaultMessage(messageType);
            //        break;
            //}
        }

        private async Task ShowShortcuts(string token)
        {
            var shortcuts = _shortcutManager.GetShortcuts(token);
            string shortcutsJson = JsonConvert.SerializeObject(shortcuts);
            string script = $"showShortcuts({shortcutsJson});";

            await ExecuteScriptAsync(script);
        }


        public async Task ReceiveMessage(VsixMessage message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            switch (message.MessageType)
            {
                case "webviewJsCall":
                    ExecuteScriptAsync(message.Content);
                    break;
                case "autocompleteResponse":
                    await _autocompleteManager.HandleAutocompleteResponse(message.Content);
                    break;
                case "response":
                    //           string escapedMessage = HttpUtility.JavaScriptStringEncode(message.Content);
                    //           await ExecuteScriptAsync($"chatHistory.innerHTML = '{escapedMessage}';document.querySelector('#ChatHistory').scrollTop = document.querySelector('#ChatHistory').scrollHeight;");
                    //
                    //           await ExecuteScriptAsync(@"
                    //       document.addEventListener('click', function(e) {
                    //           if (e.target && e.target.textContent === 'Copy' && e.target.closest('.message-content')) {
                    //               const codeBlock = e.target.closest('.message-content').querySelector('div[style*=""font-family: monospace""]');
                    //               if (codeBlock) {
                    //                   const codeText = codeBlock.textContent;
                    //                   navigator.clipboard.writeText(codeText);
                    //               }
                    //           }
                    //       });
                    //   ");

                    break;
            }
        }
    }

    public class VsixUiMessage
    {
        public string type { get; set; }
        public string content { get; set; }
        public string selectedTools { get; set; }
        public string addEmbeddings { get; set; }
    }

}