using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;


namespace VSIXTest.FileGroups
{

    public class FileGroup
    {
        // Unique identifier for the file group
        public Guid Id { get; set; }

        // Name of the file group
        public string Name { get; set; }

        // List of file paths included in this group
        public List<string> FilePaths { get; set; }

        // Date and time when the group was created
        public DateTime CreatedAt { get; set; }

        // Date and time when the group was last modified
        public DateTime LastModifiedAt { get; set; }

        // Empty constructor for JSON deserialization
        public FileGroup()
        {
        }

        // Constructor for creating a new file group
        public FileGroup(string name, List<string> filePaths)
        {
            Id = Guid.NewGuid();
            Name = name;
            FilePaths = filePaths ?? new List<string>();
            CreatedAt = DateTime.UtcNow;
            LastModifiedAt = CreatedAt;
        }

        // Constructor for loading an existing file group (e.g., from storage)
        public FileGroup(Guid id, string name, List<string> filePaths, DateTime createdAt, DateTime lastModifiedAt)
        {
            Id = id;
            Name = name;
            FilePaths = filePaths ?? new List<string>();
            CreatedAt = createdAt;
            LastModifiedAt = lastModifiedAt;
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
