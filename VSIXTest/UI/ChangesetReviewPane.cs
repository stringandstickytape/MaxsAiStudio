using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows;
using System.Text;
using VSIXTest;
using Newtonsoft.Json;
using EnvDTE80;
using System.IO;
using System.Linq;
using System.Windows.Media;
using static VSIXTest.ChangesetManager;
using System.Windows.Shapes;

[Guid("743967b7-4ad8-4103-8a28-bf2933a5bdf6")]


public class ChangesetReviewPane : ToolWindowPane
{
    private readonly DTE2 _dte;
    private List<ChangeItem> _changes;
    private int _currentChangeIndex = 0;
    private string _originalContent;

    // UI Elements
    private Grid _mainGrid;
    private ListBox _fileListBox;
    private Button _applySecondaryAiButton;
    private Button _applyAllAiButton;
    private TextBox _changeDetailsTextBox;
    private Button _applyButton;
    private Button _nextButton;
    private Button _undoButton;
    private Button _cancelButton;
    private Label _changeTypeLabel;

    // Events
    public event EventHandler<ChangeAppliedEventArgs> ChangeApplied;
    public event EventHandler<RunMergeEventArgs> RunMerge;

    public ChangesetReviewPane() : base(null)
    {
        Caption = "Changeset Review";
        _dte = (DTE2)ServiceProvider.GlobalProvider.GetService(typeof(SDTE));

        InitializeUIComponents();
        SetupEventHandlers();
    }

    private void InitializeUIComponents()
    {
        _mainGrid = new Grid { Margin = new Thickness(10) };
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _fileListBox = new ListBox
        {
            Height = 100,
            Margin = new Thickness(0, 0, 0, 5),
            Background = System.Windows.Media.Brushes.Black,
            Foreground = System.Windows.Media.Brushes.White
        };
        Grid.SetRow(_fileListBox, 0);
        _mainGrid.Children.Add(_fileListBox);

        _applySecondaryAiButton = new Button
        {
            Content = "Apply For This File Via Secondary AI",
            Height = 25,
            Margin = new Thickness(0, 0, 0, 5)
        };
        Grid.SetRow(_applySecondaryAiButton, 1);
        _mainGrid.Children.Add(_applySecondaryAiButton);

        _applyAllAiButton = new Button
        {
            Content = "Apply For All Files Via Secondary AI",
            Height = 25,
            Margin = new Thickness(0, 0, 0, 5)
        };
        Grid.SetRow(_applyAllAiButton, 2);
        _mainGrid.Children.Add(_applyAllAiButton);

        var separator = new Separator { Margin = new Thickness(0, 5, 0, 5) };
        Grid.SetRow(separator, 2);
        _mainGrid.Children.Add(separator);

        _changeTypeLabel = new Label
        {
            Margin = new Thickness(0, 0, 0, 5),
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White
        };
        Grid.SetRow(_changeTypeLabel, 3);
        _mainGrid.Children.Add(_changeTypeLabel);

_changeDetailsTextBox = new TextBox
{
    IsReadOnly = true,
    TextWrapping = TextWrapping.NoWrap,
    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
    Margin = new Thickness(0, 0, 0, 10),
    Padding = new Thickness(5),
    Background = System.Windows.Media.Brushes.Black,
    Foreground = System.Windows.Media.Brushes.White,
    FontFamily = new FontFamily("Consolas"),
    FontSize = 12
};
        Grid.SetRow(_changeDetailsTextBox, 4);
        _mainGrid.Children.Add(_changeDetailsTextBox);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0)
        };
        Grid.SetRow(buttonPanel, 5);
        _mainGrid.Children.Add(buttonPanel);

        _applyButton = new Button
        {
            Content = "Apply",
            Width = 75,
            Height = 25,
            Margin = new Thickness(5, 0, 0, 0)
        };
        buttonPanel.Children.Add(_applyButton);

        _undoButton = new Button
        {
            Content = "Undo",
            Width = 75,
            Height = 25,
            Margin = new Thickness(5, 0, 0, 0),
            IsEnabled = false
        };
        buttonPanel.Children.Add(_undoButton);

        _nextButton = new Button
        {
            Content = "Next",
            Width = 75,
            Height = 25,
            Margin = new Thickness(5, 0, 0, 0)
        };
        buttonPanel.Children.Add(_nextButton);

        _cancelButton = new Button
        {
            Content = "Cancel",
            Width = 75,
            Height = 25,
            Margin = new Thickness(5, 0, 0, 0)
        };
        buttonPanel.Children.Add(_cancelButton);

        Content = _mainGrid;
    }

    private void SetupEventHandlers()
    {
        _applySecondaryAiButton.Click += ApplySecondaryAiButton_Click;
        _applyAllAiButton.Click += ApplyAllAiButton_Click;
        _applyButton.Click += ApplyButton_Click;
        _undoButton.Click += UndoButton_Click;
        _nextButton.Click += NextButton_Click;
        _cancelButton.Click += CancelButton_Click;
    }

    private async void ApplyAllAiButton_Click(object sender, RoutedEventArgs e)
    {
       //try
       //{
       //    var mergeableChanges = _changes.Where(c => c.change_type == "modifyFile" || c.change_type == "addToFile" || c.change_type == "deleteFromFile");
       //    var unmergeableChanges = _changes.Where(c => c.change_type != "modifyFile" && c.change_type != "addToFile" && c.change_type != "deleteFromFile");
       //
       //    
       //    foreach (var item in _fileListBox.Items)
       //    {
       //        var fileName = item as string;
       //        if (!string.IsNullOrEmpty(fileName))
       //        {
       //            var mergeableChangesForFile = mergeableChanges.Where(c => c.p == fileName).ToList();
       //
       //            /*                     case "createnewFile":
       //            case "addToFile":
       //            case "deleteFromFile":
       //            case "modifyFile": */
       //
       //            if (mergeableChangesForFile.Any())
       //            {
       //                RunMerge?.Invoke(this, new RunMergeEventArgs(mergeableChangesForFile));
       //            }
       //
       //
       //        }
       //    }
       //
       //    foreach (var item in unmergeableChanges)
       //    {
       //        if(item.ChangeType == "createnewFile")
       //        {
       //            await ChangesetManager.HandleCreateNewFileAsync(_dte, item);
       //        } else await ChangesetManager.HandleModifyFileAsync(_dte, item);
       //    }
       //}
       //
       //catch (Exception ex)
       //{
       //    HandleError("Error applying secondary AI changes", ex);
       //}
       //
       //MessageBox.Show("Merge completed...");
    }

    private void ApplySecondaryAiButton_Click(object sender, RoutedEventArgs e)
    {
       //try
       //{
       //    var fileName = _fileListBox.SelectedItem as string;
       //    if (!string.IsNullOrEmpty(fileName))
       //    {
       //        var changesForFile = _changes.Where(c => c.Path == fileName).ToList();
       //        if (changesForFile.Any())
       //        {
       //            RunMerge?.Invoke(this, new RunMergeEventArgs(changesForFile));
       //        }
       //    }
       //}
       //catch (Exception ex)
       //{
       //    HandleError("Error applying secondary AI changes", ex);
       //}
    }

    public void Initialize(List<ChangeItem> changes)
    {
        _changes = changes ?? throw new ArgumentNullException(nameof(changes));
        _currentChangeIndex = 0;
        PopulateFileListBox();
        ShowNextChange();
    }

    private void PopulateFileListBox()
    {
        _fileListBox.Items.Clear();
        // Get all unique paths from FileChanges containing these changes
        var paths = new HashSet<string>();
        
        foreach (var change in _changes)
        {
            // Find file path for the change by looking at the current changeset
            var anyFileChange = VSIXTestPackage.Instance.FindFilePathForChange(change);
            if (!string.IsNullOrEmpty(anyFileChange))
            {
                paths.Add(anyFileChange);
            }
        }
        
        foreach (var fileName in paths)
        {
            _fileListBox.Items.Add(fileName);
        }
    }

    private void ShowNextChange()
    {
        try
        {
            if (_currentChangeIndex < _changes.Count)
            {
                var change = _changes[_currentChangeIndex];
                string filePath = VSIXTestPackage.Instance.FindFilePathForChange(change);

                if (filePath.Substring(1, 1) != ":")
                {
                    //await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    string solutionDir = System.IO.Path.GetDirectoryName(_dte.Solution.FullName);
                    filePath = System.IO.Path.Combine(solutionDir, filePath.TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar));
                }



                _changeTypeLabel.Content = $"Change Type: {FormatChangeType(change.change_type)}";
                _applyButton.IsEnabled = true;
                _undoButton.IsEnabled = false;
// Format the change details with better formatting
_changeDetailsTextBox.Text = FormatChangeDetails(change, filePath);

                if (change.change_type != "createnewFile" && !string.IsNullOrEmpty(filePath))
                    JumpToChange(filePath);
            }
            else
            {
                CloseToolWindow();
            }
        }
        catch (Exception ex)
        {
            HandleError("Error showing next change", ex);
        }
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var change = _changes[_currentChangeIndex];
            string filePath = VSIXTestPackage.Instance.FindFilePathForChange(change);
            
            if (change.change_type != "createnewFile" && !string.IsNullOrEmpty(filePath))
            {
                var window = _dte.ItemOperations.OpenFile(filePath);
                var textDocument = window.Document.Object() as EnvDTE.TextDocument;
                var editPoint = textDocument.StartPoint.CreateEditPoint();
                _originalContent = editPoint.GetText(textDocument.EndPoint);
            }

            ChangeApplied?.Invoke(this, new ChangeAppliedEventArgs(change));
            _applyButton.IsEnabled = false;
            _undoButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            HandleError("Error applying change", ex);
        }
    }


    private void UndoButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var change = _changes[_currentChangeIndex];
            string filePath = VSIXTestPackage.Instance.FindFilePathForChange(change);
            
            if (change.change_type != "createnewFile" && !string.IsNullOrEmpty(filePath))
            {
                UndoChange(filePath);
            }
            else if (change.change_type == "createnewFile" && !string.IsNullOrEmpty(filePath))
            {
                DeleteNewFile(filePath);
            }
            _applyButton.IsEnabled = true;
            _undoButton.IsEnabled = false;
        }
        catch (Exception ex)
        {
            HandleError("Error undoing change", ex);
        }
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _currentChangeIndex++;
            ShowNextChange();
        }
        catch (Exception ex)
        {
            HandleError("Error moving to next change", ex);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        CloseToolWindow();
    }

    private void JumpToChange(string filePath)
    {
        if (_currentChangeIndex < _changes.Count)
        {
            var change = _changes[_currentChangeIndex];

            if (_dte != null && !string.IsNullOrEmpty(filePath))
            {
                try
                {
                    _dte.ItemOperations.OpenFile(filePath, EnvDTE.Constants.vsViewKindCode);
                    var selection = _dte.ActiveDocument.Selection as EnvDTE.TextSelection;
                    selection.GotoLine(change.lineNumber, true);
                }
                catch (Exception ex)
                {
                    HandleError($"Error opening file: {ex.Message}", ex);
                }
            }
        }
    }
    private void UndoChange(string filePath)
    {
        var window = _dte.ItemOperations.OpenFile(filePath);
        var textDocument = window.Document.Object() as EnvDTE.TextDocument;
        var editPoint = textDocument.StartPoint.CreateEditPoint();
        editPoint.Delete(textDocument.EndPoint);
        editPoint.Insert(_originalContent);
        JumpToChange(filePath);
    }

    /// <summary>
    /// Helper method to apply text replacements using the shared TextReplacer
    /// </summary>
    private string ApplyTextReplacement(string sourceText, string oldText, string newText, int lineNumber)
    {
        return new TextReplacer().ReplaceTextAtHint(sourceText, oldText, newText, lineNumber);
    }

    private void DeleteNewFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                // Close any open instances of the file
                foreach (EnvDTE.Document doc in _dte.Documents)
                {
                    if (doc.FullName.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                    {
                        doc.Close(EnvDTE.vsSaveChanges.vsSaveChangesNo);
                        break;
                    }
                }
                
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                HandleError($"Error deleting file: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Format the change type to be more user-friendly
    /// </summary>
    private string FormatChangeType(string changeType)
    {
        switch (changeType.ToLower())
        {
            case "createNewFile":
            case "createnewfile":
                return "Create New File";
            case "addtofile":
                return "Add To File";
            case "deletefromfile":
                return "Delete From File";
            case "modifyfile":
                return "Modify File";
            case "replacefile":
                return "Replace File";
            case "renamefile":
                return "Rename File";
            case "deletefile":
                return "Delete File";
            default:
                return changeType;
        }
    }

    /// <summary>
    /// Format the change details with better readability
    /// </summary>
    private string FormatChangeDetails(ChangeItem change, string filePath)
    {
        var sb = new System.Text.StringBuilder();
        
        // File information section
        sb.AppendLine($"▶ FILE INFORMATION:");
        sb.AppendLine($"  Path: {filePath}");
        if (change.lineNumber > 0)
            sb.AppendLine($"  Line Number: {change.lineNumber}");
        sb.AppendLine();
        
        // Description section
        sb.AppendLine($"▶ DESCRIPTION:");
        sb.AppendLine($"  {(string.IsNullOrEmpty(change.description) ? "No description provided" : change.description)}");
        sb.AppendLine();
        
        // Content sections
        if (!string.IsNullOrEmpty(change.oldContent))
        {
            sb.AppendLine($"▶ ORIGINAL CONTENT:");
            sb.AppendLine(FormatCode(change.oldContent));
            sb.AppendLine();
        }
        
        if (!string.IsNullOrEmpty(change.newContent))
        {
            sb.AppendLine($"▶ NEW CONTENT:");
            sb.AppendLine(FormatCode(change.newContent));
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Format code with proper indentation for display
    /// </summary>
    private string FormatCode(string code)
    {
        if (string.IsNullOrEmpty(code))
            return string.Empty;
            
        var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new System.Text.StringBuilder();
        
        foreach (var line in lines)
        {
            sb.AppendLine($"    {line}");
        }
        
        return sb.ToString();
    }
    
    private void CloseToolWindow()
    {
        IVsWindowFrame frame = (IVsWindowFrame)Frame;
        frame?.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
    }

    private void HandleError(string message, Exception ex)
    {
        MessageBox.Show($"{message}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    internal void MergeCompleted(string content, string filename)
    {
        try
        {
            // Find the filename in _fileListBox
            var listBoxItem = _fileListBox.Items.Cast<object>()
                .FirstOrDefault(item => item.ToString() == filename);

            if (listBoxItem != null)
            {
                int index = _fileListBox.Items.IndexOf(listBoxItem);
                if (index >= 0)
                {
                    // Get the ListBoxItem container
                    var container = (ListBoxItem)_fileListBox.ItemContainerGenerator
                        .ContainerFromIndex(index);

                    if (container != null)
                    {
                        container.Background = Brushes.Red;
                        container.Foreground = Brushes.White;
                    }
                }
            }

            var window = _dte.ItemOperations.OpenFile(filename);
            var textDocument = window.Document.Object() as EnvDTE.TextDocument;
            var editPoint = textDocument.StartPoint.CreateEditPoint();
            editPoint.Delete(textDocument.EndPoint);

            // Use our shared TextReplacer for content processing
            string[] lines = content.Split('\n');
            string result = string.Join("\n", lines.Skip(1).Take(lines.Length - 2));

            editPoint.Insert(result);
        }
        catch (Exception e)
        {
            HandleError($"Merge failed: {e}", e);
        }
    }
}



public class ChangeAppliedEventArgs : EventArgs
{
    public ChangeItem Change { get; set; }
    public ChangeAppliedEventArgs(ChangeItem change)
    {
        Change = change;
    }
}

public class RunMergeEventArgs : EventArgs
{
    public List<ChangeItem> Changes { get; set; }
    public RunMergeEventArgs(List<ChangeItem> changes)
    {
        Changes = changes;
    }
}