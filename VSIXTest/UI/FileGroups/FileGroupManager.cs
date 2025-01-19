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

        public FileGroup CreateFileGroup(string name, List<string> filePaths, string sourceSolutionPath)
        {
            if (FileGroupNameExists(name))
                throw new ArgumentException($"A file group with the name '{name}' already exists.");

            var newGroup = new FileGroup(name, filePaths, sourceSolutionPath);
            _fileGroups.Add(newGroup);
            SaveFileGroups();
            return newGroup;
        }

        public void DeselectAllFileGroups()
        {
            foreach (var fileGroup in _fileGroups)
            {
                fileGroup.Selected = false;
            }
            SaveFileGroups();
        }

        public List<FileGroup> GetAllFileGroups(string sourceSolutionPath)
        {
            return _fileGroups.Where(x => x.SourceSolutionPath == sourceSolutionPath).ToList();
        }

        public bool UpdateAllFileGroups(List<FileGroup> updatedGroups, string sourceSolutionPath)
        {
            var groupsForOtherSolutions = _fileGroups.Where(x => x.SourceSolutionPath != sourceSolutionPath).ToList();

            _fileGroups = updatedGroups;

            foreach(var fileGroup in _fileGroups)
            {
                var newFilePaths = new List<string>();
                foreach (var x in fileGroup.FilePaths)
                {
                    if (File.Exists(x))
                        newFilePaths.Add(x);

                }
                fileGroup.FilePaths = newFilePaths;
            }

            _fileGroups.AddRange(groupsForOtherSolutions);

            SaveFileGroups();
            return true;
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
                catch (Exception)
                {
                    // Handle deserialization error
                    _fileGroups = new List<FileGroup>();
                }
            }
        }

        public bool FileGroupNameExists(string name)
        {
            return _fileGroups.Any(fg => fg.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public HashSet<string> GetAllUniquePaths()
        {
            return new HashSet<string>(_fileGroups.SelectMany(fg => fg.FilePaths));
        }

        internal List<FileGroup> GetSelectedFileGroups()
        {
            return _fileGroups.Where(fg => fg.Selected).ToList();
        }
    }
}