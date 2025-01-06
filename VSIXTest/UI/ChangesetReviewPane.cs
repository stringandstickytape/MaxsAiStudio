using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows;
using VSIXTest;
using Newtonsoft.Json;
using EnvDTE80;
using System.IO;
using System.Linq;
using SharedClasses.Models;

[Guid("743967b7-4ad8-4103-8a28-bf2933a5bdf6")]


public class ChangesetReviewPane : ToolWindowPane
{ 
    private Grid _mainGrid;
    private ListBox _fileListBox;
    private Button _applySecondaryAiButton;
    private TextBox _changeDetailsTextBox;
    private Button _applyButton;
    private Button _nextBUtton;
    private Button _undoButton;
    private Button _cancelButton;
    private Label _changeTypeLabel;
    private List<Change> _changes;
    private int _currentChangeIndex = 0;
    private DTE2 _dte;
    private string _originalContent;
    public event EventHandler<ChangeAppliedEventArgs> ChangeApplied;
    public event EventHandler<RunMergeEventArgs> RunMerge;

    public ChangesetReviewPane() : base(null)
    {
        this.Caption = "Changeset Review";
        _dte = ServiceProvider.GlobalProvider.GetService(typeof(SDTE)) as DTE2;
        InitializeContent();
    }

    private void InitializeContent()
    {
        _mainGrid = new Grid
        {
            Margin = new Thickness(10)
        };

        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // File list
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Secondary AI button
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Separator
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Change type label
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Details
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons

        // File ListBox
        _fileListBox = new ListBox
        {
            Height = 100,
            Margin = new Thickness(0, 0, 0, 5),
            Background = System.Windows.Media.Brushes.Black,
            Foreground = System.Windows.Media.Brushes.White
        };
        Grid.SetRow(_fileListBox, 0);
        _mainGrid.Children.Add(_fileListBox);

        // Secondary AI Button
        _applySecondaryAiButton = new Button
        {
            Content = "Apply For This File Via Secondary AI",
            Height = 25,
            Margin = new Thickness(0, 0, 0, 5)
        };
        _applySecondaryAiButton.Click += ApplySecondaryAiButton_Click;
        Grid.SetRow(_applySecondaryAiButton, 1);
        _mainGrid.Children.Add(_applySecondaryAiButton);

        // Separator
        var separator = new Separator
        {
            Margin = new Thickness(0, 5, 0, 5)
        };
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
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0, 0, 0, 10),
            Padding = new Thickness(5),
            Background = System.Windows.Media.Brushes.Black,
            Foreground = System.Windows.Media.Brushes.White
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
        _applyButton.Click += ApplyButton_Click;
        buttonPanel.Children.Add(_applyButton);

        _undoButton = new Button
        {
            Content = "Undo",
            Width = 75,
            Height = 25,
            Margin = new Thickness(5, 0, 0, 0),
            IsEnabled = false
        };
        _undoButton.Click += UndoButton_Click;
        buttonPanel.Children.Add(_undoButton);

        _nextBUtton = new Button
        {
            Content = "Next",
            Width = 75,
            Height = 25,
            Margin = new Thickness(5, 0, 0, 0)
        };
        _nextBUtton.Click += NextButton_Click;
        buttonPanel.Children.Add(_nextBUtton);

        _cancelButton = new Button
        {
            Content = "Cancel",
            Width = 75,
            Height = 25,
            Margin = new Thickness(5, 0, 0, 0)
        };
        _cancelButton.Click += CancelButton_Click;
        buttonPanel.Children.Add(_cancelButton);

        this.Content = _mainGrid;
    }

    private void ApplySecondaryAiButton_Click(object sender, RoutedEventArgs e)
    {
        // get the selected filename
        var fileName = _fileListBox.SelectedItem as string;
        if (fileName != null)
        {
            // get the changes for the selected filename
            var changesForFile = _changes.Where(c => c.Path == fileName).ToList();
            if (changesForFile.Count > 0)
            {
                RunMerge?.Invoke(this, new RunMergeEventArgs(changesForFile));
            }
        }
    }

    public void Initialize(List<Change> changes)
    {
        _changes = changes;
        _currentChangeIndex = 0;

        // populate the listbox with the distinct filenames of the changes
        _fileListBox.Items.Clear();
        var fileNames = changes.Select(c => c.Path).Distinct();
        foreach (var fileName in fileNames)
        {
            _fileListBox.Items.Add(fileName);
        }

        ShowNextChange();
    }

    private void ShowNextChange()
    {
        if (_currentChangeIndex < _changes.Count)
        {
            var change = _changes[_currentChangeIndex];
            _changeTypeLabel.Content = $"Change Type: {change.ChangeType}";
            _applyButton.IsEnabled = true;
            _undoButton.IsEnabled = false;
            _changeDetailsTextBox.Text =
                $"Path: {change.Path}\n" +
                $"Line Number: {change.LineNumber}\n" +
                $"Old Content:\n{(string.IsNullOrEmpty(change.OldContent) ? "" : change.OldContent)}\n" +
                $"New Content:\n{(string.IsNullOrEmpty(change.NewContent) ? "" : change.NewContent)}";

            if(change.ChangeType != "createnewFile")
                JumpToChange();
        }
        else
        {
            // Close the tool window when all changes are processed
            IVsWindowFrame frame = (IVsWindowFrame)this.Frame;
            frame?.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
        }
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        var change = _changes[_currentChangeIndex];
        if (change.ChangeType != "createnewFile")
        {
            var window = _dte.ItemOperations.OpenFile(change.Path);
            var textDocument = window.Document.Object() as EnvDTE.TextDocument;
            var editPoint = textDocument.StartPoint.CreateEditPoint();
            _originalContent = editPoint.GetText(textDocument.EndPoint);
        }
        
        ChangeApplied?.Invoke(this, new ChangeAppliedEventArgs(change));
        _applyButton.IsEnabled = false;
        _undoButton.IsEnabled = true;
    }


    private void UndoButton_Click(object sender, RoutedEventArgs e)
    {
        var change = _changes[_currentChangeIndex];
        if (change.ChangeType != "createnewFile")
        {
            try
            {
                var window = _dte.ItemOperations.OpenFile(change.Path);
                var textDocument = window.Document.Object() as EnvDTE.TextDocument;
                var editPoint = textDocument.StartPoint.CreateEditPoint();
                editPoint.Delete(textDocument.EndPoint);
                editPoint.Insert(_originalContent);

                JumpToChange();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error undoing change: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            // For new files, delete the file if it exists
            if (File.Exists(change.Path))
            {
                try
                {
                    //File.Delete(change.Path);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show($"Error deleting file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        _applyButton.IsEnabled = true;
        _undoButton.IsEnabled = false;
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        _currentChangeIndex++;
        ShowNextChange();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        IVsWindowFrame frame = (IVsWindowFrame)this.Frame;
        frame?.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
    }

    private void JumpToChange()
    {
        if (_currentChangeIndex < _changes.Count)
        {
            var change = _changes[_currentChangeIndex];

            //var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            if (_dte != null)
            {
                try
                {
                    _dte.ItemOperations.OpenFile(change.Path);
                    var selection =_dte.ActiveDocument.Selection as EnvDTE.TextSelection;
                    selection.GotoLine(change.LineNumber, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    internal void MergeCompleted(string content)
    {
        try
        {
            var fileName = _fileListBox.SelectedItem as string;
            var window = _dte.ItemOperations.OpenFile(fileName);
            var textDocument = window.Document.Object() as EnvDTE.TextDocument;
            var editPoint = textDocument.StartPoint.CreateEditPoint();
            editPoint.Delete(textDocument.EndPoint);
            editPoint.Insert(content);
        }
        catch(Exception e)
        {
            MessageBox.Show($"Merge failed: {e}");
        }
    }
}




public class ChangeAppliedEventArgs : EventArgs
{
    public Change Change { get; set; }
    public ChangeAppliedEventArgs(Change change)
    {
        Change = change;
    }
}

public class RunMergeEventArgs : EventArgs
{
    public List<Change> Changes { get; set; }
    public RunMergeEventArgs(List<Change> changes)
    {
        Changes = changes;
    }
}