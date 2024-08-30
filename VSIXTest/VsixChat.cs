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

namespace VSIXTest
{
    public class VsixChat : WebView2
    {
        private DTE2 _dte;
        private readonly ResourceManager _resourceManager;
        private readonly VsixMessageHandler _messageHandler;
        private readonly ShortcutManager _shortcutManager; 

        public VsixChat() : base()
        {
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            Loaded += VsixChat_Loaded;
            _resourceManager = new ResourceManager(Assembly.GetExecutingAssembly());
            _messageHandler = new VsixMessageHandler(_dte);
            _shortcutManager = new ShortcutManager(_dte);
        }

        private readonly ButtonManager _buttonManager = new ButtonManager();

        private async void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            string script = _buttonManager.GenerateButtonScript();
            await CoreWebView2.ExecuteScriptAsync(script);
        }

        private async void VsixChat_Loaded(object sender, RoutedEventArgs e)
        {
            await Initialise();
        }

        public async Task Initialise()
        {
            var env = await CoreWebView2Environment.CreateAsync(null, "C:\\temp");
            if (this.CoreWebView2 == null)
            {
                await EnsureCoreWebView2Async(env);
            }
            WebMessageReceived += WebView_WebMessageReceived;

            CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;

            foreach (var resource in _resourceManager.GetResourceDetails())
            {
                CoreWebView2.AddWebResourceRequestedFilter(resource.Uri, CoreWebView2WebResourceContext.All);
            }

            CoreWebView2.Navigate("http://localhost/Home.html");
        }



        private void CoreWebView2_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            if (_resourceManager.IsResourceRequested(e.Request.Uri))
            {
                var resourceDetail = _resourceManager.GetResourceDetailByUri(e.Request.Uri);
                _resourceManager.ReturnResourceToWebView(e, resourceDetail.ResourceName, resourceDetail.MimeType, CoreWebView2);
            }
        }



        private void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var message = JsonConvert.DeserializeObject<dynamic>(e.WebMessageAsJson);
            string messageType = (string)message.type;

            switch (messageType)
            {

                case "sendMessage":
                    _messageHandler.SendPrompt((string)message.message);
                    break;
                case "getShortcuts":
                    GetShortcuts((string)message.token);
                    break;
                case "newChat":
                    _messageHandler.SendNewConversationMessage();
                    break;
                default:
                    _messageHandler.HandleDefaultMessage(messageType);
                    break;
            }
        }

        private async Task GetShortcuts(string token)
        {
            var shortcuts = _shortcutManager.GetShortcuts(token);
            string shortcutsJson = JsonConvert.SerializeObject(shortcuts);
            string script = $"showShortcuts({shortcutsJson});";

            await ExecuteScriptAsync(script);
        }



        private async Task ShowCompletionAsync(string completionText)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var package = VSIXTestPackage.Instance;
            var dte = package.DTE;

            if (dte?.ActiveDocument?.Object("TextDocument") is TextDocument textDocument)
            {
                var selection = textDocument.Selection as TextSelection;
                if (selection != null)
                {
                    // Store the starting point
                    var startPoint = selection.ActivePoint.CreateEditPoint();

                    // Insert a carriage return and then the completion text
                    selection.Insert(Environment.NewLine + completionText);

                    // Move the cursor to the start of the inserted text (after the carriage return)
                    var afterCarriageReturn = startPoint.CreateEditPoint();
                    afterCarriageReturn.LineDown(1);
                    afterCarriageReturn.StartOfLine();

                    // Calculate the end point based on the length of the inserted text
                    var endPoint = afterCarriageReturn.CreateEditPoint();
                    endPoint.CharRight(completionText.Length);

                    try
                    {
                        // Attempt to format the inserted text
                        selection.MoveToPoint(afterCarriageReturn);
                        selection.MoveToPoint(endPoint, true);
                        dte.ExecuteCommand("Edit.FormatSelection");
                    }
                    catch (Exception ex)
                    {
                        // If formatting fails, just continue without formatting
                        System.Diagnostics.Debug.WriteLine($"Formatting failed: {ex.Message}");
                    }

                    // Ensure the inserted text is selected after formatting
                    selection.MoveToPoint(afterCarriageReturn);
                    selection.MoveToPoint(endPoint, true);
                }
            }
        }


        private async Task HandleAutocompleteResponse(string content)
        {
            if(content.StartsWith("{\"code="))
            {
                var firstIndex = content.IndexOf("{\"code=");
                if(firstIndex > -1)
                    content = content.Substring(0, firstIndex) + "{\"Code" + content.Substring(firstIndex + 7);
                firstIndex = content.IndexOf("{\"Code=");
                if (firstIndex > -1)
                    content = content.Substring(0, firstIndex) + "{\"Code" + content.Substring(firstIndex + 7);
            }
            var response = JsonConvert.DeserializeObject<AutocompleteResponse>(content);
            if (response != null && !string.IsNullOrEmpty(response.Code))
            {
                await ShowCompletionAsync(response.Code);
            }
        }

        private class AutocompleteResponse
        {
            public string Code { get; set; }
            public string Explanation { get; set; }
        }


        public async Task ReceiveMessage(VsixMessage message)
        {
            

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            switch (message.MessageType)
            {
                case "autocompleteResponse":
                    await HandleAutocompleteResponse(message.Content);
                    break;
                case "response":
                    string escapedMessage = HttpUtility.JavaScriptStringEncode(message.Content);
                    await ExecuteScriptAsync($"chatHistory.innerHTML = '{escapedMessage}';document.querySelector('#ChatHistory').scrollTop = document.querySelector('#ChatHistory').scrollHeight;");

                    await ExecuteScriptAsync(@"
                        document.addEventListener('click', function(e) {
                            if (e.target && e.target.textContent === 'Copy' && e.target.closest('.message-content')) {
                                const codeBlock = e.target.closest('.message-content').querySelector('div[style*=""font-family: monospace""]');
                                if (codeBlock) {
                                    const codeText = codeBlock.textContent;
                                    navigator.clipboard.writeText(codeText);
                                }
                            }
                        });
                    ");

                    break;
            }
        }
    }

}