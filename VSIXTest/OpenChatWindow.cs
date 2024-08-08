using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Task = System.Threading.Tasks.Task;
namespace VSIXTest
{
    internal sealed class OpenChatWindowCommand
    {
        public const int CommandId = 0x0101;
        public static readonly Guid CommandSet = new Guid("743967b7-4ad8-4103-8a28-bf2933a5bdf3");

        private readonly AsyncPackage package;

        private OpenChatWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static OpenChatWindowCommand Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new OpenChatWindowCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var textManager = Package.GetGlobalService(typeof(SVsTextManager)) as IVsTextManager;
            if (textManager == null)
            {
                // Handle the case where we couldn't get the text manager
                return;
            }

            textManager.GetActiveView(1, null, out IVsTextView textViewCom);

            if (textViewCom == null)
            {
                // Handle the case where we couldn't get the active text view
                return;
            }

            // Get the IVsEditorAdaptersFactoryService
            var editorAdaptersFactoryService = Package.GetGlobalService(typeof(SComponentModel)) as IComponentModel;
            if (editorAdaptersFactoryService == null)
            {
                // Handle the case where we couldn't get the component model
                return;
            }

            var adaptersFactory = editorAdaptersFactoryService.GetService<IVsEditorAdaptersFactoryService>();
            if (adaptersFactory == null)
            {
                // Handle the case where we couldn't get the adapters factory
                return;
            }

            // Get the IWpfTextView from the IVsTextView
            IWpfTextView textView = adaptersFactory.GetWpfTextView(textViewCom);


            if (textView != null)
            {
                // Remove any existing InlineChatAdornment
                var existingAdornmentLayer = textView.GetAdornmentLayer("InlineChatAdornment");
                existingAdornmentLayer.RemoveAllAdornments();

                // Create a new InlineChatAdornment
                new InlineChatAdornment(textView);
            }
        }
    }
}