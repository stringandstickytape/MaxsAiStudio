using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AiTool3
{
    public class FileSearchForm : Form
    {
        private TreeView treeView;
        private Panel buttonPanel;
        private Button testButton;
        private Button addFilesToInputButton;
        private string rootPath;
        private string[] fileExtensions;
        public EventHandler<List<string>> AddFilesToInput;

        public FileSearchForm(string path, string csvFileTypes)
        {
            rootPath = path;
            fileExtensions = csvFileTypes.Replace("*", "").Split(',').Select(ext => ext.Trim().ToLower()).ToArray();

            InitializeComponent();

            List<string> checkedFiles = new List<string>();
            
            if (File.Exists("Settings\\ProjectHelperSelection.json"))
            {
                var json = File.ReadAllText("Settings\\ProjectHelperSelection.json");
                checkedFiles = JsonConvert.DeserializeObject<List<string>>(json);
            }

            PopulateTreeView(@"", checkedFiles);

            Load += (sender, e) =>
            {
                FindLastNode(treeView.Nodes)?.EnsureVisible();
                FindFirstCheckedNode(treeView.Nodes)?.EnsureVisible();
            };
        }

        private TreeNode FindLastNode(TreeNodeCollection nodes)
        {
            TreeNode lastNode = null;
            foreach (TreeNode node in nodes)
            {
                if (node.Nodes.Count > 0)
                {
                    var childNode = FindLastNode(node.Nodes);
                    if (childNode != null)
                    {
                        lastNode = childNode;
                    }
                }
                else
                {
                    lastNode = node;
                }
            }
            return lastNode;
            
        }

        private TreeNode FindFirstCheckedNode(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Checked)
                {
                    return node;
                }
                var childNode = FindFirstCheckedNode(node.Nodes);
                if (childNode != null)
                {
                    return childNode;
                }
            }
            return null;
        }

        private void InitializeComponent()
        {
            this.treeView = new TreeView();
            this.buttonPanel = new Panel();
            this.testButton = new Button();
            this.addFilesToInputButton = new Button();
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
            this.testButton.Enabled = false;

            // Add Test Button to Button Panel
            this.buttonPanel.Controls.Add(this.testButton);

            // add files to input button

            this.addFilesToInputButton.Text = "Add Files to Input";
            this.addFilesToInputButton.Location = new Point(100, 5);
            this.addFilesToInputButton.Size = new Size(250, 30);
            this.addFilesToInputButton.Click += (sender, e) => AddFilesToInput?.Invoke(this, GetCheckedFiles());

            this.buttonPanel.Controls.Add(this.addFilesToInputButton);
            

            // Form
            this.ClientSize = new System.Drawing.Size(500, 800);
            this.Controls.Add(this.buttonPanel);
            this.Controls.Add(this.treeView);
            this.Name = "FileExplorerForm";
            this.Text = "File Explorer (set start location in Edit -> Settings -> Default Path)";
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

        private void PopulateTreeView(string gitignoreContent = null, List<string>? checkedFiles = null)
        {
            treeView.Nodes.Clear();
            TreeNode rootNode = new TreeNode(rootPath);
            var ignoreList = ParseGitignore(gitignoreContent);
            if (PopulateTreeNode(rootNode, rootPath, ignoreList, checkedFiles))
            {
                treeView.Nodes.Add(rootNode);
            }
            treeView.ExpandAll();
        }

        private bool PopulateTreeNode(TreeNode node, string path, List<string> ignoreList, List<string>? checkedFiles)
        {
            bool hasValidChildren = false;

            string[] subdirectories = Directory.GetDirectories(path);
            foreach (string subdirectory in subdirectories)
            {
                string relativePath = GetRelativePath(rootPath, subdirectory);
                if (!ShouldIgnore(relativePath, ignoreList))
                {
                    TreeNode subNode = new TreeNode(Path.GetFileName(subdirectory));
                    if (PopulateTreeNode(subNode, subdirectory, ignoreList, checkedFiles))
                    {
                        if (checkedFiles != null && checkedFiles.Contains(subdirectory))
                        {
                            subNode.Checked = true;
                        }

                        node.Nodes.Add(subNode);
                        hasValidChildren = true;
                    }
                }
                else Debug.WriteLine(subdirectory);
            }

            string[] files = Directory.GetFiles(path);
            foreach (string file in files)
            {
                string relativePath = GetRelativePath(rootPath, file);
                if (!ShouldIgnore(relativePath, ignoreList))
                {
                    string extension = Path.GetExtension(file).ToLower();
                    if (fileExtensions.Contains(extension))
                    {
                        TreeNode fileNode = new TreeNode(Path.GetFileName(file));
                        if(checkedFiles != null && checkedFiles.Contains(file))
                        {
                            fileNode.Checked = true;
                        }
                        node.Nodes.Add(fileNode);
                        hasValidChildren = true;
                    }
                }
                else Debug.WriteLine(file);
            }

            return hasValidChildren;
        }

        private List<string> ParseGitignore(string gitignoreContent)
        {
            if (string.IsNullOrEmpty(gitignoreContent))
                return new List<string>();

            return gitignoreContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !line.StartsWith("#") && !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Trim())
                .ToList();
        }

        private bool ShouldIgnore(string path, List<string> ignoreList)
        {
            return ignoreList.Any(ignore =>
                path.StartsWith(ignore, StringComparison.OrdinalIgnoreCase) ||
                Regex.IsMatch(path, WildcardToRegex(ignore), RegexOptions.IgnoreCase));
        }

        private string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern)
                       .Replace("\\*", ".*")
                       .Replace("\\?", ".") + "$";
        }

        private string GetRelativePath(string rootPath, string fullPath)
        {
            return fullPath.Substring(rootPath.Length).TrimStart(Path.DirectorySeparatorChar);
        }

        private void treeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            treeView.AfterCheck -= treeView_AfterCheck;
            CheckAllChildNodes(e.Node, e.Node.Checked);
            CheckParentNodes(e.Node, e.Node.Checked);
            treeView.AfterCheck += treeView_AfterCheck;

            var checkedFiles = GetCheckedFiles(true);
            
            // serialize to Settings\ProjectHelperSelection.json
            var json = JsonConvert.SerializeObject(checkedFiles);
            File.WriteAllText("Settings\\ProjectHelperSelection.json", json);
            
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

        public List<string> GetCheckedFiles(bool includeDirectories = false)
        {
            List<string> checkedFiles = new List<string>();
            GetCheckedFiles(treeView.Nodes, checkedFiles, includeDirectories);
            return checkedFiles;
        }

        private void GetCheckedFiles(TreeNodeCollection nodes, List<string> checkedFiles, bool includeDirectories = false)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Checked && (includeDirectories || node.Nodes.Count == 0)) // Only add leaf nodes (files)
                {
                    checkedFiles.Add(Path.Combine(GetFullPath(node)));
                }
                GetCheckedFiles(node.Nodes, checkedFiles, includeDirectories);
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