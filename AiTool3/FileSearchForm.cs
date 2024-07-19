using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace AiTool3
{
    public class FileSearchForm : Form
    {
        private TreeView treeView;
        private Panel buttonPanel;
        private Button testButton;
        private string rootPath;
        private string[] fileExtensions;

        public FileSearchForm(string path, string csvFileTypes)
        {
            rootPath = path;
            fileExtensions = csvFileTypes.Split(',').Select(ext => ext.Trim().ToLower()).ToArray();

            InitializeComponent();
            PopulateTreeView();
        }

        private void InitializeComponent()
        {
            this.treeView = new TreeView();
            this.buttonPanel = new Panel();
            this.testButton = new Button();
            this.SuspendLayout();

            // TreeView
            this.treeView.Dock = DockStyle.Fill;
            this.treeView.CheckBoxes = true;
            this.treeView.AfterCheck += new TreeViewEventHandler(treeView_AfterCheck);
            this.treeView.ItemDrag += new ItemDragEventHandler(treeView_ItemDrag);
            this.treeView.AllowDrop = true;

            // Button Panel
            this.buttonPanel.Dock = DockStyle.Bottom;
            this.buttonPanel.Height = 40;

            // Test Button
            this.testButton.Text = "Test";
            this.testButton.Location = new Point(10, 5);
            this.testButton.Size = new Size(75, 30);
            this.testButton.Click += new EventHandler(testButton_Click);

            // Add Test Button to Button Panel
            this.buttonPanel.Controls.Add(this.testButton);

            // Form
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.buttonPanel);
            this.Controls.Add(this.treeView);
            this.Name = "FileExplorerForm";
            this.Text = "File Explorer";
            this.ResumeLayout(false);
        }

        private void testButton_Click(object sender, EventArgs e)
        {
            var files = GetCheckedFiles();

            var cSharpAnalyzer = new CSharpAnalyzer();
            var methodInfos = cSharpAnalyzer.AnalyzeFiles(files);
            // remove all System methods
            methodInfos.RemoveAll(m => m.Namespace.StartsWith("System"));
            foreach(var m in methodInfos)
                m.RelatedMethodsFullName = m.RelatedMethodsFullName.Where(m => !m.StartsWith("System")).ToList();

            var mermaidDiagram = cSharpAnalyzer.GenerateMermaidDiagram(methodInfos);
            
            var interestingMethods = methodInfos.OrderByDescending(m => m.RelatedMethodsFullName.Count).ToList();

            MessageBox.Show($"Files checked: {string.Join(", ", files)}");
        }

        private void treeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Item is TreeNode node)
            {
                string fullPath = GetFullPath(node);
                if (File.Exists(fullPath))
                {
                    DataObject data = new DataObject(DataFormats.Text, fullPath);
                    DoDragDrop(data, DragDropEffects.Copy);
                }
            }
        }

        private void PopulateTreeView()
        {
            treeView.Nodes.Clear();
            TreeNode rootNode = new TreeNode(rootPath);
            if (PopulateTreeNode(rootNode, rootPath))
            {
                treeView.Nodes.Add(rootNode);
            }
            treeView.ExpandAll();
        }

        private bool PopulateTreeNode(TreeNode node, string path)
        {
            bool hasValidChildren = false;

            string[] subdirectories = Directory.GetDirectories(path);
            foreach (string subdirectory in subdirectories)
            {
                TreeNode subNode = new TreeNode(Path.GetFileName(subdirectory));
                if (PopulateTreeNode(subNode, subdirectory))
                {
                    node.Nodes.Add(subNode);
                    hasValidChildren = true;
                }
            }

            string[] files = Directory.GetFiles(path);
            foreach (string file in files)
            {
                string extension = Path.GetExtension(file).ToLower();
                if (fileExtensions.Contains(extension))
                {
                    TreeNode fileNode = new TreeNode(Path.GetFileName(file));
                    node.Nodes.Add(fileNode);
                    hasValidChildren = true;
                }
            }

            return hasValidChildren;
        }

        private void treeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            treeView.AfterCheck -= treeView_AfterCheck;
            CheckAllChildNodes(e.Node, e.Node.Checked);
            CheckParentNodes(e.Node, e.Node.Checked);
            treeView.AfterCheck += treeView_AfterCheck;
        }

        private void CheckAllChildNodes(TreeNode treeNode, bool nodeChecked)
        {
            foreach (TreeNode node in treeNode.Nodes)
            {
                node.Checked = nodeChecked;
                if (node.Nodes.Count > 0)
                {
                    CheckAllChildNodes(node, nodeChecked);
                }
            }
        }

        private void CheckParentNodes(TreeNode treeNode, bool nodeChecked)
        {
            var parent = treeNode.Parent;
            if (parent != null)
            {
                if (!nodeChecked && parent.Checked)
                {
                    parent.Checked = false;
                }
                else if (nodeChecked && !parent.Checked && AllSiblingsChecked(treeNode))
                {
                    parent.Checked = true;
                }
                CheckParentNodes(parent, parent.Checked);
            }
        }

        private bool AllSiblingsChecked(TreeNode node)
        {
            return node.Parent.Nodes.Cast<TreeNode>().All(n => n.Checked);
        }

        public List<string> GetCheckedFiles()
        {
            List<string> checkedFiles = new List<string>();
            GetCheckedNodes(treeView.Nodes, checkedFiles);
            return checkedFiles;
        }

        private void GetCheckedNodes(TreeNodeCollection nodes, List<string> checkedFiles)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Checked && node.Nodes.Count == 0) // Only add leaf nodes (files)
                {
                    checkedFiles.Add(Path.Combine(GetFullPath(node)));
                }
                GetCheckedNodes(node.Nodes, checkedFiles);
            }
        }

        private string GetFullPath(TreeNode node)
        {
            List<string> pathParts = new List<string>();
            while (node != null)
            {
                pathParts.Add(node.Text);
                node = node.Parent;
            }
            pathParts.Reverse();
            return Path.Combine(pathParts.ToArray());
        }

        /* private string GetFullPath(TreeNode node)
   {
       List<string> pathParts = new List<string>();
       while (node != null)
       {
           pathParts.Add(node.Text);
           node = node.Parent;
       }
       pathParts.Reverse();
       return Path.Combine(rootPath, Path.Combine(pathParts.ToArray()));
   } */
    }
}