using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace VSIXTest
{
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(VSIXTestPackage.PackageGuidString)]
    [ProvideToolWindow(typeof(ChatWindowPane))]
    [ProvideToolWindow(typeof(QuickButtonOptionsWindow))] 
    public sealed class VSIXTestPackage : AsyncPackage
    {
        private readonly ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

        public const string PackageGuidString = "743967b7-4ad8-4103-8a28-bf2933a5bdf2";
        public static VSIXTestPackage Instance { get; private set; }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            VsixChat.VsixPackage = this;
            await base.InitializeAsync(cancellationToken, progress);
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            Instance = this;
            await OpenChatWindowCommand.InitializeAsync(this);
            await MaxsAiStudioAutoCompleteCommand.InitializeAsync(this);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await ShowToolWindowAsync(typeof(ChatWindowPane), 0, true, cancellationToken);
            await ShowToolWindowAsync(typeof(QuickButtonOptionsWindow), 0, true, cancellationToken);
        }

        private async Task ShowToolWindowAsync(Type toolWindowType, int id, bool create, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            ToolWindowPane window = await FindToolWindowAsync(toolWindowType, id, create, cancellationToken);
            if ((window == null) || (window.Frame == null))
            {
                throw new NotSupportedException($"Cannot create tool window of type {toolWindowType.Name}");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}