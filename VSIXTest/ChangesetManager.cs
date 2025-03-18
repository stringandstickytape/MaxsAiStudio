using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedClasses;
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

        // Classes to deserialize JSON
        public class ChangesetRoot
        {
            public Changeset changeset { get; set; }
        }

        public class Changeset
        {
            public string description { get; set; }
            public List<FileChange> files { get; set; }
        }

        public class FileChange
        {
            public string path { get; set; }
            public List<ChangeItem> changes { get; set; }
        }

        public class ChangeItem
        {
            public string change_type { get; set; }
            public int lineNumber { get; set; }
            public string oldContent { get; set; }
            public string newContent { get; set; }
            public string description { get; set; }
        }

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
                var changeset = JsonConvert.DeserializeObject<ChangesetRoot>(changesetJson);
                if (changeset != null && changeset.changeset != null)
                {
                    CurrentChangeset = changeset.changeset;

                    // Extract all changes from all files for the popup
                    var allChanges = new List<ChangeItem>();
                    foreach (var file in CurrentChangeset.files)
                    {
                        allChanges.AddRange(file.changes);
                    }

                    await ShowChangesetPopupAsync(allChanges);
                    VsixDebugLog.Instance.Log("Received applyNewDiff message.");
                }
                else
                {
                    throw new Exception("Invalid changeset format");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error applying change: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ShowChangesetPopupAsync(List<ChangeItem> changes)
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

        private async Task RunMergeAsync(List<ChangeItem> changes)
        {
            await _messageHandler.SendVsixMessageAsync(
                new VsixMessage
                {
                    MessageType = "vsRunMerge",
                    Content = JsonConvert.SerializeObject(changes)
                },
                _simpleClient);
        }

        private async Task ApplyChangeAsync(ChangeItem change)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var changeType = change.change_type;
            // Find the file path from the CurrentChangeset
            string path = null;
            foreach (var file in CurrentChangeset.files)
            {
                if (file.changes.Contains(change))
                {
                    path = file.path;
                    break;
                }
            }

            if (path == null)
            {
                throw new Exception("Could not find file path for change");
            }

            try
            {
                switch (changeType)
                {
                    case "createnewFile":
                        await HandleCreateNewFileAsync(_dte, path, change);
                        break;

                    case "addToFile":
                    case "deleteFromFile":
                    case "modifyFile":
                        await HandleModifyFileAsync(_dte, path, change);
                        break;

                    case "replaceFile":
                        await HandleReplaceFileAsync(_dte, path, change);
                        break;

                    case "renameFile":
                        await HandleRenameFileAsync(_dte, path, change);
                        break;

                    case "deleteFile":
                        await HandleDeleteFileAsync(_dte, path);
                        break;

                    default:
                        throw new Exception($"Unsupported change type: {changeType}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying change: {ex}");
                MessageBox.Show($"Error applying change: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static async Task HandleCreateNewFileAsync(DTE2 dte, string path, ChangeItem change)
        {
            string deserNewContent = change.newContent;

            try
            {
                deserNewContent = JsonConvert.DeserializeObject<string>($"\"{(change.newContent ?? "")}\"");
            }
            catch (Exception)
            {
                // Handle exception if needed
            }

            var directoryPath = Path.GetDirectoryName(path);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(path, string.Empty);

            var window = TryOpenFile(dte, path);

            if (window == null)
            {
                Debug.WriteLine($"Path not found: {path}");
                throw new Exception($"Path not found: {path}");
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

        public static async Task HandleModifyFileAsync(DTE2 dte, string path, ChangeItem change)
        {
            var window = TryOpenFile(dte, path);

            if (window == null)
            {
                Debug.WriteLine($"Path not found: {path}");
                throw new Exception($"Path not found: {path}");
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

            var outp = new TextReplacer().ReplaceTextAtHint(fullText, change.oldContent, change.newContent, change.lineNumber);

            editPoint.StartOfDocument();
            editPoint.Delete(textDocument.EndPoint);
            editPoint.Insert(outp);

            var line = textDocument.StartPoint.CreateEditPoint();
            line.LineDown(change.lineNumber - 1);
            textDocument.Selection.MoveToPoint(line);

            await Task.Yield();
        }

        public static async Task HandleDeleteFileAsync(DTE2 dte, string path)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Check if file exists
            if (!File.Exists(path))
            {
                Debug.WriteLine($"File not found for deletion: {path}");
                return; // Nothing to delete
            }

            // Close any open document with this path
            foreach (Document doc in dte.Documents)
            {
                if (doc.FullName.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    doc.Close(vsSaveChanges.vsSaveChangesNo);
                    break;
                }
            }

            // Delete the file
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting file: {ex.Message}");
                throw new Exception($"Error deleting file: {ex.Message}");
            }
        }

        public static async Task HandleReplaceFileAsync(DTE2 dte, string path, ChangeItem change)
        {
            // Check if file exists, if not create it
            if (!File.Exists(path))
            {
                await HandleCreateNewFileAsync(dte, path, change);
                return;
            }

            // If file exists, replace its contents
            var window = TryOpenFile(dte, path);

            if (window == null)
            {
                Debug.WriteLine($"Path not found: {path}");
                throw new Exception($"Path not found: {path}");
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
            editPoint.Delete(textDocument.EndPoint);
            editPoint.Insert(change.newContent);

            textDocument.Selection.MoveToPoint(textDocument.StartPoint);

            await Task.Yield();

            document.Save();
        }

        public static async Task HandleRenameFileAsync(DTE2 dte, string oldPath, ChangeItem change)
        {
            // Close file if open
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Make sure the file exists
            if (!File.Exists(oldPath))
            {
                Debug.WriteLine($"Source file not found: {oldPath}");
                throw new Exception($"Source file not found: {oldPath}");
            }

            // Get the content before closing
            string fileContent = File.ReadAllText(oldPath);

            // Close any open document with this path
            foreach (Document doc in dte.Documents)
            {
                if (doc.FullName.Equals(oldPath, StringComparison.OrdinalIgnoreCase))
                {
                    doc.Close();
                    break;
                }
            }

            // Get new path from the change
            string newPath = change.newContent;
            string directoryPath = Path.GetDirectoryName(newPath);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Perform the rename
            try
            {
                // Try to delete target if it exists
                if (File.Exists(newPath))
                {
                    File.Delete(newPath);
                }

                // Move the file
                File.Move(oldPath, newPath);

                // Open the new file
                var window = TryOpenFile(dte, newPath);
                if (window != null)
                {
                    window.Activate();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error renaming file: {ex.Message}");
                throw new Exception($"Error renaming file: {ex.Message}");
            }
        }
    }
}