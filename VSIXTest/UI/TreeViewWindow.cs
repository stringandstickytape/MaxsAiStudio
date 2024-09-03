using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Window = System.Windows.Window;
using TreeView = System.Windows.Controls.TreeView;
using System;

namespace VSIXTest
{
    public class TreeViewWindow : Window
    {
        public TreeView MainTreeView { get; private set; }
        public event EventHandler<EventArgs> OnClose;
        public TreeViewWindow()
        {
            InitializeComponent();
            this.Closing += TreeViewWindow_Closing;
        }

        private void InitializeComponent()
        {
            Title = "TreeView Window";
            Width = 300;
            Height = 450;

            // Create the main grid
            var grid = new Grid();

            // Create the TreeView
            MainTreeView = new TreeView();

            // Add the TreeView to the grid
            grid.Children.Add(MainTreeView);

            // Set the grid as the window's content
            Content = grid;
        }

        private void TreeViewWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Raise the OnClose event
            OnClose?.Invoke(this, EventArgs.Empty);
        }

        public void PopulateTreeView(IEnumerable<TreeViewItem> items)
        {
            MainTreeView.Items.Clear();
            foreach (var item in items)
            {
                AddCheckBoxToItem(item);
                MainTreeView.Items.Add(item);
            }
        }

        private void AddCheckBoxToItem(TreeViewItem item)
        {
            // Create a StackPanel to hold the CheckBox and the original Header
            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;

            // Create a CheckBox
            CheckBox checkBox = new CheckBox();
            checkBox.VerticalAlignment = VerticalAlignment.Center;
            checkBox.Margin = new Thickness(0, 0, 5, 0);

            // Add the CheckBox to the panel
            panel.Children.Add(checkBox);

            // If the original Header is a string, add it as a TextBlock
            if (item.Header is string headerText)
            {
                TextBlock textBlock = new TextBlock();
                textBlock.Text = headerText;
                textBlock.VerticalAlignment = VerticalAlignment.Center;
                panel.Children.Add(textBlock);
            }
            else
            {
                // If it's not a string, add the original Header
                panel.Children.Add(item.Header as UIElement);
            }

            // Set the panel as the new Header of the TreeViewItem
            item.Header = panel;

            // Recursively add checkboxes to child items
            foreach (TreeViewItem childItem in item.Items)
            {
                AddCheckBoxToItem(childItem);
            }
        }

        public List<string> GetCheckedItems()
        {
            List<string> checkedItems = new List<string>();
            foreach (TreeViewItem item in MainTreeView.Items)
            {
                GetCheckedItemsRecursive(item, checkedItems);
            }
            return checkedItems;
        }

        private void GetCheckedItemsRecursive(TreeViewItem item, List<string> checkedItems)
        {
            StackPanel panel = item.Header as StackPanel;
            if (panel != null)
            {
                CheckBox checkBox = panel.Children[0] as CheckBox;
                if (checkBox != null && checkBox.IsChecked == true)
                {
                    // Assuming the second child of the StackPanel is a TextBlock or contains the item text
                    if (panel.Children[1] is TextBlock textBlock)
                    {
                        checkedItems.Add(item.Tag.ToString());
                    }
                    else
                    {
                        // Fallback in case the text is stored differently
                        checkedItems.Add(item.ToString());
                    }
                }
            }

            // Recursively check child items
            foreach (TreeViewItem childItem in item.Items)
            {
                GetCheckedItemsRecursive(childItem, checkedItems);
            }
        }
    }
}