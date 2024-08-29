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

        public async Task ReceiveMessage(VsixMessage message)
        {
            

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            switch (message.MessageType)
            {
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