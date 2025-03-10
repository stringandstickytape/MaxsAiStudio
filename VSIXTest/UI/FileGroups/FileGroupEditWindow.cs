﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.IO;
using VSIXTest.FileGroups;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE;

namespace VSIXTest
{
    public class FileGroupEditWindow : System.Windows.Window
    {
        private List<FileGroup> _fileGroups;
        private List<string> _availableFiles;
        private ListBox _groupListBox;
        private TextBox _nameTextBox;
        private TreeView _fileTreeView;
        private Dictionary<Guid, List<string>> _editedGroups;
        private Guid? previousGroupId = null;
        private string _commonPath;
        private string _solutionName;
        private readonly DTE2 _dte;
        public List<FileGroup> EditedFileGroups { get; private set; }

        public FileGroupEditWindow(List<FileGroup> fileGroups, List<string> availableFiles)
        {
            _fileGroups = new List<FileGroup>(fileGroups);
            _availableFiles = availableFiles;
            _dte = (DTE2)ServiceProvider.GlobalProvider.GetService(typeof(SDTE));
            _commonPath = FindCommonPath(_availableFiles);
            _solutionName = SolutionInfo.GetCurrentSolutionPath(_dte);
            _editedGroups = new Dictionary<Guid, List<string>>();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
           Title = "Edit File Groups";
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            FontSize = 12;
;
            Width = 700;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

           var grid = new Grid { Margin = new Thickness(10) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });

;
            Content = grid;

            //grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
           grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Group ListBox
           _groupListBox = new ListBox { Margin = new Thickness(5), Background = System.Windows.Media.Brushes.White };

            _groupListBox.ItemTemplate = CreateGroupItemTemplate();
            PopulateGroupListBox();
            InitializeEditedGroups();
            _groupListBox.SelectionChanged += GroupListBox_SelectionChanged;
            Grid.SetRowSpan(_groupListBox, 2);
            grid.Children.Add(_groupListBox);

            // Add and Delete buttons for groups
          var groupButtonPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
         var addGroupButton = new Button { Content = "+", Width = 30, Margin = new Thickness(5), Padding = new Thickness(5), ToolTip = "Add a new file group", FontWeight = FontWeights.Bold };

         var deleteGroupButton = new Button { Content = "-", Width = 30, Margin = new Thickness(5), Padding = new Thickness(5), ToolTip = "Delete the selected file group", FontWeight = FontWeights.Bold };

           addGroupButton.Click += AddGroupButton_Click;
           deleteGroupButton.Click += DeleteGroupButton_Click;
           groupButtonPanel.Children.Add(addGroupButton);
           groupButtonPanel.Children.Add(deleteGroupButton);
           Grid.SetRow(groupButtonPanel, 2);
           grid.Children.Add(groupButtonPanel);

            // Name input
             var nameLabel = new Label { Content = "Group Name:", FontWeight = FontWeights.Bold };
         _nameTextBox = new TextBox { Margin = new Thickness(5), Padding = new Thickness(5), VerticalContentAlignment = VerticalAlignment.Center };
            _nameTextBox.TextChanged += NameTextBox_TextChanged;
            var namePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
            namePanel.Children.Add(nameLabel);
          namePanel.Children.Add(_nameTextBox);
            Grid.SetRow(namePanel, 0);
            Grid.SetColumn(namePanel, 1);
            grid.Children.Add(namePanel);

            // File TreeView
           _fileTreeView = new TreeView { Margin = new Thickness(5), Background = System.Windows.Media.Brushes.White };
            Grid.SetRow(_fileTreeView, 1);
            Grid.SetColumn(_fileTreeView, 1);
            grid.Children.Add(_fileTreeView);

            // Buttons
           var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(10) };;
          var saveButton = new Button { Content = "Save", Width = 80, Margin = new Thickness(5), Padding = new Thickness(5), ToolTip = "Save all changes" };

          var cancelButton = new Button { Content = "Cancel", Width = 80, Margin = new Thickness(5), Padding = new Thickness(5), ToolTip = "Cancel and close the window" };

            saveButton.Click += SaveButton_Click;
            cancelButton.Click += CancelButton_Click;
            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);
            Grid.SetColumn(buttonPanel, 1);
            grid.Children.Add(buttonPanel);
        }

        private DataTemplate CreateGroupItemTemplate()
        {
            var template = new DataTemplate();
            var stackPanel = new FrameworkElementFactory(typeof(StackPanel));
            stackPanel.SetValue(StackPanel.BackgroundProperty, System.Windows.Media.Brushes.White);
          stackPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            var checkBox = new FrameworkElementFactory(typeof(CheckBox));
            checkBox.SetBinding(CheckBox.IsCheckedProperty, new Binding("Selected"));
            checkBox.AddHandler(CheckBox.ClickEvent, new RoutedEventHandler(GroupCheckBox_Click));

            var textBlock = new FrameworkElementFactory(typeof(TextBlock));
            textBlock.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            textBlock.SetValue(TextBlock.ForegroundProperty, System.Windows.Media.Brushes.Black);
         textBlock.SetValue(TextBlock.MarginProperty, new Thickness(5, 0, 0, 0));

            stackPanel.AppendChild(checkBox);
            stackPanel.AppendChild(textBlock);

            template.VisualTree = stackPanel;
            return template;
        }

        private void InitializeEditedGroups()
        {
            foreach (var group in _fileGroups)
            {
                _editedGroups[group.Id] = new List<string>(group.FilePaths);
            }
        }

        private void PopulateGroupListBox()
        {
            _groupListBox.ItemsSource = null;
            _groupListBox.ItemsSource = _fileGroups;
        }

        private void GroupCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            var fileGroup = checkBox.DataContext as FileGroup;
            fileGroup.Selected = checkBox.IsChecked ?? false;
            e.Handled = true; // Prevent the selection from changing
        }

        private void GroupListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SavePreviousGroupState();

            var selectedGroup = _groupListBox.SelectedItem as FileGroup;
            if (selectedGroup != null)
            {
                _nameTextBox.Text = selectedGroup.Name;
                PopulateTreeView(selectedGroup.Id);

                previousGroupId = selectedGroup.Id;

                // Add this new code to scroll to the first checked file
                if (_editedGroups[selectedGroup.Id].Any())
                {
                    string firstCheckedFile = _editedGroups[selectedGroup.Id].First();
                    ScrollToCheckedFile(_fileTreeView.Items[0] as TreeViewItem, firstCheckedFile);
                }
            }
        }

        private ScrollViewer FindParentScrollViewer(DependencyObject child)
        {
            DependencyObject parent = child.GetVisualOrLogicalParent();
            while (parent != null && !(parent is ScrollViewer))
            {
                parent = parent.GetVisualOrLogicalParent();
            }
            return parent as ScrollViewer;
        }

        private void ScrollToCheckedFile(TreeViewItem rootNode, string targetFilePath)
        {
            if (rootNode == null) return;

            // Queue to store nodes to process
            var nodesToProcess = new Queue<TreeViewItem>();
            nodesToProcess.Enqueue(rootNode);

            while (nodesToProcess.Count > 0)
            {
                var currentNode = nodesToProcess.Dequeue();

                // Check if this is the file we're looking for
                if (currentNode.Header is CheckBox checkBox &&
                    checkBox.Tag.ToString() == targetFilePath)
                {
                    // Expand all parent nodes
                    var parent = currentNode;
                    while (parent != null)
                    {
                        parent.IsExpanded = true;
                        parent = parent.Parent as TreeViewItem;
                    }

                    // Find the ScrollViewer parent
                    ScrollViewer scrollViewer = FindParentScrollViewer(currentNode);
                    if (scrollViewer != null)
                    {
                        scrollViewer.ScrollToEnd();
                    }


                    // Bring the item into view
                    currentNode.BringIntoView();
                    currentNode.Focus();
                    return;
                }

                // Add all child nodes to the queue
                foreach (TreeViewItem childNode in currentNode.Items)
                {
                    nodesToProcess.Enqueue(childNode);
                }
            }
        }

        private string FindCommonPath(IEnumerable<string> paths)
        {
            if (!paths.Any()) return string.Empty;

            var parts = paths.First().Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var commonPath = new List<string>();

            for (int i = 0; i < parts.Length; i++)
            {
                if (paths.All(p => p.StartsWith(string.Join(Path.DirectorySeparatorChar.ToString(), parts.Take(i + 1)))))
                {
                    commonPath.Add(parts[i]);
                }
                else
                {
                    break;
                }
            }

            return string.Join(Path.DirectorySeparatorChar.ToString(), commonPath);
        }

        private void PopulateTreeView(Guid groupId)
        {
            _fileTreeView.Items.Clear();
            var rootNode = CreateTreeViewItem("Root", isFolder: true);
            _fileTreeView.Items.Add(rootNode);

            var commonPathLength = _commonPath.Length;

            foreach (var file in _availableFiles)
            {
                AddFileToTree(rootNode, file, _editedGroups[groupId].Contains(file), commonPathLength);
            }

            ExpandAllNodes(_fileTreeView);
        }

        private void ExpandAllNodes(TreeView treeView)
        {
            foreach (object item in treeView.Items)
            {
                if (item is TreeViewItem treeViewItem)
                {
                    ExpandTreeViewItem(treeViewItem);
                }
            }
        }

        private void ExpandTreeViewItem(TreeViewItem item)
        {
            item.IsExpanded = true;
            foreach (object childItem in item.Items)
            {
                if (childItem is TreeViewItem childTreeViewItem)
                {
                    ExpandTreeViewItem(childTreeViewItem);
                }
            }
        }

        private void AddFileToTree(TreeViewItem parentNode, string filePath, bool isChecked, int commonPathLength)
        {
            var displayPath = filePath.Substring(commonPathLength).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var parts = displayPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var currentNode = parentNode;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                var folderNode = FindOrCreateChildNode(currentNode, parts[i], isFolder: true);
                currentNode = folderNode;
            }

            var fileName = parts[parts.Length - 1];
            var fileNode = CreateTreeViewItem(fileName, isFolder: false, filePath: filePath, isChecked: isChecked);
            currentNode.Items.Add(fileNode);
        }

        private TreeViewItem FindOrCreateChildNode(TreeViewItem parentNode, string name, bool isFolder)
        {
            foreach (TreeViewItem childNode in parentNode.Items)
            {
                if ((childNode.Header as CheckBox)?.Content.ToString() == name)
                {
                    return childNode;
                }
            }

            var newNode = CreateTreeViewItem(name, isFolder: isFolder);
            parentNode.Items.Add(newNode);
            return newNode;
        }

        private TreeViewItem CreateTreeViewItem(string name, bool isFolder, string filePath = null, bool isChecked = false)
        {
            var checkBox = new CheckBox
            {
                Foreground = System.Windows.Media.Brushes.Black,
                Background = System.Windows.Media.Brushes.White,
                Content = name,
                IsChecked = isChecked,
                Tag = filePath ?? name
            };

            checkBox.Checked += (s, e) => UpdateChildCheckState((s as CheckBox), true);
            checkBox.Unchecked += (s, e) => UpdateChildCheckState((s as CheckBox), false);

            var item = new TreeViewItem { Header = checkBox };

            if (isFolder)
            {
                checkBox.Checked += (s, e) => PropagateCheckState(item, true);
                checkBox.Unchecked += (s, e) => PropagateCheckState(item, false);
            }

            return item;
        }

        private void UpdateChildCheckState(CheckBox parentCheckBox, bool isChecked)
        {
            var parentItem = parentCheckBox.Parent as TreeViewItem;
            if (parentItem != null)
            {
                foreach (TreeViewItem childItem in parentItem.Items)
                {
                    if (childItem.Header is CheckBox childCheckBox)
                    {
                        childCheckBox.IsChecked = isChecked;
                    }
                }
            }
        }

        private void PropagateCheckState(TreeViewItem item, bool isChecked)
        {
            foreach (TreeViewItem childItem in item.Items)
            {
                if (childItem.Header is CheckBox childCheckBox)
                {
                    childCheckBox.IsChecked = isChecked;
                    PropagateCheckState(childItem, isChecked);
                }
            }
        }

        private void SavePreviousGroupState()
        {
            if (previousGroupId.HasValue)
            {
                var previousGroup = _fileGroups.FirstOrDefault(g => g.Id == previousGroupId.Value);
                if (previousGroup != null)
                {
                    _editedGroups[previousGroupId.Value] = GetCheckedFiles(_fileTreeView.Items[0] as TreeViewItem);
                }
            }
        }

        private List<string> GetCheckedFiles(TreeViewItem node)
        {
            var checkedFiles = new List<string>();

            if (node.Header is CheckBox checkBox)
            {
                if (checkBox.IsChecked == true)
                {
                    if (checkBox.Tag.ToString() != "Root" && !Directory.Exists(checkBox.Tag.ToString()))
                    {
                        checkedFiles.Add(checkBox.Tag.ToString());
                    }
                }

                foreach (TreeViewItem childNode in node.Items)
                {
                    checkedFiles.AddRange(GetCheckedFiles(childNode));
                }
            }
            else
            {
                foreach (TreeViewItem childNode in node.Items)
                {
                    checkedFiles.AddRange(GetCheckedFiles(childNode));
                }
            }

            return checkedFiles;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SavePreviousGroupState(); // Save the state of the last selected group
            EditedFileGroups = new List<FileGroup>();

            foreach (var group in _fileGroups)
            {
                EditedFileGroups.Add(new FileGroup(
                    group.Id,
                    group.Name,
                    _editedGroups[group.Id],
                    group.CreatedAt,
                    DateTime.UtcNow,
                    group.SourceSolutionPath
                )
                { Selected = group.Selected });
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void AddGroupButton_Click(object sender, RoutedEventArgs e)
        {
            var newGroupName = "New Group";
            int counter = 1;
            while (_fileGroups.Any(g => g.Name == newGroupName))
            {
                newGroupName = $"New Group {counter++}";
            }

            var newGroup = new FileGroup(newGroupName, new List<string>(), _solutionName );
            _fileGroups.Add(newGroup);
            _editedGroups[newGroup.Id] = new List<string>();

            PopulateGroupListBox();
            _groupListBox.SelectedItem = newGroup;
        }

        private void DeleteGroupButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedGroup = _groupListBox.SelectedItem as FileGroup;
            if (selectedGroup != null)
            {
                _fileGroups.Remove(selectedGroup);
                _editedGroups.Remove(selectedGroup.Id);

                PopulateGroupListBox();
                if (_fileGroups.Any())
                {
                    _groupListBox.SelectedItem = _fileGroups[0];
                }
                else
                {
                    _nameTextBox.Text = string.Empty;
                    _fileTreeView.Items.Clear();
                }
            }
        }

        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var selectedGroup = _groupListBox.SelectedItem as FileGroup;
            if (selectedGroup != null)
            {
                string newName = _nameTextBox.Text;
                if (newName.Length > 50)
                {
                    MessageBox.Show("The group name cannot be longer than 50 characters.", "Name Too Long", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (_fileGroups.Any(g => g != selectedGroup && g.Name == newName))
        {
                    // Name already exists, maybe show an error message
                    MessageBox.Show("A group with this name already exists. Please choose a different name.", "Duplicate Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                selectedGroup.Name = newName;
                _groupListBox.Items.Refresh();
            }
        }
    }
}




