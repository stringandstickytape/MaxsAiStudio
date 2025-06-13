// Core/Models/FileSystemChangedEventArgs.cs



namespace AiStudio4.Core.Models
{
    /// <summary>
    /// Event arguments for file system change events
    /// </summary>
    public class FileSystemChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the list of directories in the project
        /// </summary>
        public IReadOnlyList<string> Directories { get; }

        /// <summary>
        /// Gets the list of files in the project
        /// </summary>
        public IReadOnlyList<string> Files { get; }

        /// <summary>
        /// Creates a new instance of FileSystemChangedEventArgs
        /// </summary>
        /// <param name="directories">The list of directories</param>
        /// <param name="files">The list of files</param>
        public FileSystemChangedEventArgs(IReadOnlyList<string> directories, IReadOnlyList<string> files)
        {
            Directories = directories;
            Files = files;
        }
    }
}
