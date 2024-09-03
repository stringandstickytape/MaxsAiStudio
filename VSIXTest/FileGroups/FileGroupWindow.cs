using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using VSIXTest.FileGroups;

namespace VSIXTest
{
    public class FileGroupEditWindow : Window
    {
        private List<FileGroup> _fileGroups;
        private List<string> _availableFiles;
        private ListBox _groupListBox;
        private TextBox _nameTextBox;
        private TreeView _fileTreeView;
        private Dictionary<string, List<string>> _editedGroups;

        public List<FileGroup> EditedFileGroups { get; private set; }

        public FileGroupEditWindow(List<FileGroup> fileGroups, List<string> availableFiles)
        {
            _fileGroups = new List<FileGroup>(fileGroups);
            _availableFiles = availableFiles;
            _editedGroups = new Dictionary<string, List<string>>();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Title = "Edit File Groups";
            Width = 700;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var grid = new Grid();
            Content = grid;

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Group ListBox
            _groupListBox = new ListBox { Margin = new Thickness(5) };
            PopulateGroupListBox();
            _groupListBox.SelectionChanged += GroupListBox_SelectionChanged;
            Grid.SetRowSpan(_groupListBox, 2);
            grid.Children.Add(_groupListBox);

            // Name input
            var nameLabel = new Label { Content = "Group Name:" };
            _nameTextBox = new TextBox { Margin = new Thickness(5) };
            var namePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
            namePanel.Children.Add(nameLabel);
            namePanel.Children.Add(_nameTextBox);
            Grid.SetRow(namePanel, 0);
            Grid.SetColumn(namePanel, 1);
            grid.Children.Add(namePanel);

            // File TreeView
            _fileTreeView = new TreeView { Margin = new Thickness(5) };
            Grid.SetRow(_fileTreeView, 1);
            Grid.SetColumn(_fileTreeView, 1);
            grid.Children.Add(_fileTreeView);

            // Buttons
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(5) };
            var saveButton = new Button { Content = "Save All", Width = 75, Margin = new Thickness(5) };
            var cancelButton = new Button { Content = "Cancel", Width = 75, Margin = new Thickness(5) };
            saveButton.Click += SaveButton_Click;
            cancelButton.Click += CancelButton_Click;
            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);
            Grid.SetColumn(buttonPanel, 1);
            grid.Children.Add(buttonPanel);
        }

        private void PopulateGroupListBox()
        {
            foreach (var group in _fileGroups)
            {
                _groupListBox.Items.Add(group.Name);
                _editedGroups[group.Name] = new List<string>(group.FilePaths);
            }
        }

        private string previousGroupName = null;
        private void GroupListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (previousGroupName != null)
            {
                SaveCurrentGroupState();
            }

            if (_groupListBox.SelectedIndex != -1)
            {
                
                var selectedGroup = _fileGroups[_groupListBox.SelectedIndex];

                _nameTextBox.Text = selectedGroup.Name;
                PopulateTreeView(selectedGroup.Name);

                previousGroupName = selectedGroup.Name;
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

        private void PopulateTreeView(string groupName)
        {
            _fileTreeView.Items.Clear();
            var rootNode = CreateTreeViewItem("Root", isFolder: true);
            _fileTreeView.Items.Add(rootNode);

            var commonPath = FindCommonPath(_availableFiles);
            var commonPathLength = commonPath.Length;

            foreach (var file in _availableFiles)
            {
                AddFileToTree(rootNode, file, _editedGroups[groupName].Contains(file), commonPathLength);
            }

            // Expand all nodes after populating the tree
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

        private void SaveCurrentGroupState()
        {
            if (previousGroupName != null)
            {
                _editedGroups[previousGroupName] = GetCheckedFiles(_fileTreeView.Items[0] as TreeViewItem);
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
                    else
                    {

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
            SaveCurrentGroupState();
            EditedFileGroups = new List<FileGroup>();

            for (int i = 0; i < _fileGroups.Count; i++)
            {
                var originalGroup = _fileGroups[i];
                EditedFileGroups.Add(new FileGroup(
                    originalGroup.Id,
                    originalGroup.Name,
                    _editedGroups[originalGroup.Name],
                    originalGroup.CreatedAt,
                    DateTime.UtcNow
                ));
            }

            DialogResult = true;
            Close();
        }



        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}