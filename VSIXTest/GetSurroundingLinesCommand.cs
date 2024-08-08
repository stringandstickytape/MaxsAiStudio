using System;
using System.ComponentModel.Design;
using System.IO.Pipes;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Task = System.Threading.Tasks.Task;
using Newtonsoft.Json;

namespace VSIXTest
{
    internal sealed class GetSurroundingLinesCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("743967b7-4ad8-4103-8a28-bf2933a5bdf3");

        private readonly AsyncPackage package;
        private NamedPipeClientStream pipeClient;
        private StreamWriter writer;
        private StreamReader reader;

        private GetSurroundingLinesCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);

            InitializePipeClient();
        }

        private void InitializePipeClient()
        {
            pipeClient = new NamedPipeClientStream(".", "MaxsAIStudioVSIX", PipeDirection.InOut, PipeOptions.Asynchronous);
            pipeClient.Connect(3000);
            writer = new StreamWriter(pipeClient) { AutoFlush = true };
            reader = new StreamReader(pipeClient);
        }

        public static GetSurroundingLinesCommand Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new GetSurroundingLinesCommand(package, commandService);
        }

        private async void Execute(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = Package.GetGlobalService(typeof(SDTE)) as EnvDTE80.DTE2;
            var textDocument = dte.ActiveDocument.Object("TextDocument") as EnvDTE.TextDocument;

            if (textDocument != null)
            {
                var selection = textDocument.Selection;
                string entireFileContent = textDocument.CreateEditPoint(textDocument.StartPoint).GetText(textDocument.EndPoint);

                int selectionStart = selection.TopPoint.AbsoluteCharOffset - 1;
                int selectionEnd = selection.BottomPoint.AbsoluteCharOffset - 1;

                string before = entireFileContent.Substring(0, selectionStart);
                string selected = selection.Text;
                string after = entireFileContent.Substring(selectionEnd);

                var jsonObject = new
                {
                    before,
                    selected,
                    after
                };

                string jsonString = JsonConvert.SerializeObject(jsonObject);

                try
                {
                    await writer.WriteLineAsync(jsonString);
                    await writer.WriteLineAsync("<END>");
                    await writer.FlushAsync();
                    System.Diagnostics.Debug.WriteLine("JSON object sent to AiTool3 via pipe.");

                    string returnMessage = "";
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (line == "<END>")
                            break;
                        returnMessage += line + "\n";
                    }
                    System.Diagnostics.Debug.WriteLine("Received return message from AiTool3:");
                    System.Diagnostics.Debug.WriteLine(returnMessage);

                    // insert the return message into the active document at the current cursor location
                    selection.Insert(returnMessage);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in pipe communication: {ex.Message}");
                    InitializePipeClient(); // Try to reconnect
                }
            }
        }
    }
}