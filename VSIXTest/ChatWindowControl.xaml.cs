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
        }

        private void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
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

        private void PopulateAndShowShortcutMenu()
        {
            ShortcutMenu.Items.Clear();
            string currentToken = GetCurrentHashtagToken().ToLower();
            var files = GetAllFilesInSolution();

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file).ToLower();
                if (fileName.Contains(currentToken))
                {
                    var menuItem = new MenuItem
                    {
                        Header = $"#{Path.GetFileName(file)}",
                        Tag = file
                    };
                    menuItem.Click += ShortcutMenuItem_Click;
                    ShortcutMenu.Items.Add(menuItem);
                }
            }

            if (ShortcutMenu.Items.Count > 0)
            {
                var textBoxPosition = InputTextBox.GetRectFromCharacterIndex(InputTextBox.CaretIndex);
                ShortcutMenu.PlacementTarget = InputTextBox;
                ShortcutMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.RelativePoint;
                ShortcutMenu.HorizontalOffset = textBoxPosition.Right;
                ShortcutMenu.VerticalOffset = textBoxPosition.Bottom;
                ShortcutMenu.IsOpen = true;
            }
            else
            {
                ShortcutMenu.IsOpen = false;
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

            if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
            {
                foreach (ProjectItem item in project.ProjectItems)
                {
                    if (item.SubProject != null)
                    {
                        GetProjectFiles(item.SubProject, files);
                    }
                }
            }
            else
            {
                foreach (ProjectItem item in project.ProjectItems)
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
                        catch (Exception)
                        {
                            // Handle or log any exceptions
                        }
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

                ShortcutMenu.IsOpen = false;
            }
        }

        private void SendMessage()
        {
            string message = InputTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(message))
            {
                ChatHistoryTextBox.AppendText($"You: {message}\n");
                InputTextBox.Clear();

                // Send message through named pipe
                VSIXTestPackage.Instance.SendMessageThroughPipe(message);
            }
        }

        public void ReceiveMessage(string message)
        {
            ChatHistoryTextBox.AppendText($"AI: {message}\n");
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }
    }
}