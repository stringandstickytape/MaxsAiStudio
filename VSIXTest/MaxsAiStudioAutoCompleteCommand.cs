using System;
using System.ComponentModel.Design;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SharedClasses;
using Task = System.Threading.Tasks.Task;

namespace VSIXTest
{
    internal sealed class MaxsAiStudioAutoCompleteCommand
    {
        public const int CommandId = 0x0102;
        public static readonly Guid CommandSet = new Guid("743967b7-4ad8-4103-8a28-bf2933a5bdf3");

        private readonly AsyncPackage package;
        private readonly DTE2 _dte;

        private MaxsAiStudioAutoCompleteCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE2;

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static MaxsAiStudioAutoCompleteCommand Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new MaxsAiStudioAutoCompleteCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            TextDocument textDocument = _dte.ActiveDocument.Object("TextDocument") as TextDocument;
            if (textDocument == null) return;

            EditPoint startPoint = textDocument.Selection.ActivePoint.CreateEditPoint();
            EditPoint endPoint = textDocument.Selection.ActivePoint.CreateEditPoint();
            startPoint.CharLeft(500);
            string textBefore = startPoint.GetText(endPoint);

            startPoint = textDocument.Selection.ActivePoint.CreateEditPoint();
            endPoint.CharRight(500);
            string textAfter = startPoint.GetText(endPoint);

            string output = $"{textBefore}\n//!\n{textAfter}";

            SendAutoCompleteRequest(output);
        }

        private void SendAutoCompleteRequest(string surroundingCode)
        {
            var messageHandler = new VsixMessageHandler(_dte);
            messageHandler.SendNewConversationMessage();
            messageHandler.SendMessage($"{BacktickHelper.ThreeTicks}\n{surroundingCode}\n{BacktickHelper.ThreeTicks}\n\nAutocomplete this code where you see the marker //! . Give only the inserted text and no other output, demarcated with three ticks before and after.");
        }
    }
}