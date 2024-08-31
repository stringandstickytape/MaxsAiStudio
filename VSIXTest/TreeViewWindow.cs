using System.Collections.Generic;
using System.Windows.Controls;
using Window = System.Windows.Window;
using TreeView = System.Windows.Controls.TreeView;


namespace VSIXTest
{
    public class TreeViewWindow : Window
    {
        public TreeView MainTreeView { get; private set; }

        public TreeViewWindow()
        {
            InitializeComponent();
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

        public void PopulateTreeView(IEnumerable<TreeViewItem> items)
        {
            MainTreeView.Items.Clear();
            foreach (var item in items)
            {
                MainTreeView.Items.Add(item);
            }
        }
    }
}