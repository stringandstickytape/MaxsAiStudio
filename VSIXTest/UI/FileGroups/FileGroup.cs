using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace VSIXTest.FileGroups
{

    public class FileGroup : INotifyPropertyChanged
    {
        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Guid Id { get; set; }

        public List<string> FilePaths { get; set; }

        public string SourceSolutionPath { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime LastModifiedAt { get; set; }

        public bool Selected { get; set; }

        public FileGroup()
        {
            Selected = false;
        }

        public FileGroup(string name, List<string> filePaths, string sourceSolutionPath)
        {
            Id = Guid.NewGuid();
            Name = name;
            FilePaths = filePaths ?? new List<string>();
            SourceSolutionPath = sourceSolutionPath;
            CreatedAt = DateTime.UtcNow;
            LastModifiedAt = CreatedAt;
            Selected = false;
        }

        public FileGroup(Guid id, string name, List<string> filePaths, DateTime createdAt, DateTime lastModifiedAt, string sourceSolutionPath)
        {
            Id = id;
            Name = name;
            FilePaths = filePaths ?? new List<string>();
            SourceSolutionPath = sourceSolutionPath;
            CreatedAt = createdAt;
            LastModifiedAt = lastModifiedAt;
            Selected = false;
        }

        public int FileCount => FilePaths.Count;

        public override string ToString()
        {
            return $"FileGroup: {Name} (Id: {Id}, Files: {FileCount})";
        }
    }
}