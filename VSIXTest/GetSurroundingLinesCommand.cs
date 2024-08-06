using System;
using System.ComponentModel.Design;
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

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = Package.GetGlobalService(typeof(SDTE)) as EnvDTE80.DTE2;
            var textDocument = dte.ActiveDocument.Object("TextDocument") as EnvDTE.TextDocument;

            if (textDocument != null)
            {
                var selection = textDocument.Selection;
                var currentLine = selection.CurrentLine;
                var text = textDocument.CreateEditPoint().GetLines(Math.Max(1, currentLine - 10), Math.Min(textDocument.EndPoint.Line, currentLine + 10));

                System.Diagnostics.Debug.WriteLine("Surrounding 20 lines:");
                System.Diagnostics.Debug.WriteLine(text);
            }
        }
    }
}