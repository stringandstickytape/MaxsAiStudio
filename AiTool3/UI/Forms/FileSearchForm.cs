using AiTool3.Embeddings;
using Newtonsoft.Json;
using System.Data;
using System.Text.RegularExpressions;

namespace AiTool3.UI.Forms
{
    public class FileSearchForm : Form
    {
        private TreeView treeView;
        private Panel buttonPanel;
        private Button testButton;
        private Button addFilesToInputButton;
        private TextBox quickJumpTextBox;
        private string rootPath;
        private string[] fileExtensions;
        public EventHandler<List<string>> AddFilesToInput;
        private GitIgnoreFilterManager gitIgnoreFilterManager = new GitIgnoreFilterManager("");

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

            string gitignore = null;
            gitIgnoreFilterManager = null;
            // check for a .gitignore
            if (File.Exists(Path.Combine(rootPath, ".gitignore")))
            {
                gitignore = File.ReadAllText(Path.Combine(rootPath, ".gitignore"));
                gitIgnoreFilterManager = new GitIgnoreFilterManager(gitignore);
            }

            PopulateTreeView(gitignore, checkedFiles);

            Load += (sender, e) =>
            {
                if (GetCheckedFiles().Any())
                {
                    FindLastNode(treeView.Nodes)?.EnsureVisible();
                    FindFirstCheckedNode(treeView.Nodes)?.EnsureVisible();
                }
            };
        }

        public FileSearchForm(List<string> filePaths)
        {
            fileExtensions = new string[1] { "*" };


            InitializeComponent();

            List<string> checkedFiles = new List<string>();

            if (File.Exists("Settings\\ProjectHelperSelection.json"))
            {
                var json = File.ReadAllText("Settings\\ProjectHelperSelection.json");
                checkedFiles = JsonConvert.DeserializeObject<List<string>>(json);
            }

            PopulateTreeViewFromPaths(filePaths, checkedFiles);

            Load += (sender, e) =>
            {
                if (GetCheckedFiles().Any())
                {
                    FindLastNode(treeView.Nodes)?.EnsureVisible();
                    FindFirstCheckedNode(treeView.Nodes)?.EnsureVisible();
                }
            };
        }

        private static string FindCommonRoot(List<string> paths)
        {
            if (paths == null || paths.Count == 0)
                return string.Empty;

            var firstPath = paths[0];
            var commonRoot = firstPath;

            for (int i = 1; i < paths.Count; i++)
            {
                var path = paths[i];
                int j;
                for (j = 0; j < commonRoot.Length && j < path.Length; j++)
                {
                    if (char.ToLower(commonRoot[j]) != char.ToLower(path[j]))
                        break;
                }
                commonRoot = commonRoot.Substring(0, j);
            }

            // Ensure the common root ends at a directory separator
            int lastSeparatorIndex = commonRoot.LastIndexOf(Path.DirectorySeparatorChar);
            if (lastSeparatorIndex >= 0)
                commonRoot = commonRoot.Substring(0, lastSeparatorIndex + 1);
            else
                commonRoot = string.Empty;

            return commonRoot;
        }

        private void PopulateTreeViewFromPaths(List<string> filePaths, List<string> checkedFiles)
        {
            var commonRoot = FindCommonRoot(filePaths);
            treeView.Nodes.Clear();
            var rootNode = new TreeNode("Files");
            treeView.Nodes.Add(rootNode);

            foreach (var filePath in filePaths)
            {
                if (fileExtensions.Contains("*") || fileExtensions.Contains(Path.GetExtension(filePath).ToLower()))
                {
                    var pathParts = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    var currentNode = rootNode;

                    for (int i = 0; i < pathParts.Length; i++)
                    {
                        var part = pathParts[i];
                        var existingNode = currentNode.Nodes.Cast<TreeNode>().FirstOrDefault(n => n.Text == part);

                        if (existingNode == null)
                        {
                            existingNode = new TreeNode(part);
                            currentNode.Nodes.Add(existingNode);
                        }

                        if (i == pathParts.Length - 1) // This is a file
                        {
                            existingNode.Checked = checkedFiles.Contains(filePath);
                        }

                        currentNode = existingNode;
                    }
                }
            }

            treeView.ExpandAll();
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
            ClientSize = new Size(500, 800);
            treeView = new TreeView();
            buttonPanel = new Panel();
            testButton = new Button();
            addFilesToInputButton = new Button();
            SuspendLayout();

            // TreeView
            treeView.Dock = DockStyle.Fill;
            treeView.CheckBoxes = true;
            treeView.AfterCheck += new TreeViewEventHandler(treeView_AfterCheck);
            treeView.ItemDrag += new ItemDragEventHandler(treeView_ItemDrag);
            treeView.AllowDrop = true;
            treeView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            treeView.Width = Width - 22;
            treeView.Top = 40;
            treeView.Height = Height - 140;
            // Button Panel
            buttonPanel.Dock = DockStyle.Bottom;
            buttonPanel.Height = 40;

            // Test Button
            testButton.Text = "Test";
            testButton.Location = new Point(10, 5);
            testButton.Size = new Size(75, 30);
            testButton.Click += new EventHandler(testButton_Click);
            testButton.Enabled = false;

            // Add Test Button to Button Panel
            buttonPanel.Controls.Add(testButton);

            // add files to input button

            addFilesToInputButton.Text = "Add Files to Input";
            addFilesToInputButton.Location = new Point(100, 5);
            addFilesToInputButton.Size = new Size(250, 30);
            addFilesToInputButton.Click += (sender, e) => AddFilesToInput?.Invoke(this, GetCheckedFiles());

            buttonPanel.Controls.Add(addFilesToInputButton);


            // Quick Jump TextBox
            quickJumpTextBox = new TextBox();
            quickJumpTextBox.Dock = DockStyle.Top;
            quickJumpTextBox.Font = new Font("Segoe UI", 9F);
            quickJumpTextBox.PlaceholderText = "Quick Jump (type to search)";
            quickJumpTextBox.TextChanged += new EventHandler(quickJumpTextBox_TextChanged);

            // Form

            Controls.Add(buttonPanel);
            Controls.Add(treeView);
            Controls.Add(quickJumpTextBox); // Add the quick jump textbox
            Name = "FileExplorerForm";
            Text = "File Explorer (set start location in Edit -> Settings -> Default Path)";


            ResumeLayout(false);


        }

        private void quickJumpTextBox_TextChanged(object sender, EventArgs e)
        {
            string searchText = quickJumpTextBox.Text.ToLower();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return;
            }

            TreeNode matchingNode = FindMatchingNode(treeView.Nodes, searchText);
            if (matchingNode != null)
            {
                treeView.SelectedNode = matchingNode;
                // scroll to the last node first
                FindLastNode(treeView.Nodes)?.EnsureVisible();

                matchingNode.EnsureVisible();
            }
        }

        private TreeNode FindMatchingNode(TreeNodeCollection nodes, string searchText)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Text.ToLower().Contains(searchText))
                {
                    return node;
                }

                TreeNode matchingChild = FindMatchingNode(node.Nodes, searchText);
                if (matchingChild != null)
                {
                    return matchingChild;
                }
            }

            return null;
        }
        private void testButton_Click(object sender, EventArgs e)
        {
            var files = GetCheckedFiles();

            var cSharpAnalyzer = new CSharpAnalyzer();
            var methodInfos = cSharpAnalyzer.AnalyzeFiles(files);
            // remove all System methods
            methodInfos.RemoveAll(m => m.Namespace.StartsWith("System"));
            foreach (var m in methodInfos)
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

            if (PopulateTreeNode(rootNode, rootPath, checkedFiles))
            {
                treeView.Nodes.Add(rootNode);
            }
            treeView.ExpandAll();
        }

        private bool PopulateTreeNode(TreeNode node, string path, List<string>? checkedFiles)
        {
            bool hasValidChildren = false;

            string[] subdirectories = Directory.GetDirectories(path);
            foreach (string subdirectory in subdirectories)
            {
                string relativePath = GetRelativePath(rootPath, subdirectory);

                if (gitIgnoreFilterManager == null || !gitIgnoreFilterManager.PathIsIgnored(subdirectory))
                {
                    TreeNode subNode = new TreeNode(Path.GetFileName(subdirectory));
                    if (PopulateTreeNode(subNode, subdirectory, checkedFiles))
                    {
                        if (checkedFiles != null && checkedFiles.Contains(subdirectory))
                        {
                            subNode.Checked = true;
                        }

                        node.Nodes.Add(subNode);
                        hasValidChildren = true;
                    }
                }
            }

            string[] files = Directory.GetFiles(path);
            foreach (string file in files)
            {
                string relativePath = GetRelativePath(rootPath, file);

                if (gitIgnoreFilterManager == null || !gitIgnoreFilterManager.PathIsIgnored(file))
                {
                    string extension = Path.GetExtension(file).ToLower();
                    if (fileExtensions.Contains(extension))
                    {
                        TreeNode fileNode = new TreeNode(Path.GetFileName(file));
                        if (checkedFiles != null && checkedFiles.Contains(file))
                        {
                            fileNode.Checked = true;
                        }
                        node.Nodes.Add(fileNode);
                        hasValidChildren = true;
                    }
                }
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
    }
}