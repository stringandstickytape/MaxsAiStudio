using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Newtonsoft.Json;
using SharedClasses;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace VSIXTest
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(VSIXTestPackage.PackageGuidString)]
    [ProvideToolWindow(typeof(ChatWindowPane))]
    public sealed class VSIXTestPackage : AsyncPackage
    {
        private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();
        private SemaphoreSlim processingSemaphore = new SemaphoreSlim(1, 1);


        public const string PackageGuidString = "743967b7-4ad8-4103-8a28-bf2933a5bdf2";
        public static VSIXTestPackage Instance { get; private set; }
        public DTE2 DTE { get; private set; }

        private NamedPipeClientStream pipeClient;
        private StreamWriter writer;
        private StreamReader reader;
        private TcpCommsManager namedPipeManager;
        private bool isClientInitialized = false;
        private SemaphoreSlim clientInitSemaphore = new SemaphoreSlim(1, 1);


        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            Instance = this;
            await OpenChatWindowCommand.InitializeAsync(this);
            await MaxsAiStudioAutoCompleteCommand.InitializeAsync(this);
        }

        private async Task InitializeClientAsync()
        {
            await clientInitSemaphore.WaitAsync();
            try
            {
                if (!isClientInitialized)
                {
                    namedPipeManager = new TcpCommsManager(isVsix: true);
                    namedPipeManager.ReceiveMessage += NamedPipeManager_ReceiveMessage;
                    await namedPipeManager.ConnectAsync();
                    isClientInitialized = true;
                }
            }
            finally
            {
                clientInitSemaphore.Release();
            }
        }

        protected override void Dispose(bool disposing)
        {
            namedPipeManager.Dispose();
            base.Dispose(disposing);
        }

        private async void NamedPipeManager_ReceiveMessage(object sender, object e)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var chatWindowPane = GetChatWindowPane();
            if (chatWindowPane != null)
            {
                var chatWindowControl = chatWindowPane.Content as ChatWindowControl;
                if (chatWindowControl != null && chatWindowControl.WebView != null)
                {
                    // Convert e (jobject) to VsixMessage obj
                    var vsixMessage = JsonConvert.DeserializeObject<VsixMessage>(e.ToString());
                    chatWindowControl.WebView.ReceiveMessage(vsixMessage);


                }
            }
        }

        private ChatWindowPane GetChatWindowPane()
        {
            return this.FindToolWindow(typeof(ChatWindowPane), 0, true) as ChatWindowPane;
        }

        public async Task SendMessageThroughPipe(string message)
        {
            if (!isClientInitialized)
            {
                await InitializeClientAsync();
            }
            namedPipeManager.EnqueueMessage(message);
        }
    }
}
