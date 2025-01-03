using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using System.IO;

namespace VSIXTest
{
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(VSIXTestPackage.PackageGuidString)]
    [ProvideToolWindow(typeof(ChatWindowPane))]
    [ProvideToolWindow(typeof(QuickButtonOptionsWindow))]
    [ProvideToolWindow(typeof(ChangesetReviewPane))]
    public sealed class VSIXTestPackage : AsyncPackage, IVsSolutionEvents, IVsFileChangeEvents
    {
        private uint _solutionEventsCookie;
        private IVsSolution _solution;
        private IVsFileChangeEx _fileChangeService;
        private uint _fileChangeCookie;

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

            // Get the file change service
            _fileChangeService = await GetServiceAsync(typeof(SVsFileChangeEx)) as IVsFileChangeEx;

            // Get the solution service and register for events
            _solution = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            if (_solution != null)
            {
                _solution.AdviseSolutionEvents(this, out _solutionEventsCookie);
            }
        }

        private void StartWatchingSystemPrompt(string solutionPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Stop watching previous file if any
            StopWatchingSystemPrompt();

            if (string.IsNullOrEmpty(solutionPath))
                return;

            string systemPromptPath = Path.Combine(Path.GetDirectoryName(solutionPath), "systemprompt.txt");

            if (_fileChangeService != null)
            {
                // Watch for file changes
                _fileChangeService.AdviseFileChange(
                    systemPromptPath,
                    (uint)(_VSFILECHANGEFLAGS.VSFILECHG_Time | _VSFILECHANGEFLAGS.VSFILECHG_Size),
                    this,
                    out _fileChangeCookie);
            }
        }

        private void StopWatchingSystemPrompt()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_fileChangeService != null && _fileChangeCookie != 0)
            {
                _fileChangeService.UnadviseFileChange(_fileChangeCookie);
                _fileChangeCookie = 0;
            }
        }

        #region IVsSolutionEvents Implementation
        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var solution = (IVsSolution)Package.GetGlobalService(typeof(SVsSolution));
            solution.GetSolutionInfo(out string solutionDir, out string solutionFile, out string userOptsFile);

            // Start watching the system prompt file
            StartWatchingSystemPrompt(solutionFile);

            // Load initial system prompt
            _ = VsixChat.Instance.SetSolutionSystemPrompt();

            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }
        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) { return Microsoft.VisualStudio.VSConstants.S_OK; }
        public int OnBeforeCloseSolution(object pUnkReserved) { return Microsoft.VisualStudio.VSConstants.S_OK; }
        public int OnAfterCloseSolution(object pUnkReserved)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            StopWatchingSystemPrompt();
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }
        public int OnBeforeOpenSolution(string pszSolutionFilename) { return Microsoft.VisualStudio.VSConstants.S_OK; }
        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) { return Microsoft.VisualStudio.VSConstants.S_OK; }
        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) { return Microsoft.VisualStudio.VSConstants.S_OK; }
        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) { return Microsoft.VisualStudio.VSConstants.S_OK; }
        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) { return Microsoft.VisualStudio.VSConstants.S_OK; }
        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) { return Microsoft.VisualStudio.VSConstants.S_OK; }
        public int OnAfterCloseProject(IVsHierarchy pHierarchy, int fRemoved) { return Microsoft.VisualStudio.VSConstants.S_OK; }
        #endregion

        #region IVsFileChangeEx Implementation

        public int FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Check if our systemprompt.txt was changed
            for (int i = 0; i < cChanges; i++)
            {
                if (Path.GetFileName(rgpszFile[i]).Equals("systemprompt.txt", StringComparison.OrdinalIgnoreCase))
                {
                    _ = VsixChat.Instance.SetSolutionSystemPrompt();
                    break;
                }
            }
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int DirectoryChanged(string pszDirectory)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                // Stop watching file changes
                StopWatchingSystemPrompt();

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