// QuickButtonOptionsControl.xaml.cs
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

        public QuickButtonOptionsControl()
        {
            InitializeComponent();
        }


        public List<string> SelectedOptions
        {
            get
            {
                var selectedOptions = new List<string>();

                if (cbCurrentSelection.IsChecked == true) selectedOptions.Add("CurrentSelection");
                if (cbClipboard.IsChecked == true) selectedOptions.Add("Clipboard");
                if (cbCurrentFile.IsChecked == true) selectedOptions.Add("CurrentFile");
                if (cbGitDiff.IsChecked == true) selectedOptions.Add("GitDiff");
                return selectedOptions; // Default
            }
        }

        public VsixUiMessage OriginalMessage { get; set; }

        private void OkButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OptionsSelected?.Invoke(this, new QuickButtonMessageAndOptions { SelectedOptions = SelectedOptions, OriginalVsixMessage = OriginalMessage });
            var window = Window.GetWindow(this);
            window?.Close();
        }
    }

    public class QuickButtonMessageAndOptions
    {
        public List<string> SelectedOptions { get; internal set; }
        public VsixUiMessage OriginalVsixMessage { get; internal set; }
    }
}