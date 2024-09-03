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
                if (cbClipboard.IsChecked == true)
                    selectedOptions.Add(new OptionWithParameter("Clipboard", txtClipboard.Text, false));
                if (cbCurrentFile.IsChecked == true)
                    selectedOptions.Add(new OptionWithParameter("CurrentFile", txtCurrentFile.Text, false));
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
            OptionsSelected?.Invoke(this, new QuickButtonMessageAndOptions { SelectedOptions = SelectedOptions, OriginalVsixMessage = OriginalMessage });
            var window = Window.GetWindow(this);
            window?.Close();
        }

        private void btnFileGroups_Click(object sender, RoutedEventArgs e)
        {
            FileGroupsEditorInvoked?.Invoke(this, txtFileGroups.Text);
        }
    }

    public class QuickButtonMessageAndOptions
    {
        public List<OptionWithParameter> SelectedOptions { get; internal set; }
        public VsixUiMessage OriginalVsixMessage { get; internal set; }
    }

    public class OptionWithParameter
    {
        public string Option { get; set; }
        public string Parameter { get; set; }
        public bool ShowParameter { get; set; }

        public OptionWithParameter(string option, string parameter, bool showParameter)
        {
            Option = option;
            Parameter = parameter;
            ShowParameter = showParameter;
        }
    }

    public class OptionTemplate
    {
        public string Option { get; set; }
        public bool ShowParameter { get; set; }
    }
}