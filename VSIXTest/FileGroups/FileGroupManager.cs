using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace VSIXTest.FileGroups
{


    public class FileGroupManager
    {
        private List<FileGroup> _fileGroups;
        private readonly string _storageDirectory;
        private readonly string _storageFilePath;

        public FileGroupManager(string storageDirectory)
        {
            _storageDirectory = storageDirectory;
            _storageFilePath = Path.Combine(_storageDirectory, "filegroups.json");
            _fileGroups = new List<FileGroup>();
            LoadFileGroups();
        }

        public FileGroup CreateFileGroup(string name, List<string> filePaths)
        {
            if (FileGroupNameExists(name))
                throw new ArgumentException($"A file group with the name '{name}' already exists.");

            var newGroup = new FileGroup(name, filePaths);
            _fileGroups.Add(newGroup);
            SaveFileGroups();
            return newGroup;
        }

        public List<FileGroup> GetAllFileGroups()
        {
            return new List<FileGroup>(_fileGroups);
        }

        
        public FileGroup GetFileGroupByGuid(Guid guid)
        {
            return _fileGroups.FirstOrDefault(fg => fg.Id == guid);
        }

        public FileGroup GetFileGroupByName(string name)
        {
            return _fileGroups.FirstOrDefault(fg => fg.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public bool UpdateFileGroup(Guid guid, string newName, List<string> newFilePaths)
        {
            var group = GetFileGroupByGuid(guid);
            if (group == null) return false;

            if (!string.IsNullOrEmpty(newName) && newName != group.Name)
            {
                if (FileGroupNameExists(newName))
                    throw new ArgumentException($"A file group with the name '{newName}' already exists.");
                group.Name = newName;
            }

            if (newFilePaths != null)
            {
                group.FilePaths = newFilePaths;
            }

            group.LastModifiedAt = DateTime.UtcNow;
            SaveFileGroups();
            return true;
        }

        public bool UpdateAllFileGroups(List<FileGroup> updatedGroups)
        {
            _fileGroups = updatedGroups;
            SaveFileGroups();
            return true;
        }

        public bool DeleteFileGroup(Guid guid)
        {
            var group = GetFileGroupByGuid(guid);
            if (group == null) return false;

            _fileGroups.Remove(group);
            SaveFileGroups();
            return true;
        }

        public bool AddFilesToGroup(Guid guid, List<string> filePaths)
        {
            var group = GetFileGroupByGuid(guid);
            if (group == null) return false;

            foreach (var path in filePaths)
            {
                group.AddFile(path);
            }

            SaveFileGroups();
            return true;
        }

        public bool RemoveFilesFromGroup(Guid guid, List<string> filePaths)
        {
            var group = GetFileGroupByGuid(guid);
            if (group == null) return false;

            bool anyRemoved = false;
            foreach (var path in filePaths)
            {
                if (group.RemoveFile(path))
                    anyRemoved = true;
            }

            if (anyRemoved)
                SaveFileGroups();

            return anyRemoved;
        }

        private void SaveFileGroups()
        {
            var json = JsonConvert.SerializeObject(_fileGroups, Formatting.Indented);
            File.WriteAllText(_storageFilePath, json);
        }

        private void LoadFileGroups()
        {
            if (File.Exists(_storageFilePath))
            {
                var json = File.ReadAllText(_storageFilePath);
                try
                {
                    _fileGroups = JsonConvert.DeserializeObject<List<FileGroup>>(json);
                }
                catch (Exception e)
                {

                }
            }
        }

        public bool FileGroupNameExists(string name)
        {
            return _fileGroups.Any(fg => fg.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private bool ValidateFilePaths(List<string> filePaths)
        {
            // Implement validation logic here
            // For example, check if files exist and are within the solution
            return filePaths.All(File.Exists);
        }

        public Dictionary<string, string> GetFileContents(Guid guid)
        {
            var group = GetFileGroupByGuid(guid);
            if (group == null) return null;

            var contents = new Dictionary<string, string>();
            foreach (var path in group.FilePaths)
            {
                if (File.Exists(path))
                {
                    contents[path] = File.ReadAllText(path);
                }
            }
            return contents;
        }

        public bool RenameFileGroup(Guid guid, string newName)
        {
            if (FileGroupNameExists(newName))
                throw new ArgumentException($"A file group with the name '{newName}' already exists.");

            var group = GetFileGroupByGuid(guid);
            if (group == null) return false;

            group.Name = newName;
            group.LastModifiedAt = DateTime.UtcNow;
            SaveFileGroups();
            return true;
        }

        public HashSet<string> GetAllUniquePaths()
        {
            return new HashSet<string>(_fileGroups.SelectMany(fg => fg.FilePaths));
        }

        public FileGroup MergeFileGroups(Guid guid1, Guid guid2, string newName)
        {
            var group1 = GetFileGroupByGuid(guid1);
            var group2 = GetFileGroupByGuid(guid2);
            if (group1 == null || group2 == null) return null;

            var mergedPaths = new HashSet<string>(group1.FilePaths);
            mergedPaths.UnionWith(group2.FilePaths);

            var newGroup = CreateFileGroup(newName, mergedPaths.ToList());
            DeleteFileGroup(guid1);
            DeleteFileGroup(guid2);

            return newGroup;
        }

        public FileGroup CloneFileGroup(Guid sourceGuid, string newName)
        {
            var sourceGroup = GetFileGroupByGuid(sourceGuid);
            if (sourceGroup == null) return null;

            return CreateFileGroup(newName, new List<string>(sourceGroup.FilePaths));
        }

        public List<FileGroup> GetFileGroupsContainingFile(string filePath)
        {
            return _fileGroups.Where(fg => fg.ContainsFile(filePath)).ToList();
        }

        public string ExportToJson()
        {
            return JsonConvert.SerializeObject(_fileGroups, Formatting.Indented);
        }

        public void ImportFromJson(string json)
        {
            var importedGroups = JsonConvert.DeserializeObject<List<FileGroup>>(json);
            foreach (var group in importedGroups)
            {
                if (!FileGroupNameExists(group.Name))
                {
                    _fileGroups.Add(group);
                }
            }
            SaveFileGroups();
        }

        internal List<FileGroup> GetSelectedFileGroups()
        {
            return _fileGroups.Where(fg => fg.Selected).ToList();
        }
    }
}
