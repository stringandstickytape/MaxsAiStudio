using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Threading.Tasks;

namespace VSIXTest
{
    public class Change
    {
        [JsonProperty("change_type")]
        public string ChangeType { get; set; }
        public string Path { get; set; }
        public int LineNumber { get; set; }
        public string OldContent { get; set; }
        public string NewContent { get; set; }
    }

    public class Changeset
    {
        public List<Change> Changes { get; set; }
    }

    
    public class ChangeAppliedEventArgs : EventArgs
    {
        public Change Change { get; set; }
        public ChangeAppliedEventArgs(Change change)
        {
            Change = change;
        }
    }

    public class ChangesetReviewWindow : Window
    {
        public event EventHandler<ChangeAppliedEventArgs> ChangeApplied;

        private readonly List<Change> _changes;
        private int _currentChangeIndex = 0;
        private Label _changeTypeLabel;
        private TextBox _changeDetailsTextBox;
        private Button _openFileButton;
        private Button _applyButton;
        private Button _skipButton;
        private Button _cancelButton;
        private System.Windows.Media.Brush _backgroundBrush;

        public ChangesetReviewWindow(List<Change> changes)
        {
            _changes = changes;

            // Ensure we're on the UI thread
            //ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                InitializeWindow();
                InitializeComponent();
                _ = ShowNextChangeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }
        private System.Windows.Threading.DispatcherTimer _topMostTimer;
        private void InitializeWindow(){
            _backgroundBrush = (System.Windows.Media.Brush)Application.Current.TryFindResource("ButtonBackgroundBrush");
            Title = "Review Changes";
            Width = 500;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = System.Windows.Media.Brushes.Transparent;

            Topmost = true;

            // Initialize and start the timer
            _topMostTimer = new System.Windows.Threading.DispatcherTimer();
            _topMostTimer.Tick += (s, e) =>
            {
                if (!IsActive)
                {
                    Topmost = false;
                    Topmost = true;
                    Activate();
                }
            };
            _topMostTimer.Interval = TimeSpan.FromMilliseconds(100);
            _topMostTimer.Start();

            // Stop the timer when the window closes
            Closed += (s, e) =>
            {
                _topMostTimer.Stop();
            };
        }

        private void InitializeComponent()
        {
            var grid = new Grid
            {
                Margin = new Thickness(10), Background = System.Windows.Media.Brushes.Transparent
            };
            Content = grid;

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _changeTypeLabel = new Label
            {
                Margin = new Thickness(0, 0, 0, 5),
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White
            };
            Grid.SetRow(_changeTypeLabel, 0);
            grid.Children.Add(_changeTypeLabel);

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
            grid.Children.Add(_changeDetailsTextBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };
            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            _openFileButton = new Button
            {
                Content = "Open File",
                Width = 75,
                Height = 25,
                Margin = new Thickness(5, 0, 0, 0)
            };
            _openFileButton.Click += OpenFileButton_Click;
            buttonPanel.Children.Add(_openFileButton);

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
        }

        private async Task ShowNextChangeAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (_currentChangeIndex < _changes.Count)
                {
                    var change = _changes[_currentChangeIndex];
                    _changeTypeLabel.Content = $"Change Type: {change.ChangeType}";
                    _changeDetailsTextBox.Text =
                        $"Path: {change.Path}\n" +
                        $"Line Number: {change.LineNumber}\n" +
                        $"Old Content:\n{(string.IsNullOrEmpty(change.OldContent) ? "" : change.OldContent)}\n" +
                        $"New Content:\n{(string.IsNullOrEmpty(change.NewContent) ? "" : change.NewContent)}";

                    // Add these lines
                    Topmost = true;  // Makes the window appear on top
                    Activate();      // Activates the window
                    Topmost = false; // Removes the always-on-top behavior after activation
                    
                }
                else
                {
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing change: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                ChangeApplied?.Invoke(this, new ChangeAppliedEventArgs(_changes[_currentChangeIndex]));
                _currentChangeIndex++;
                await ShowNextChangeAsync();
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying change 1: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                _currentChangeIndex++;
                await ShowNextChangeAsync();
                BringIntoView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error skipping change: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error canceling: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (_currentChangeIndex < _changes.Count)
                {
                    var change = _changes[_currentChangeIndex];
                    var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                    if (dte != null)
                    {
                        try
                        {
                            dte.ItemOperations.OpenFile(change.Path);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}