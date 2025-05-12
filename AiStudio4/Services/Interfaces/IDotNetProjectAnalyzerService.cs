// AiStudio4/Services/Interfaces/IDotNetProjectAnalyzerService.cs
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.Services.Interfaces
{
    public interface IDotNetProjectAnalyzerService
    {
        /// <summary>
        /// Analyzes a C# project file (.csproj) and retrieves a structured list of namespaces, classes, and methods.
        /// </summary>
        /// <param name="projectPath">The absolute path to the .csproj file.</param>
        /// <returns>
        /// A dictionary where the key is the namespace name (or "[Global Namespace]" for types outside a namespace)
        /// and the value is another dictionary. This inner dictionary's key is the class name, and the value is a list
        /// of method names declared within that class.
        /// Returns an empty dictionary if the project cannot be loaded or contains no analyzable documents.
        /// </returns>
        /// <exception cref="System.IO.FileNotFoundException">Thrown if the project file does not exist.</exception>
        List<FileWithMembers> AnalyzeProjectFiles(string projectPath);
    }
}