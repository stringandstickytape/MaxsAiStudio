using SharedClasses;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace VSIXTest
{
    public partial class QuickButtonOptionsControl : UserControl
    {
        public event EventHandler<QuickButtonMessageAndOptions> OptionsSelected;
        public event EventHandler<string> FileGroupsEditorInvoked;

        public QuickButtonOptionsControl()
        {
            InitializeComponent();
            UpdateTextBoxVisibility();
        }

        private void UpdateTextBoxVisibility()
        {
            txtCurrentSelection.Visibility = Visibility.Collapsed;
            txtClipboard.Visibility = Visibility.Collapsed;
            txtCurrentFile.Visibility = Visibility.Collapsed;
            txtGitDiff.Visibility = Visibility.Collapsed;
            txtXmlDoc.Visibility = Visibility.Visible;
            txtFileGroups.Visibility = Visibility.Visible;

        }

        public List<OptionWithParameter> SelectedOptions
        {
            get
            {
                var selectedOptions = new List<OptionWithParameter>();

                if (cbCurrentSelection.IsChecked == true)
                    selectedOptions.Add(new OptionWithParameter("CurrentSelection", txtCurrentSelection.Text, false));

                if (cbEmbeddings.IsChecked == true)
                    selectedOptions.Add(new OptionWithParameter("Embeddings", "", false));

                if (cbClipboard.IsChecked == true)
                    selectedOptions.Add(new OptionWithParameter("Clipboard", txtClipboard.Text, false));
                if (cbCurrentFile.IsChecked == true)
                    selectedOptions.Add(new OptionWithParameter("CurrentFile", txtCurrentFile.Text, false));
                if (cbAllOpenFiles.IsChecked == true)
                    selectedOptions.Add(new OptionWithParameter("AllOpenFiles", txtAllOpenFiles.Text, false));
                if (cbGitDiff.IsChecked == true)
                    selectedOptions.Add(new OptionWithParameter("GitDiff", txtGitDiff.Text, false));
                if (cbXmlDoc.IsChecked == true)
                    selectedOptions.Add(new OptionWithParameter("XmlDoc", txtXmlDoc.Text, true));
                if (cbFileGroups.IsChecked == true)
                    selectedOptions.Add(new OptionWithParameter("FileGroups", txtFileGroups.Text, true));
                return selectedOptions;
            }
        }

        public VsixUiMessage OriginalMessage { get; set; }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var responseType = ((RadioButton)FindName("rbFileChanges"))?.IsChecked == true ? "FileChanges" : "PlainText";
            OptionsSelected?.Invoke(this, new QuickButtonMessageAndOptions { SelectedOptions = SelectedOptions, OriginalVsixMessage = OriginalMessage, ResponseType = responseType });
            var window = Window.GetWindow(this);
            window?.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            window?.Close();
        }
        private void btnFileGroups_Click(object sender, RoutedEventArgs e)
        {
            FileGroupsEditorInvoked?.Invoke(this, txtFileGroups.Text);
        }


        private void btnOpenFromClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string clipboardText = System.Windows.Clipboard.GetText();
                if (string.IsNullOrEmpty(clipboardText))
                    return;

                // Extract filenames from the clipboard text
                List<string> potentialPaths = new List<string>();

                // Pattern 1: Extract filenames enclosed in backticks
                System.Text.RegularExpressions.Regex backtickRegex = new System.Text.RegularExpressions.Regex("`([^`]+)`");
                var backtickMatches = backtickRegex.Matches(clipboardText);
                foreach (System.Text.RegularExpressions.Match match in backtickMatches)
                {
                    potentialPaths.Add(match.Groups[1].Value.Trim());
                }

                // Pattern 2: Extract paths in "MaxsFilePath format" (paths followed by dash and description)
                System.Text.RegularExpressions.Regex pathRegex = new System.Text.RegularExpressions.Regex(@"((?:[A-Za-z]:\\|/)[^-\r\n]+?)(?:\s+-\s+.+)?$",
                    System.Text.RegularExpressions.RegexOptions.Multiline);
                var pathMatches = pathRegex.Matches(clipboardText);
                foreach (System.Text.RegularExpressions.Match match in pathMatches)
                {
                    potentialPaths.Add(match.Groups[1].Value.Trim());
                }

                // If we didn't find any paths, exit
                if (potentialPaths.Count == 0)
                    return;

                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                EnvDTE80.DTE2 dte = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;

                if (dte == null)
                    return;

                // Get the solution directory as a fallback for relative paths
                string solutionDir = string.Empty;
                if (dte.Solution != null && !string.IsNullOrEmpty(dte.Solution.FullName))
                {
                    solutionDir = System.IO.Path.GetDirectoryName(dte.Solution.FullName);
                }

                // Get all project directories in the solution
                List<string> projectDirs = new List<string>();
                if (dte.Solution != null)
                {
                    foreach (EnvDTE.Project project in dte.Solution.Projects)
                    {
                        try
                        {
                            if (project.FullName != null && System.IO.File.Exists(project.FullName))
                            {
                                string projectDir = System.IO.Path.GetDirectoryName(project.FullName);
                                if (!string.IsNullOrEmpty(projectDir) && !projectDirs.Contains(projectDir))
                                {
                                    projectDirs.Add(projectDir);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error accessing project: {ex.Message}");
                        }
                    }
                }

                // Try to open each potential file path
                foreach (string filename in potentialPaths)
                {
                    TryOpenFile(dte, filename, solutionDir, projectDirs);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing clipboard: {ex.Message}");
            }
        }

        private void TryOpenFile(EnvDTE80.DTE2 dte, string filename, string solutionDir, List<string> projectDirs)
        {
            bool fileOpened = false;

            try
            {
                // Check if this is a fully qualified Windows path (with drive letter)
                bool hasWindowsDriveLetter = (filename.Length >= 2 && filename[1] == ':');

                // If it's an absolute path with a drive letter, try it directly
                if (hasWindowsDriveLetter && System.IO.File.Exists(filename))
                {
                    dte.ItemOperations.OpenFile(filename);
                    fileOpened = true;
                    return;
                }

                // For paths without drive letters (including those starting with / or \)
                if (!string.IsNullOrEmpty(solutionDir))
                {
                    string adjustedFilename = filename;

                    // Remove leading slash if present
                    if (adjustedFilename.StartsWith("/") || adjustedFilename.StartsWith("\\"))
                    {
                        adjustedFilename = adjustedFilename.Substring(1);
                    }

                    string fullPath = System.IO.Path.Combine(solutionDir, adjustedFilename);

                    if (System.IO.File.Exists(fullPath))
                    {
                        dte.ItemOperations.OpenFile(fullPath);
                        fileOpened = true;
                        return;
                    }
                }

                // Try each project directory
                if (!fileOpened)
                {
                    string adjustedFilename = filename;

                    // Remove leading slash if present
                    if (adjustedFilename.StartsWith("/") || adjustedFilename.StartsWith("\\"))
                    {
                        adjustedFilename = adjustedFilename.Substring(1);
                    }

                    foreach (string projectDir in projectDirs)
                    {
                        string fullPath = System.IO.Path.Combine(projectDir, adjustedFilename);

                        if (System.IO.File.Exists(fullPath))
                        {
                            dte.ItemOperations.OpenFile(fullPath);
                            fileOpened = true;
                            break;
                        }
                    }
                }

                // Last resort - try the original path as-is
                if (!fileOpened && System.IO.File.Exists(filename))
                {
                    dte.ItemOperations.OpenFile(filename);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening file {filename}: {ex.Message}");
            }
        }

    }

        public class QuickButtonMessageAndOptions
    {
        public List<OptionWithParameter> SelectedOptions { get; internal set; }
        public VsixUiMessage OriginalVsixMessage { get; internal set; }
        public string ResponseType { get; internal set; }
    }
}