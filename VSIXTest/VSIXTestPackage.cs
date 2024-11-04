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
    public sealed class VSIXTestPackage : AsyncPackage, IVsSolutionEvents
    {
        private uint _solutionEventsCookie;
        private IVsSolution _solution;

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

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await ShowToolWindowAsync(typeof(ChatWindowPane), 0, true, cancellationToken);
            //await ShowToolWindowAsync(typeof(QuickButtonOptionsWindow), 0, true, cancellationToken);

            // Add this after the existing initialization:
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Get the solution service and register for events
            _solution = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            if (_solution != null)
            {
                _solution.AdviseSolutionEvents(this, out _solutionEventsCookie);
            }
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Instead of calling VsixChat.Instance.OnSolutionOpened(), 
            // call the existing SetSolutionSystemPrompt method:
            _ = VsixChat.Instance.SetSolutionSystemPrompt();

            return Microsoft.VisualStudio.VSConstants.S_OK;
        }
        // You need to implement other IVsSolutionEvents methods, but can leave them empty if not needed
        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }
        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) { return Microsoft.VisualStudio.VSConstants.S_OK; }
        public int OnBeforeCloseSolution(object pUnkReserved) { return Microsoft.VisualStudio.VSConstants.S_OK; }
        public int OnAfterCloseSolution(object pUnkReserved) { return Microsoft.VisualStudio.VSConstants.S_OK; }
        public int OnBeforeOpenSolution(string pszSolutionFilename) { return Microsoft.VisualStudio.VSConstants.S_OK; }
        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) { return Microsoft.VisualStudio.VSConstants.S_OK; }
        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) { return Microsoft.VisualStudio.VSConstants.S_OK; }
        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) { return Microsoft.VisualStudio.VSConstants.S_OK; }
        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) { return Microsoft.VisualStudio.VSConstants.S_OK; }
        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) { return Microsoft.VisualStudio.VSConstants.S_OK; }
        public int OnAfterCloseProject(IVsHierarchy pHierarchy, int fRemoved) { return Microsoft.VisualStudio.VSConstants.S_OK; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unregister from solution events
                if (_solution != null && _solutionEventsCookie != 0)
                {
                    _solution.UnadviseSolutionEvents(_solutionEventsCookie);
                    _solutionEventsCookie = 0;
                }
            }
            base.Dispose(disposing);
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

