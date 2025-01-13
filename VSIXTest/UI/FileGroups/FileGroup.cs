using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace VSIXTest.FileGroups
{

    public class FileGroup : INotifyPropertyChanged // Implement INotifyPropertyChanged
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

        // Add this event
        public event PropertyChangedEventHandler PropertyChanged;

        // Add this method
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Unique identifier for the file group
        public Guid Id { get; set; }


        // List of file paths included in this group
        public List<string> FilePaths { get; set; }

        // Path to the source solution associated with this file group
        public string SourceSolutionPath { get; set; }

        // Date and time when the group was created
        public DateTime CreatedAt { get; set; }

        // Date and time when the group was last modified
        public DateTime LastModifiedAt { get; set; }

        // Empty constructor for JSON deserialization
        public bool Selected { get; set; }

        public FileGroup()
        {
            Selected = false;
        }

        // Constructor for creating a new file group
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

        // Constructor for loading an existing file group (e.g., from storage)
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

        // Method to add a file to the group
        public void AddFile(string filePath)
        {
            if (!FilePaths.Contains(filePath))
            {
                FilePaths.Add(filePath);
                LastModifiedAt = DateTime.UtcNow;
            }
        }

        // Method to remove a file from the group
        public bool RemoveFile(string filePath)
        {
            bool removed = FilePaths.Remove(filePath);
            if (removed)
            {
                LastModifiedAt = DateTime.UtcNow;
            }
            return removed;
        }

        // Method to check if the group contains a specific file
        public bool ContainsFile(string filePath)
        {
            return FilePaths.Contains(filePath);
        }

        // Method to get the number of files in the group
        public int FileCount => FilePaths.Count;

        // Override ToString for easy debugging and logging
        public override string ToString()
        {
            return $"FileGroup: {Name} (Id: {Id}, Files: {FileCount})";
        }
    }
}