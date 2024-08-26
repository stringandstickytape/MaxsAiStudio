using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;

namespace VSIXTest
{
    public partial class ChatWindowControl : UserControl
    {
        private DTE2 _dte;

        public ChatWindowControl()
        {
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            InitializeComponent();
            ShortcutPopup.LayoutUpdated += ShortcutPopup_LayoutUpdated;
        }

        private void ShortcutPopup_LayoutUpdated(object sender, EventArgs e)
        {
            PositionPopup();
        }

        private void PopulateAndShowShortcutMenu()
        {
            ShortcutListBox.Items.Clear();
            string currentToken = GetCurrentHashtagToken().ToLower();
            var files = GetAllFilesInSolution();

            ShortcutListBox.Items.Add(new ListBoxItem { Content = $"#:selection:", Tag = "selection" });

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file).ToLower();
                if (fileName.Contains(currentToken))
                {
                    ShortcutListBox.Items.Add(new ListBoxItem
                    {
                        Content = $"#{Path.GetFileName(file)}",
                        Tag = file
                    });
                }
            }

            if (ShortcutListBox.Items.Count > 0)
            {
                ShortcutPopup.IsOpen = true;

                // Use dispatcher to ensure UI is updated before positioning
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    PositionPopup();
                }), System.Windows.Threading.DispatcherPriority.Render);
            }
            else
            {
                ShortcutPopup.IsOpen = false;
            }
        }

        private void PositionPopup()
        {
            if (ShortcutPopup.IsOpen)
            {
                var caretPosition = InputTextBox.GetRectFromCharacterIndex(InputTextBox.CaretIndex);
                var popupPosition = InputTextBox.TranslatePoint(new Point(caretPosition.Left, caretPosition.Top), this);

                // Measure the popup to get its actual size
                ShortcutPopup.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                ShortcutPopup.Arrange(new Rect(ShortcutPopup.DesiredSize));

                double popupHeight = ShortcutPopup.ActualHeight;
                double popupWidth = ShortcutPopup.ActualWidth;

                
                // Position the popup above the caret
                ShortcutPopup.HorizontalOffset = caretPosition.X+20;
                ShortcutPopup.VerticalOffset = 0-InputTextBox.Height; // 5 is an additional offset
            }
        }

        private void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (ShortcutPopup.IsOpen)
            {
                switch (e.Key)
                {
                    case Key.Down:
                        if (ShortcutListBox.SelectedIndex == -1)
                            ShortcutListBox.SelectedIndex = 0;
                        else
                            ShortcutListBox.SelectedIndex = (ShortcutListBox.SelectedIndex + 1) % ShortcutListBox.Items.Count;
                        ShortcutListBox.ScrollIntoView(ShortcutListBox.SelectedItem);
                        e.Handled = true;
                        break;
                    case Key.Up:
                        if (ShortcutListBox.SelectedIndex == -1)
                            ShortcutListBox.SelectedIndex = ShortcutListBox.Items.Count - 1;
                        else
                            ShortcutListBox.SelectedIndex = (ShortcutListBox.SelectedIndex - 1 + ShortcutListBox.Items.Count) % ShortcutListBox.Items.Count;
                        ShortcutListBox.ScrollIntoView(ShortcutListBox.SelectedItem);
                        e.Handled = true;
                        break;
                    case Key.Enter:
                        if (ShortcutListBox.SelectedItem != null)
                        {
                            InsertSelectedShortcut();
                            e.Handled = true;
                        }
                        break;
                    case Key.Escape:
                        ShortcutPopup.IsOpen = false;
                        e.Handled = true;
                        break;
                }
            }
            else if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SendMessage();
                e.Handled = true;
            }
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsCaretAtEndOfHashTag())
            {
                PopulateAndShowShortcutMenu();
            }
            else
            {
                ShortcutPopup.IsOpen = false;
            }
        }

        private string GetCurrentHashtagToken()
        {
            int caretIndex = InputTextBox.CaretIndex;
            string text = InputTextBox.Text;

            int hashIndex = text.LastIndexOf('#', caretIndex - 1);
            if (hashIndex == -1) return string.Empty;

            return text.Substring(hashIndex + 1, caretIndex - hashIndex - 1);
        }

        private bool IsCaretAtEndOfHashTag()
        {
            int caretIndex = InputTextBox.CaretIndex;
            string text = InputTextBox.Text;

            // Check if we're at the end of the text
            if (caretIndex == text.Length)
            {
                int hashIndex = text.LastIndexOf('#');
                if (hashIndex == -1) return false;

                // Check if there's whitespace before the '#'
                if (hashIndex > 0 && !char.IsWhiteSpace(text[hashIndex - 1])) return false;

                // Check if there's non-whitespace after the '#'
                for (int i = hashIndex + 1; i < text.Length; i++)
                {
                    if (char.IsWhiteSpace(text[i])) return false;
                    if (!char.IsWhiteSpace(text[i])) return true;
                }
            }
            else if (caretIndex > 0)
            {
                // If we're not at the end, check if we're right after a non-whitespace character
                if (!char.IsWhiteSpace(text[caretIndex - 1]))
                {
                    int hashIndex = text.LastIndexOf('#', caretIndex - 1);
                    if (hashIndex == -1) return false;

                    // Check if there's whitespace before the '#'
                    if (hashIndex > 0 && !char.IsWhiteSpace(text[hashIndex - 1])) return false;

                    // Check if there's only non-whitespace between '#' and caret
                    for (int i = hashIndex + 1; i < caretIndex; i++)
                    {
                        if (char.IsWhiteSpace(text[i])) return false;
                    }

                    return true;
                }
            }

            return false;
        }

        private void ShortcutListBox_MouseClick(object sender, MouseButtonEventArgs e)
        {
            InsertSelectedShortcut();
        }

        private void InsertSelectedShortcut()
        {
            if (ShortcutListBox.SelectedItem is ListBoxItem selectedItem)
            {
                string text = InputTextBox.Text;
                int caretIndex = InputTextBox.CaretIndex;
                int hashIndex = text.LastIndexOf('#', caretIndex - 1);

                if (hashIndex != -1)
                {
                    string insertText = selectedItem.Content.ToString();
                    InputTextBox.Text = text.Substring(0, hashIndex) + insertText + text.Substring(caretIndex);
                    InputTextBox.CaretIndex = hashIndex + insertText.Length;
                }

                ShortcutPopup.IsOpen = false;
            }
        }



        private List<string> GetAllFilesInSolution()
        {
            var files = new List<string>();
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_dte.Solution != null)
            {
                foreach (Project project in _dte.Solution.Projects)
                {
                    GetProjectFiles(project, files);
                }
            }

            return files;
        }

        private void GetProjectFiles(Project project, List<string> files)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project == null)
                return;

            if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
            {
                foreach (ProjectItem item in project.ProjectItems)
                {
                    if (item.SubProject != null)
                    {
                        GetProjectFiles(item.SubProject, files);
                    }
                    else
                    {
                        ProcessProjectItem(item, files);
                    }
                }
            }
            else
            {
                foreach (ProjectItem item in project.ProjectItems)
                {
                    ProcessProjectItem(item, files);
                }
            }
        }

        private void ProcessProjectItem(ProjectItem item, List<string> files)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (item == null)
                return;

            if (item.Kind == Constants.vsProjectItemKindPhysicalFolder)
            {
                foreach (ProjectItem subItem in item.ProjectItems)
                {
                    ProcessProjectItem(subItem, files);
                }
            }
            else
            {
                if (item.Properties != null)
                {
                    try
                    {
                        string filePath = item.Properties.Item("FullPath").Value.ToString();
                        if (File.Exists(filePath))
                        {
                            files.Add(filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle or log the exception
                        System.Diagnostics.Debug.WriteLine($"Error processing item: {ex.Message}");
                    }
                }
            }
        }

        private void ShortcutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                string text = InputTextBox.Text;
                int caretIndex = InputTextBox.CaretIndex;
                int hashIndex = text.LastIndexOf('#', caretIndex - 1);

                if (hashIndex != -1)
                {
                    string insertText = menuItem.Header.ToString();
                    InputTextBox.Text = text.Substring(0, hashIndex) + insertText + text.Substring(caretIndex);
                    InputTextBox.CaretIndex = hashIndex + insertText.Length;
                }

                ShortcutPopup.IsOpen = false;
            }
        }

        private void SendMessage()
        {
            string message = InputTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(message))
            {
                // Replace #:selection: with the current selection
                if (message.Contains("#:selection:"))
                {
                    ThreadHelper.ThrowIfNotOnUIThread();

                    TextSelection selection = _dte.ActiveDocument?.Selection as TextSelection;
                    if (selection != null && !selection.IsEmpty)
                    {
                        string selectedText = selection.Text;
                        message = message.Replace("#:selection:", selectedText);
                    }
                }

                // Handle filename hashtags
                var files = GetAllFilesInSolution();
                foreach (var file in files)
                {
                    string fileName = $"#{Path.GetFileName(file)}";
                    if (message.Contains(fileName))
                    {
                        ThreadHelper.ThrowIfNotOnUIThread();
                        ProjectItem projectItem = _dte.Solution.FindProjectItem(file);
                        if (projectItem != null)
                        {
                            EnvDTE.Window window = projectItem.Open();
                            if (window != null)
                            {
                                TextDocument textDoc = window.Document.Object("TextDocument") as TextDocument;
                                if (textDoc != null)
                                {
                                    string fileContent = textDoc.StartPoint.CreateEditPoint().GetText(textDoc.EndPoint);
                                    string backticks = new string('`', 3);
                                    string replacement = $"\n{backticks}\n{fileContent}\n{backticks}\n";
                                    message = message.Replace(fileName, replacement);
                                }
                                window.Close();
                            }
                        }
                    }
                }

                ChatHistoryTextBox.Clear();// AppendText($"You: {message}\n");
                InputTextBox.Clear();

                // Send message through named pipe
                VSIXTestPackage.Instance.SendMessageThroughPipe(message);
            }
        }
        public void ReceiveMessage(string message)
        {
            ChatHistoryTextBox.AppendText($"{message}\n");
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }
    }
}