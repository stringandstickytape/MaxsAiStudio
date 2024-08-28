using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
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

        private readonly ResourceManager _resourceManager;

        public VsixChat() : base()
        {
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            Loaded +=  VsixChat_Loaded;
            _resourceManager = new ResourceManager(Assembly.GetExecutingAssembly());

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
            if (_resourceManager.IsResourceRequested(e.Request.Uri))
            {
                var resourceDetail = _resourceManager.GetResourceDetailByUri(e.Request.Uri);
                _resourceManager.ReturnResourceToWebView(e, resourceDetail.ResourceName, resourceDetail.MimeType, CoreWebView2);
            }
        }

     

        private static readonly MessagePrompt[] MessagePrompts = new[]
        {
            // Code Analysis and Explanation
            new MessagePrompt { Category = "Code Analysis", ButtonLabel = "Explain Code", MessageType = "explainCode", Prompt = "Provide a detailed explanation of what this code does:" },
            new MessagePrompt { Category = "Code Analysis", ButtonLabel = "Identify Potential Bugs", MessageType = "identifyBugs", Prompt = "Analyze this code for potential bugs or edge cases that might cause issues:" },

            // Code Improvement and Refactoring
            new MessagePrompt { Category = "Refactoring 1", ButtonLabel = "Extract Method", MessageType = "extractMethod", Prompt = "Perform an extract method on this:" },
            new MessagePrompt { Category = "Refactoring 1", ButtonLabel = "Extract Static Method", MessageType = "extractStaticMethod", Prompt = "Perform an extract static method on this:" },
            new MessagePrompt { Category = "Refactoring 1", ButtonLabel = "DRY This", MessageType = "dryThis", Prompt = "Suggest some clever ways, with examples, to DRY this code:" },
            new MessagePrompt { Category = "Refactoring 1", ButtonLabel = "General Refactor", MessageType = "generalRefactor", Prompt = "Suggest some clever ways, with examples, to generally refactor this code:" },
            new MessagePrompt { Category = "Refactoring 1", ButtonLabel = "Improve Performance", MessageType = "improvePerformance", Prompt = "Analyse and, if possible, suggest some clever ways with examples, to improve the performance of this code:" },
            new MessagePrompt { Category = "Refactoring 1", ButtonLabel = "Simplify Logic", MessageType = "simplifyLogic", Prompt = "Analyze and suggest ways to simplify the logic in this code without changing its functionality:" },
            new MessagePrompt { Category = "Refactoring 2", ButtonLabel = "Convert to LINQ", MessageType = "convertToLinq", Prompt = "Convert this code to use LINQ expressions where appropriate:" },
            new MessagePrompt { Category = "Refactoring 2", ButtonLabel = "Extract Best Class", MessageType = "extractBestClass", Prompt = "Analyze this code and identify the single best class that could be extracted to improve general Object-Oriented Programming (OOP) principles. Describe the proposed class, its properties, methods, and how it would enhance the overall design:" },
            new MessagePrompt { Category = "Refactoring 2", ButtonLabel = "String Interpolation", MessageType = "stringInterpolation", Prompt = "Rewrite this to use string interpolation:" },

            // Code Enhancement
            new MessagePrompt { Category = "Enhancement", ButtonLabel = "Add Error Handling", MessageType = "addErrorHandling", Prompt = "Suggest appropriate error handling mechanisms for this code:" },
            new MessagePrompt { Category = "Enhancement", ButtonLabel = "Add Logging", MessageType = "addLogging", Prompt = "Suggest appropriate logging statements to add to this code for better debugging and monitoring:" },

            // Naming and Documentation
            new MessagePrompt { Category = "Documentation", ButtonLabel = "Suggest Name", MessageType = "suggestName", Prompt = "Suggest a concise and descriptive name for this code element:" },
            new MessagePrompt { Category = "Documentation", ButtonLabel = "Commit Message", MessageType = "commitMsg", Prompt = "Give me a short, high-quality, bulleted, tersely-phrased summary for this diff.  Break the changes down by project and category.  Demarcate the summary as a single code block. Do not mention unused categories or insignficiant changes." },

            // Code Generation and Extension
            new MessagePrompt { Category = "Generation", ButtonLabel = "Autocomplete at //"+"! marker", MessageType = "autocompleteThis", Prompt = "Autocomplete this code where you see the marker //! . Give only the inserted text and no other output, demarcated with three ticks before and after." },
            new MessagePrompt { Category = "Generation", ButtonLabel = "Extend Series", MessageType = "addToSeries", Prompt = "Extend the series you see in this code:" },
            new MessagePrompt { Category = "Generation", ButtonLabel = "Create Unit Tests", MessageType = "createUnitTests", Prompt = "Generate unit tests for this code:" },

            // Code Readability
            new MessagePrompt { Category = "Readability", ButtonLabel = "Add Comments", MessageType = "addComments", Prompt = "Add appropriate comments to this code to improve its readability:" },
            new MessagePrompt { Category = "Readability", ButtonLabel = "Remove Comments", MessageType = "removeComments", Prompt = "Remove all comments from this code:" },

            // User Documentation
            new MessagePrompt { Category = "Documentation", ButtonLabel = "Generate README", MessageType = "generateReadme", Prompt = "Generate a comprehensive README.md file for this project based on the code provided:" },
            new MessagePrompt { Category = "Documentation", ButtonLabel = "Create User Guide", MessageType = "createUserGuide", Prompt = "Create a user guide explaining how to use the functionality implemented in this code:" },
            new MessagePrompt { Category = "Documentation", ButtonLabel = "API Documentation", MessageType = "generateApiDocs", Prompt = "Generate API documentation for the public methods and classes in this code:" },
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

    // Clear existing buttons
    buttonContainer.innerHTML = '';

    // Function to create a button
    function createButton(label, messageType) {
        var button = document.createElement('button');
        button.textContent = label;
        button.onclick = function() {
            performAction(messageType);
        };
        return button;
    }

    // Group prompts by category
    var groupedPrompts = {};
    ");

            foreach (var prompt in MessagePrompts)
            {
                scriptBuilder.Append($@"
    if (!groupedPrompts['{prompt.Category}']) {{
        groupedPrompts['{prompt.Category}'] = [];
    }}
    groupedPrompts['{prompt.Category}'].push({{ label: '{prompt.ButtonLabel}', messageType: '{prompt.MessageType}' }});
    ");
            }

            scriptBuilder.Append(@"
    // Create category boxes and add buttons
    for (var category in groupedPrompts) {
        var categoryBox = document.createElement('div');
        categoryBox.className = 'category-box';
        var categoryTitle = document.createElement('div');
        categoryTitle.className = 'category-title';
        categoryTitle.textContent = category;
        categoryBox.appendChild(categoryTitle);

        groupedPrompts[category].forEach(function(prompt) {
            var button = createButton(prompt.label, prompt.messageType);
            categoryBox.appendChild(button);
        });

        buttonContainer.appendChild(categoryBox);
    }

    // Add 'New' button
    var newButton = createButton('New', 'newChat');
    buttonContainer.appendChild(newButton);
    ");

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
                    var insertionType = messageType == "commitMsg" ? BacktickHelper.PrependHash(":diff:") : BacktickHelper.PrependHash(":selection:");

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
            VSIXTestPackage.Instance.SendMessageThroughPipe(JsonConvert.SerializeObject(new VsixMessage { MessageType = "new" }));
        }

        private string ReplaceFileNameWithContent(string message, string file)
        {
            string fileName = $"#{Path.GetFileName(file)}";
            if (message.IndexOf(fileName, StringComparison.OrdinalIgnoreCase) >= 0)
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
                return ReplaceIgnoreCase(message, fileName, replacement);
            }
            return message;
        }

        private static string ReplaceIgnoreCase(string source, string oldValue, string newValue)
        {
            int index = source.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                return source;

            StringBuilder result = new StringBuilder();
            int previousIndex = 0;

            while (index >= 0)
            {
                result.Append(source, previousIndex, index - previousIndex);
                result.Append(newValue);
                index += oldValue.Length;
                previousIndex = index;
                index = source.IndexOf(oldValue, index, StringComparison.OrdinalIgnoreCase);
            }

            result.Append(source, previousIndex, source.Length - previousIndex);

            return result.ToString();
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

                // Replace #:all-open: with contents of all open code windows
                if (message.Contains(BacktickHelper.PrependHash(":all-open:")))
                {
                    message = ReplaceAllOpenContents(message);
                }

                // replace any '<hash>:selection:' with the selected text
                if (message.Contains(BacktickHelper.PrependHash(":selection:")) && _dte.ActiveDocument != null)
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    var selection = (TextSelection)_dte.ActiveDocument.Selection;
                    var documentFilename = _dte.ActiveDocument.Name;
                    string textToInsert = "";

                    if (selection.IsEmpty)
                    {
                        // If selection is empty, get the entire text of the document
                        TextDocument textDocument = _dte.ActiveDocument.Object("TextDocument") as TextDocument;
                        EditPoint startPoint = textDocument.StartPoint.CreateEditPoint();
                        textToInsert = startPoint.GetText(textDocument.EndPoint);
                    }
                    else
                    {
                        // If there's a selection, use the selected text
                        textToInsert = selection.Text;
                    }

                    message = MessageFormatter.InsertFilenamedSelection(message, documentFilename, textToInsert);
                }

                if (message.Contains(BacktickHelper.PrependHash(":diff:")))
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    var gitDiffHelper = new GitDiffHelper();
                    var diff = gitDiffHelper.GetGitDiff();
                    message = message.Replace(BacktickHelper.PrependHash(":diff:"), diff);
                }

                var vsixOutgoingMessage = new VsixMessage { Content = message, MessageType = "prompt" };
                string jsonMessage = JsonConvert.SerializeObject(vsixOutgoingMessage);

                VSIXTestPackage.Instance.SendMessageThroughPipe(jsonMessage); // messagetype is p (for prompt)
            }
        }

        private string ReplaceAllOpenContents(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            StringBuilder allOpenContents = new StringBuilder();

            foreach (EnvDTE.Window window in _dte.Windows)
            {
                if (window.Kind == "Document" && window.Document != null)
                {
                    TextDocument textDoc = window.Document.Object("TextDocument") as TextDocument;
                    if (textDoc != null)
                    {
                        string fileName = window.Document.Name;
                        string fileContent = textDoc.StartPoint.CreateEditPoint().GetText(textDoc.EndPoint);
                        allOpenContents.AppendLine($"File: {fileName}\n{BacktickHelper.ThreeTicks}\n{fileContent}\n{BacktickHelper.ThreeTicks}\n");
                    }
                }
            }

            return message.Replace(BacktickHelper.PrependHash(":all-open:"), allOpenContents.ToString().TrimEnd());
        }

        private async void GetShortcuts(string token)
        {
            var shortcuts = new List<string> { BacktickHelper.PrependHash(":all-open:"), BacktickHelper.PrependHash(":selection:"), BacktickHelper.PrependHash(":diff:" )};
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

        public async void ReceiveMessage(VsixMessage message)
        {
            string escapedMessage = HttpUtility.JavaScriptStringEncode(message.Content);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            switch (message.MessageType)
            {
                //case 's':
                //    await ExecuteScriptAsync($"chatHistory.innerHTML += '{escapedMessage}';document.querySelector('#ChatHistory').scrollTop = document.querySelector('#ChatHistory').scrollHeight;");
                //    break;
                case "response":
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

    public static class MessageFormatter
    {
        public static string InsertFilenamedSelection(string message, string documentFilename, string selection)
        {
            return message.Replace(BacktickHelper.PrependHash(":selection:"), $"{BacktickHelper.ThreeTicks}{documentFilename}{Environment.NewLine}{selection}{Environment.NewLine}{BacktickHelper.ThreeTicksAndNewline}");
        }
    }

}


