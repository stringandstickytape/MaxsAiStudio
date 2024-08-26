using Microsoft.VisualStudio.Shell;
using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
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
        public const string PackageGuidString = "743967b7-4ad8-4103-8a28-bf2933a5bdf2";
        public static VSIXTestPackage Instance { get; private set; }

        private NamedPipeClientStream pipeClient;
        private StreamWriter writer;
        private StreamReader reader;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            Instance = this;
            //await GetSurroundingLinesCommand.InitializeAsync(this);
            await OpenChatWindowCommand.InitializeAsync(this);

            InitializePipeClient();
        }

        private void InitializePipeClient()
        {
            pipeClient = new NamedPipeClientStream(".", "MaxsAIStudioVSIX", PipeDirection.InOut, PipeOptions.Asynchronous);
            pipeClient.Connect(3000);
            writer = new StreamWriter(pipeClient) { AutoFlush = true };
            reader = new StreamReader(pipeClient);

            // Start listening for messages
            Task.Run(ListenForMessages);
        }

        private async Task ListenForMessages()
        {
            while (true)
            {
                string message = await reader.ReadLineAsync();
                    await JoinableTaskFactory.SwitchToMainThreadAsync();
                    var window = FindToolWindow(typeof(ChatWindowPane), 0, false) as ChatWindowPane;
                    if (window != null)
                    {
                        var control = window.Content as ChatWindowControl;
                        control?.ReceiveMessage(message);
                    }
            }
        }

        public async Task SendMessageThroughPipe(string message)
        {
            await writer.WriteLineAsync(message);
            await writer.WriteLineAsync("<END>");
            await writer.FlushAsync();
        }
    }
}
