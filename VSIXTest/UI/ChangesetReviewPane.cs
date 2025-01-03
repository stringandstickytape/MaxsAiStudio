using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows;
using VSIXTest;

[Guid("743967b7-4ad8-4103-8a28-bf2933a5bdf6")]
public class ChangesetReviewPane : ToolWindowPane
{ 
    private Grid _mainGrid;
    private TextBox _changeDetailsTextBox;
    private Button _applyButton;
    private Button _skipButton;
    private Button _cancelButton;
    private Label _changeTypeLabel;
    private List<Change> _changes;
    private int _currentChangeIndex = 0;

    public event EventHandler<ChangeAppliedEventArgs> ChangeApplied;

    public ChangesetReviewPane() : base(null)
    {
        this.Caption = "Changeset Review";
        InitializeContent();
    }

    private void InitializeContent()
    {
        _mainGrid = new Grid
        {
            Margin = new Thickness(10)
        };

        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _changeTypeLabel = new Label
        {
            Margin = new Thickness(0, 0, 0, 5),
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White
        };
        Grid.SetRow(_changeTypeLabel, 0);
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
        Grid.SetRow(_changeDetailsTextBox, 1);
        _mainGrid.Children.Add(_changeDetailsTextBox);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0)
        };
        Grid.SetRow(buttonPanel, 2);
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

        _skipButton = new Button
        {
            Content = "Skip",
            Width = 75,
            Height = 25,
            Margin = new Thickness(5, 0, 0, 0)
        };
        _skipButton.Click += SkipButton_Click;
        buttonPanel.Children.Add(_skipButton);

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

    public void Initialize(List<Change> changes)
    {
        _changes = changes;
        _currentChangeIndex = 0;
        ShowNextChange();
    }

    private void ShowNextChange()
    {
        if (_currentChangeIndex < _changes.Count)
        {
            var change = _changes[_currentChangeIndex];
            _changeTypeLabel.Content = $"Change Type: {change.ChangeType}";
            _changeDetailsTextBox.Text =
                $"Path: {change.Path}\n" +
                $"Line Number: {change.LineNumber}\n" +
                $"Old Content:\n{(string.IsNullOrEmpty(change.OldContent) ? "" : change.OldContent)}\n" +
                $"New Content:\n{(string.IsNullOrEmpty(change.NewContent) ? "" : change.NewContent)}";
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
        ChangeApplied?.Invoke(this, new ChangeAppliedEventArgs(_changes[_currentChangeIndex]));
        _currentChangeIndex++;
        ShowNextChange();
    }

    private void SkipButton_Click(object sender, RoutedEventArgs e)
    {
        _currentChangeIndex++;
        ShowNextChange();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        IVsWindowFrame frame = (IVsWindowFrame)this.Frame;
        frame?.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
    }
}