using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.IO;
using System.Windows.Media;
using static RoslynHelper;

namespace VSIXTest
{
    public class FileWithMembersSelectionWindow : Window
    {
        private List<FileWithMembers> _filesWithMembers;
        private TreeView _fileTreeView;
        private Button _okButton;
        private Button _cancelButton;

        public List<SelectedFileWithMembers> SelectedItems { get; private set; }

        public FileWithMembersSelectionWindow(List<FileWithMembers> filesWithMembers)
        {
            _filesWithMembers = filesWithMembers;
            InitializeComponent();
            PopulateTreeView();
        }

        private void InitializeComponent()
        {
            Title = "Select Files and Members";
            Width = 600;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var grid = new Grid();
            Content = grid;

            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _fileTreeView = new TreeView { Margin = new Thickness(10) };
            Grid.SetRow(_fileTreeView, 0);
            grid.Children.Add(_fileTreeView);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };
            Grid.SetRow(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            _okButton = new Button { Content = "OK", Width = 75, Margin = new Thickness(0, 0, 10, 0) };
            _okButton.Click += OkButton_Click;
            buttonPanel.Children.Add(_okButton);

            _cancelButton = new Button { Content = "Cancel", Width = 75 };
            _cancelButton.Click += CancelButton_Click;
            buttonPanel.Children.Add(_cancelButton);
        }

        private void PopulateTreeView()
        {
            foreach (var file in _filesWithMembers)
            {
                var fileItem = CreateTreeViewItem(Path.GetFileName(file.FilePath), file.FilePath, true);
                _fileTreeView.Items.Add(fileItem);

                foreach (var member in file.Members)
                {
                    var memberItem = CreateTreeViewItem($"{member.Kind}: {member.Name}", member, false);
                    fileItem.Items.Add(memberItem);
                }
            }
        }

        private TreeViewItem CreateTreeViewItem(string header, object tag, bool isFile)
        {
            var item = new TreeViewItem();
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var checkBox = new CheckBox { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) };
            checkBox.Checked += CheckBox_CheckedChanged;
            checkBox.Unchecked += CheckBox_CheckedChanged;

            stackPanel.Children.Add(checkBox);
            stackPanel.Children.Add(new TextBlock { Text = header, VerticalAlignment = VerticalAlignment.Center });

            item.Header = stackPanel;
            item.Tag = tag;

            if (isFile)
            {
                item.Expanded += FileItem_Expanded;
            }

            return item;
        }

        private void FileItem_Expanded(object sender, RoutedEventArgs e)
        {
            var fileItem = (TreeViewItem)sender;
            var fileCheckBox = ((StackPanel)fileItem.Header).Children.OfType<CheckBox>().First();

            foreach (TreeViewItem memberItem in fileItem.Items)
            {
                var memberCheckBox = ((StackPanel)memberItem.Header).Children.OfType<CheckBox>().First();
                memberCheckBox.IsChecked = fileCheckBox.IsChecked;
            }
        }

        private void CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            var checkBox = (CheckBox)sender;
            var treeViewItem = FindParentTreeViewItem(checkBox);

            if (treeViewItem.Tag is string) // File item
            {
                foreach (TreeViewItem childItem in treeViewItem.Items)
                {
                    var childCheckBox = ((StackPanel)childItem.Header).Children.OfType<CheckBox>().First();
                    childCheckBox.IsChecked = checkBox.IsChecked;
                }
            }
            else // Member item
            {
                UpdateParentCheckBox(treeViewItem);
            }
        }

        private void UpdateParentCheckBox(TreeViewItem memberItem)
        {
            var parentItem = memberItem.Parent as TreeViewItem;
            if (parentItem != null)
            {
                var parentCheckBox = ((StackPanel)parentItem.Header).Children.OfType<CheckBox>().First();
                var allChecked = parentItem.Items.Cast<TreeViewItem>().All(item => ((StackPanel)item.Header).Children.OfType<CheckBox>().First().IsChecked == true);
                var anyChecked = parentItem.Items.Cast<TreeViewItem>().Any(item => ((StackPanel)item.Header).Children.OfType<CheckBox>().First().IsChecked == true);

                parentCheckBox.IsChecked = allChecked ? true : (anyChecked ? null : (bool?)false);
            }
        }

        private TreeViewItem FindParentTreeViewItem(DependencyObject child)
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is TreeViewItem))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as TreeViewItem;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedItems = GetSelectedItems();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private List<SelectedFileWithMembers> GetSelectedItems()
        {
            var selectedItems = new List<SelectedFileWithMembers>();

            foreach (TreeViewItem fileItem in _fileTreeView.Items)
            {
                var fileCheckBox = ((StackPanel)fileItem.Header).Children.OfType<CheckBox>().First();
                if (fileCheckBox.IsChecked == true)
                {
                    var filePath = (string)fileItem.Tag;
                    var selectedMembers = new List<Member>();

                    foreach (TreeViewItem memberItem in fileItem.Items)
                    {
                        var memberCheckBox = ((StackPanel)memberItem.Header).Children.OfType<CheckBox>().First();
                        if (memberCheckBox.IsChecked == true)
                        {
                            selectedMembers.Add((Member)memberItem.Tag);
                        }
                    }

                    selectedItems.Add(new SelectedFileWithMembers(filePath, selectedMembers));
                }
            }

            return selectedItems;
        }
    }

    public class SelectedFileWithMembers
    {
        public string FilePath { get; set; }
        public List<Member> SelectedMembers { get; set; }

        public SelectedFileWithMembers(string filePath, List<Member> selectedMembers)
        {
            FilePath = filePath;
            SelectedMembers = selectedMembers;
        }
    }
}