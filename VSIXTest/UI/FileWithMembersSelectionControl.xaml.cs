using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace VSIXTest
{
    public partial class FileWithMembersSelectionControl : UserControl
    {
        public ObservableCollection<FileTreeItem> Files { get; set; }

        public event EventHandler<FileSelectionEventArgs> SelectionComplete;

        public FileWithMembersSelectionControl()
        {
            InitializeComponent();
            Files = new ObservableCollection<FileTreeItem>();
            FileTreeView.ItemsSource = Files;
        }

        public void LoadFiles(List<FileWithMembers> files)
        {
            Files.Clear();
            foreach (var file in files)
            {
                var fileItem = new FileTreeItem
                {
                    Name = file.FilePath,
                    IsFile = true,
                    Members = new ObservableCollection<FileTreeItem>(
                        file.Members.Select(m => new FileTreeItem
                        {
                            Name = m.Name,
                            IsFile = false,
                            Kind = m.Kind,
                            SourceCode = m.SourceCode
                        }))
                };
                Files.Add(fileItem);
            }
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            var item = checkBox.DataContext as FileTreeItem;

            if (item.IsFile)
            {
                foreach (var member in item.Members)
                {
                    member.IsSelected = item.IsSelected;
                }
            }
            else
            {
                var parentItem = Files.FirstOrDefault(f => f.Members.Contains(item));
                if (parentItem != null)
                {
                    parentItem.IsSelected = parentItem.Members.All(m => m.IsSelected);
                }
            }
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            SetSelectionState(true);
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            SetSelectionState(false);
        }

        private void SetSelectionState(bool isSelected)
        {
            foreach (var file in Files)
            {
                file.IsSelected = isSelected;
                foreach (var member in file.Members)
                {
                    member.IsSelected = isSelected;
                }
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            var selectedFiles = Files.Where(f => f.IsSelected || f.Members.Any(m => m.IsSelected))
                                     .Select(f => new FileWithMembers(
                                         f.Name,
                                         f.Members.Where(m => m.IsSelected)
                                                  .Select(m => new Member(m.Name, m.Kind, m.SourceCode))
                                                  .ToList()
                                     )).ToList();

            SelectionComplete?.Invoke(this, new FileSelectionEventArgs(selectedFiles));
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            SelectionComplete?.Invoke(this, new FileSelectionEventArgs(null));
        }
    }

    public class FileTreeItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        public string Name { get; set; }
        public bool IsFile { get; set; }
        public string Kind { get; set; }
        public string SourceCode { get; set; }
        public ObservableCollection<FileTreeItem> Members { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class FileSelectionEventArgs : EventArgs
    {
        public List<FileWithMembers> SelectedFiles { get; }

        public FileSelectionEventArgs(List<FileWithMembers> selectedFiles)
        {
            SelectedFiles = selectedFiles;
        }
    }
}