using System;
using System.ComponentModel.Design;
using System.IO.Pipes;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Task = System.Threading.Tasks.Task;

namespace VSIXTest
{
    internal sealed class GetSurroundingLinesCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("743967b7-4ad8-4103-8a28-bf2933a5bdf3"); // Replace with your GUID

        private readonly AsyncPackage package;

        private GetSurroundingLinesCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static GetSurroundingLinesCommand Instance
        {
            get;
            private set;
        }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

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

                int cursorPosition = selection.ActivePoint.AbsoluteCharOffset;
                string textWithCursor = entireFileContent.Insert(cursorPosition - 1, "<CURSOR>");

                System.Diagnostics.Debug.WriteLine("Entire file content with cursor position:");
                System.Diagnostics.Debug.WriteLine(textWithCursor);

                try
                {
                    using (var pipeClient = new NamedPipeClientStream(".", "MaxsAIStudioVSIX", PipeDirection.InOut))
                    {
                        await pipeClient.ConnectAsync(5000); // Wait for a maximum of 5 seconds for the connection



                        var writer = new StreamWriter(pipeClient);

                        using (var reader = new StreamReader(pipeClient))
                        {
                            writer.AutoFlush = true;
                            await writer.WriteAsync(textWithCursor + "\n<END>\n");
                            await writer.FlushAsync();
                            System.Diagnostics.Debug.WriteLine("Entire file content with cursor position sent to AiTool3 via pipe.");

                            // Listen for the return message
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
                        }

                    }
                }
                catch (TimeoutException)
                {
                    System.Diagnostics.Debug.WriteLine("Connection to AiTool3 timed out.");
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"IO error while connecting to pipe: {ex.Message}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Unexpected error: {ex.Message}");
                }
            }
        }
    }
}