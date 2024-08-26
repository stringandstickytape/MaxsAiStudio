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
            if (InputTextBox.Text.EndsWith("#"))
            {
                PopulateAndShowShortcutMenu();
            }
        }

        private void PopulateAndShowShortcutMenu()
        {
            ShortcutMenu.Items.Clear();
            var files = GetAllFilesInSolution();
            foreach (var file in files)
            {
                var menuItem = new MenuItem
                {
                    Header = $"#{Path.GetFileName(file)}",
                    Tag = file
                };
                menuItem.Click += ShortcutMenuItem_Click;
                ShortcutMenu.Items.Add(menuItem);
            }

            var textBoxPosition = InputTextBox.GetRectFromCharacterIndex(InputTextBox.CaretIndex);
            ShortcutMenu.PlacementTarget = InputTextBox;
            ShortcutMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.RelativePoint;
            ShortcutMenu.HorizontalOffset = textBoxPosition.Right;
            ShortcutMenu.VerticalOffset = textBoxPosition.Bottom;
            ShortcutMenu.IsOpen = true;
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
                int caretIndex = InputTextBox.CaretIndex;
                string insertText = menuItem.Header.ToString();
                InputTextBox.Text = InputTextBox.Text.Insert(caretIndex, insertText);
                InputTextBox.CaretIndex = caretIndex + insertText.Length;
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