using EnvDTE;
using EnvDTE80;
using Microsoft.ServiceHub.Resources;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public VsixChat() : base()
        {
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            Loaded +=  VsixChat_Loaded;
            //this.HandleCreated += OnHandleCreated;

            //EnsureCoreWebView2Async(null)
            //AllowExternalDrop = false;

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
        
            foreach (var resource in GetResourceDetails())
            {
                CoreWebView2.AddWebResourceRequestedFilter(resource.Uri, CoreWebView2WebResourceContext.All);
            }

            //NavigateToString(VsixAssemblyHelper.GetEmbeddedAssembly("VSIXTest.Html.Home.html"));
            CoreWebView2.Navigate("http://localhost/Home.html");
        

            // preload JS
            //string[] scriptResources = new[]
            //        {
            //    "AiTool3.JavaScript.JsonViewer.js",
            //    "AiTool3.JavaScript.ThemeEditor.js",
            //    "AiTool3.JavaScript.SvgViewer.js",
            //    "AiTool3.JavaScript.MermaidViewer.js",
            //    "AiTool3.JavaScript.DotViewer.js",
            //    "AiTool3.JavaScript.FindAndReplacer.js"
            //};
            //
            //foreach (var resource in scriptResources)
            //{
            //    await ExecuteScriptAsync(VsixAssemblyHelper.GetEmbeddedAssembly(resource));
            //}
        }



        private void CoreWebView2_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            var rd = GetResourceDetails();
            var matching = rd.Where(x => e.Request.Uri == x.Uri).ToList();


            GetResourceDetails().Where(x => e.Request.Uri.Equals(x.Uri, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => ReturnResourceToWebView(e, x.ResourceName, x.MimeType));
        }

        private static List<VsixResourceDetails> GetResourceDetails()
        {

            // create a new resourcedetail for each resource in namespace AiTool3.JavaScript.Components
            var resources = new List<VsixResourceDetails>();
            foreach (var resourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                if (resourceName.StartsWith("VSIXTest.Html"))
                {
                    // find the index of the penultimate dot in resource name
                    var penultimateDotIndex = resourceName.LastIndexOf(".", resourceName.LastIndexOf(".") - 1);
                    // get the filename using that
                    var filename = resourceName.Substring(penultimateDotIndex + 1);

                    resources.Add(new VsixResourceDetails
                    {
                        Uri = $"http://localhost/{filename}",
                        ResourceName = resourceName,
                        MimeType = "text/html"
                    });
                }
            }

            resources.AddRange(CreateResourceDetailsList());

            return resources;
        }

        private static List<VsixResourceDetails> CreateResourceDetailsList()
        {
            return new List<(string Uri, string ResourceName, string MimeType)>
            {
                ("https://cdn.jsdelivr.net/npm/mermaid@10.2.3/dist/mermaid.min.js", "mermaid.min.js", "application/javascript"),
                ("https://cdn.jsdelivr.net/npm/svg-pan-zoom@3.6.1/dist/svg-pan-zoom.min.js", "svg-pan-zoom.min.js", "application/javascript"),
                ("https://cdnjs.cloudflare.com/ajax/libs/jsoneditor/9.9.2/jsoneditor.min.js", "jsoneditor.min.js", "application/javascript"),
                ("https://cdnjs.cloudflare.com/ajax/libs/jsoneditor/9.9.2/jsoneditor.min.css", "jsoneditor.min.css", "text/css"),
                ("https://cdnjs.cloudflare.com/ajax/libs/jsoneditor/9.9.2/jsoneditor-icons.svg", "jsoneditor-icons.svg", "image/svg+xml"),
                ("https://cdnjs.cloudflare.com/ajax/libs/cytoscape/3.21.1/cytoscape.min.js", "cytoscape.min.js", "application/javascript"),
                ("https://cdnjs.cloudflare.com/ajax/libs/dagre/0.8.5/dagre.min.js", "dagre.min.js", "application/javascript"),
                ("https://unpkg.com/viz.js@2.1.2/viz.js", "viz.js", "application/javascript"),
                ("https://cdn.jsdelivr.net/npm/cytoscape-cxtmenu@3.4.0/cytoscape-cxtmenu.min.js", "cytoscape-cxtmenu.min.js", "application/javascript"),
                ("https://cdn.jsdelivr.net/npm/cytoscape-dagre@2.3.2/cytoscape-dagre.min.js", "cytoscape-dagre.min.js", "application/javascript")
            }.Select(item => new VsixResourceDetails
            {
                Uri = item.Uri,
                ResourceName = $"AiTool3.ThirdPartyJavascript.{item.ResourceName}",
                MimeType = item.MimeType
            }).ToList();
        }

        private void ReturnResourceToWebView(CoreWebView2WebResourceRequestedEventArgs e, string resourceName, string mimeType)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        string content = reader.ReadToEnd();
                        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
                        var response = CoreWebView2.Environment.CreateWebResourceResponse(memoryStream, 200, "OK", $"Content-Type: {mimeType}");
                        e.Response = response;
                        e.Response.Headers.AppendHeader("Access-Control-Allow-Origin", "*");
                        return;
                    }
                }
                throw new Exception("Probably forgot to embed the resource :(");
            }
        }

        private static readonly MessagePrompt[] MessagePrompts = new[]
        {
            new MessagePrompt { ButtonLabel = "Commit Message", MessageType = "commitMsg", Prompt = "Give me a short, high-quality, bulleted, tersely-phrased summary for this diff, broken down by [CATEGORY]. Do not mention unused categories:" },
            new MessagePrompt { ButtonLabel = "Extract Method", MessageType = "extractMethod", Prompt = "Perform an extract method on this:" },
            new MessagePrompt { ButtonLabel = "Extract Static Method", MessageType = "extractStaticMethod", Prompt = "Perform an extract static method on this:" },
            new MessagePrompt { ButtonLabel = "DRY This", MessageType = "dryThis", Prompt = "Suggest some clever ways, with examples, to DRY this code:" },
            new MessagePrompt { ButtonLabel = "Autocomplete This", MessageType = "autocompleteThis", Prompt = "Autocomplete this code where you see the marker //! . Give only the inserted text and no other output, demarcated with three ticks before and after." },
            new MessagePrompt { ButtonLabel = "Suggest Name", MessageType = "suggestName", Prompt = "Suggest a concise and descriptive name for this code element:" },
            new MessagePrompt { ButtonLabel = "General Refactor", MessageType = "generalRefactor", Prompt = "Suggest some clever ways, with examples, to generally refactor this code:" },
            new MessagePrompt { ButtonLabel = "Improve Performance", MessageType = "improvePerformance", Prompt = "Analyse and, if possible, suggest some clever ways with examples, to improve the performance of this code:" },
            new MessagePrompt { ButtonLabel = "Extend Series", MessageType = "addToSeries", Prompt = "Extend the series you see in this code:" },
            new MessagePrompt { ButtonLabel = "Explain Code", MessageType = "explainCode", Prompt = "Provide a detailed explanation of what this code does:" },
            new MessagePrompt { ButtonLabel = "Add Comments", MessageType = "addComments", Prompt = "Add appropriate comments to this code to improve its readability:" },
            new MessagePrompt { ButtonLabel = "Remove Comments", MessageType = "addComments", Prompt = "Remove all comments from this code:" },
            new MessagePrompt { ButtonLabel = "Convert to LINQ", MessageType = "convertToLinq", Prompt = "Convert this code to use LINQ expressions where appropriate:" },
            new MessagePrompt { ButtonLabel = "Add Error Handling", MessageType = "addErrorHandling", Prompt = "Suggest appropriate error handling mechanisms for this code:" },
            new MessagePrompt { ButtonLabel = "Create Unit Tests", MessageType = "createUnitTests", Prompt = "Generate unit tests for this code:" },
            new MessagePrompt { ButtonLabel = "Identify Potential Bugs", MessageType = "identifyBugs", Prompt = "Analyze this code for potential bugs or edge cases that might cause issues:" },
            new MessagePrompt { ButtonLabel = "Add Logging", MessageType = "addLogging", Prompt = "Suggest appropriate logging statements to add to this code for better debugging and monitoring:" },
            new MessagePrompt { ButtonLabel = "Simplify Logic", MessageType = "simplifyLogic", Prompt = "Analyze and suggest ways to simplify the logic in this code without changing its functionality:" },
        };

        private async void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            string script = GenerateButtonScript();
            await CoreWebView2.ExecuteScriptAsync(script);
        }

        private static string GenerateButtonScript()
        {
            var scriptBuilder = new StringBuilder();
            scriptBuilder.Append(@"
    // Get the button container
    var buttonContainer = document.getElementById('ButtonContainer');

    // Get the 'New' button
    var newButton = buttonContainer.querySelector('button[onclick=""performAction(\\""newChat\\"")""]');

    // Function to create a button
    function createButton(label, messageType) {
        var button = document.createElement('button');
        button.textContent = label;
        button.onclick = function() {
            performAction(messageType);
        };
        return button;
    }

    // Create and insert buttons
    ");

            foreach (var prompt in MessagePrompts)
            {
                scriptBuilder.Append($@"
    var {prompt.MessageType}Button = createButton('{prompt.ButtonLabel}', '{prompt.MessageType}');
    buttonContainer.insertBefore({prompt.MessageType}Button, newButton);
    ");
            }

            return scriptBuilder.ToString();
        }

        private void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var message = JsonConvert.DeserializeObject<dynamic>(e.WebMessageAsJson);
            string messageType = (string)message.type;

            switch (messageType)
            {
                case "sendMessage":
                    SendMessage((string)message.message);
                    break;
                case "getShortcuts":
                    GetShortcuts((string)message.token);
                    break;
                case "newChat":
                    SendNewConversationMessage();
                    break;
                default:
                    var insertionType = messageType == "commitMsg" ? "#:diff:" : "#:selection:";

                    var matchingPrompt = MessagePrompts.FirstOrDefault(mp => mp.MessageType == messageType);
                    if (matchingPrompt != null)
                    {
                        string prompt = $"{Environment.NewLine}{Environment.NewLine}{insertionType}{Environment.NewLine}{matchingPrompt.Prompt}";
                        BeginConversationWithPrompt(prompt);
                    }
                    break;
            }
        }

        private void BeginConversationWithPrompt(string userPrompt)
        {
            SendNewConversationMessage();
            SendMessage(userPrompt);
        }

        private static void SendNewConversationMessage()
        {
            VSIXTestPackage.Instance.SendMessageThroughPipe(JsonConvert.SerializeObject(new VsixOutgoingMessage { MessageType = "new" }));
        }

        private string ReplaceFileNameWithContent(string message, string file)
        {
            string fileName = $"#{Path.GetFileName(file)}";
            if (message.Contains(fileName))
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                ProjectItem projectItem = _dte.Solution.FindProjectItem(file);
                if (projectItem != null)
                {
                    EnvDTE.Window window = projectItem.Open();
                    if (window != null)
                    {
                        try
                        {
                            message = ReplaceFileNameWithContentHelper(message, fileName, window);
                        }
                        finally
                        {
                            window.Close();
                        }
                    }
                }
            }
            return message;
        }

        private static string ReplaceFileNameWithContentHelper(string message, string fileName, EnvDTE.Window window)
        {
            TextDocument textDoc = window.Document.Object("TextDocument") as TextDocument;
            if (textDoc != null)
            {
                string fileContent = textDoc.StartPoint.CreateEditPoint().GetText(textDoc.EndPoint);
                string backticks = new string('`', 3);
                string replacement = $"\n{backticks}\n{fileContent}\n{backticks}\n";
                return message.Replace(fileName, replacement);
            }
            return message;
        }


        private void SendMessage(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                // Handle filename hashtags and other processing
                var files = GetAllFilesInSolution();
                foreach (var file in files)
                {
                    message = ReplaceFileNameWithContent(message, file);
                }

                // replace any '#:selection:' with the selected text
                if (message.Contains("#:selection:"))
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    var selection = (TextSelection)_dte.ActiveDocument.Selection;
                    var documentFilename = _dte.ActiveDocument.Name;
                    message = MessageFormatter.InsertFilenamedSelection(message, documentFilename, selection);
                }

                if (message.Contains("#:diff:"))
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    var gitDiffHelper = new GitDiffHelper();
                    var diff = gitDiffHelper.GetGitDiff();
                    message = message.Replace("#:diff:", diff);
                }
                
                var vsixOutgoingMessage = new VsixOutgoingMessage { Content = message, MessageType = "prompt" };
                string jsonMessage = JsonConvert.SerializeObject(vsixOutgoingMessage);

                VSIXTestPackage.Instance.SendMessageThroughPipe(jsonMessage); // messagetype is p (for prompt)
            }
        }

        private async void GetShortcuts(string token)
        {
            var shortcuts = new List<string> { "#:selection:", "#:diff:" };
            var files = GetAllFilesInSolution();

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file).ToLower();
                if (fileName.Contains(token.ToLower()))
                {
                    shortcuts.Add($"#{Path.GetFileName(file)}");
                }
            }

            string shortcutsJson = JsonConvert.SerializeObject(shortcuts);
            string script = $"showShortcuts({shortcutsJson});";
            
            await ExecuteScriptAsync(script);
        }

        public async void ReceiveMessage(string message)
        {if (message.Length < 1)
                return;


            char messageType = message[0];
            message = message.Substring(1);

            string escapedMessage = HttpUtility.JavaScriptStringEncode(message);
            //chatHistory.scrollTop = chatHistory.scrollHeight;
            
            // switch to ui thread
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();



            switch (messageType)
            {
                case 's':
                    await ExecuteScriptAsync($"chatHistory.innerHTML += '{escapedMessage}';document.querySelector('#ChatHistory').scrollTop = document.querySelector('#ChatHistory').scrollHeight;");
                    break;
                case 'e':
                    await ExecuteScriptAsync($"chatHistory.innerHTML = '{escapedMessage}';document.querySelector('#ChatHistory').scrollTop = document.querySelector('#ChatHistory').scrollHeight;");
                    break;
            }
        }

        private List<string> GetAllFilesInSolution()
        {
            var files = new List<string>();
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_dte.Solution != null)
            {
                foreach (Project project in _dte.Solution.Projects)
                {
                    GetProjectFiles(project, files);
                }
            }

            return files;
        }

        private void GetProjectFiles(Project project, List<string> files)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project == null)
                return;

            if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
            {
                foreach (ProjectItem item in project.ProjectItems)
                {
                    if (item.SubProject != null)
                    {
                        GetProjectFiles(item.SubProject, files);
                    }
                    else
                    {
                        ProcessProjectItem(item, files);
                    }
                }
            }
            else
            {
                foreach (ProjectItem item in project.ProjectItems)
                {
                    ProcessProjectItem(item, files);
                }
            }
        }

        private void ProcessProjectItem(ProjectItem item, List<string> files)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (item == null)
                return;

            if (item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder)
            {
                foreach (ProjectItem subItem in item.ProjectItems)
                {
                    ProcessProjectItem(subItem, files);
                }
            }
            else
            {
                if (item.Properties != null)
                {
                    try
                    {
                        string filePath = item.Properties.Item("FullPath").Value.ToString();
                        if (File.Exists(filePath))
                        {
                            files.Add(filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle or log the exception
                        System.Diagnostics.Debug.WriteLine($"Error processing item: {ex.Message}");
                    }
                }
            }
        }

    }
    internal class VsixResourceDetails
    {
        public string Uri { get; set; }
        public string ResourceName { get; set; }
        public string MimeType { get; set; }
    }

    public static class VsixAssemblyHelper
    {
        public static string GetEmbeddedAssembly(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    return null;

                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            } 
        }
    }

    public static class MessageFormatter
    {
        public static string InsertFilenamedSelection(string message, string documentFilename, TextSelection selection)
        {
            return message.Replace("#:selection:", $"{BacktickHelper.ThreeTicks}{documentFilename}{Environment.NewLine}{selection.Text}{Environment.NewLine}{BacktickHelper.ThreeTicksAndNewline}");
        }
    }
    public class MessagePrompt
    {
        public string MessageType { get; set; }
        public string Prompt { get; set; }

        public string ButtonLabel { get; set; }
    }
}


