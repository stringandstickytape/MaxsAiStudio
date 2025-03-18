using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedClasses;
using SharedClasses.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using VSIXTest.UI;

namespace VSIXTest
{
    public class ChangesetManager
    {
        private readonly DTE2 _dte;
        private readonly VsixMessageHandler _messageHandler;
        private readonly SimpleClient _simpleClient;
        private bool _changesetPaneInitted = false;
        public Changeset CurrentChangeset { get; private set; }

        public ChangesetManager(DTE2 dte, VsixMessageHandler messageHandler, SimpleClient simpleClient)
        {
            _dte = dte;
            _messageHandler = messageHandler;
            _simpleClient = simpleClient;
        }

        public async Task HandleNewChangesetAsync(string changesetJson)
        {
            try
            {
                var changesetObj = JsonConvert.DeserializeObject<JObject>(changesetJson)["changeset"];
                CurrentChangeset = changesetObj.ToObject<Changeset>();
                await ShowChangesetPopupAsync(CurrentChangeset.Changes);
                VsixDebugLog.Instance.Log("Received applyNewDiff message.");
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error applying change: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ShowChangesetPopupAsync(List<Change> changes)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var window = await VSIXTestPackage.Instance.FindToolWindowAsync(
                    typeof(ChangesetReviewPane),
                    0,
                    true,
                    VSIXTestPackage.Instance.DisposalToken) as ChangesetReviewPane;

                if (window?.Frame == null)
                    throw new NotSupportedException("Cannot create changeset review window");

                if (!_changesetPaneInitted)
                {
                    _changesetPaneInitted = true;
                    window.ChangeApplied += ChangesetReviewWindow_ChangeApplied;
                    window.RunMerge += ChangesetReviewWindow_RunMerge;
                }
                window.Initialize(changes);

                IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing changeset window: {ex}");
            }
        }

        private async void ChangesetReviewWindow_RunMerge(object sender, RunMergeEventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (e.Changes != null)
                {
                    await RunMergeAsync(e.Changes);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying change: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ChangesetReviewWindow_ChangeApplied(object sender, ChangeAppliedEventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (e.Change != null)
                {
                    await ApplyChangeAsync(e.Change);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying change: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RunMergeAsync(List<Change> changes)
        {
            await _messageHandler.SendVsixMessageAsync(
                new VsixMessage
                {
                    MessageType = "vsRunMerge",
                    Content = JsonConvert.SerializeObject(changes)
                },
                _simpleClient);
        }

        private async Task ApplyChangeAsync(Change change)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var changeType = change.ChangeType;
            var path = change.Path;

            try
            {
                switch (changeType)
                {
                    case "createnewFile":
                        await HandleCreateNewFileAsync(_dte, change);
                        break;

                    case "addToFile":
                    case "deleteFromFile":
                    case "modifyFile":
                        await HandleModifyFileAsync(_dte, change);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying change: {ex}");
                MessageBox.Show($"Error applying change: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static async Task HandleCreateNewFileAsync(DTE2 dte, Change change)
        {
            string deserNewContent = change.NewContent;

            try
            {
                deserNewContent = JsonConvert.DeserializeObject<string>($"\"{(change.NewContent ?? "")}\"");
            }
            catch (Exception)
            {
                // Handle exception if needed
            }

            var directoryPath = Path.GetDirectoryName(change.Path);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(change.Path, string.Empty);

            var window = TryOpenFile(dte, change.Path);

            if (window == null)
            {
                Debug.WriteLine($"Path not found: {change.Path}");
                throw new Exception($"Path not found: {change.Path}");
            }

            await Task.Yield();

            window.Activate();
            var document = window.Document;

            if (document == null)
            {
                Debug.WriteLine("Document is null");
                throw new Exception("Document is null");
            }

            var textDocument = document.Object() as TextDocument;
            if (textDocument == null)
            {
                Debug.WriteLine("TextDocument is null");
                throw new Exception("TextDocument is null");
            }

            var editPoint = textDocument.StartPoint.CreateEditPoint();
            editPoint.Insert(deserNewContent);

            textDocument.Selection.MoveToPoint(textDocument.StartPoint);

            await Task.Yield();

            document.Save();
        }

        private static EnvDTE.Window TryOpenFile(DTE dte, string filePath)
        {
            EnvDTE.Window window = null;
            Exception lastException = null;

            try
            {
                // Try method 1: Default view kind
                window = dte.ItemOperations.OpenFile(filePath, EnvDTE.Constants.vsViewKindCode);
                return window;
            }
            catch (Exception ex)
            {
                lastException = ex;
                Debug.WriteLine($"Method 1 failed: {ex.Message}");
            }

            try
            {
                // Try method 2: Text view kind
                window = dte.ItemOperations.OpenFile(filePath, EnvDTE.Constants.vsViewKindTextView);
                return window;
            }
            catch (Exception ex)
            {
                lastException = ex;
                Debug.WriteLine($"Method 2 failed: {ex.Message}");
            }

            try
            {
                // Try method 3: No view kind specified
                window = dte.ItemOperations.OpenFile(filePath);
                return window;
            }
            catch (Exception ex)
            {
                lastException = ex;
                Debug.WriteLine($"Method 3 failed: {ex.Message}");
            }

            try
            {
                // Try method 4: Full path with code view
                var fullPath = Path.GetFullPath(filePath);
                window = dte.ItemOperations.OpenFile(fullPath, EnvDTE.Constants.vsViewKindCode);
                return window;
            }
            catch (Exception ex)
            {
                lastException = ex;
                Debug.WriteLine($"Method 4 failed: {ex.Message}");
            }

            // If we get here, all methods failed
            throw new Exception($"Failed to open file {filePath} using all available methods", lastException);
        }

        public static async Task HandleModifyFileAsync(DTE2 dte, Change change)
        {
            var window = TryOpenFile(dte, change.Path);

            if (window == null)
            {
                Debug.WriteLine($"Path not found: {change.Path}");
                throw new Exception($"Path not found: {change.Path}");
            }

            await Task.Yield();

            window.Activate();
            var document = window.Document;

            if (document == null)
            {
                Debug.WriteLine("Document is null");
                throw new Exception("Document is null");
            }

            var textDocument = document.Object() as TextDocument;
            if (textDocument == null)
            {
                Debug.WriteLine("TextDocument is null");
                throw new Exception("TextDocument is null");
            }

            var editPoint = textDocument.StartPoint.CreateEditPoint();
            var fullText = editPoint.GetText(textDocument.EndPoint);

            // Use the shared TextReplacer class to handle text replacement
            var outp = new TextReplacer().ReplaceTextAtHint(fullText, change.OldContent, change.NewContent, change.LineNumber);

            editPoint.StartOfDocument();
            editPoint.Delete(textDocument.EndPoint);
            editPoint.Insert(outp);

            var line = textDocument.StartPoint.CreateEditPoint();
            line.LineDown(change.LineNumber - 1);
            textDocument.Selection.MoveToPoint(line);

            await Task.Yield();
        }
    }
}