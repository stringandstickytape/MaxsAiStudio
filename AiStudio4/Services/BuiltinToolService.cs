using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using System;
using System.Collections.Generic;

namespace AiStudio4.Services
{
    public class BuiltinToolService : IBuiltinToolService
    {
        public List<Tool> GetBuiltinTools()
        {
            // Define default tools here with fixed GUIDs
            return new List<Tool>
            {
                new Tool
                {
                    Guid = "a1b2c3d4-e5f6-7890-1234-567890abcdef", // Fixed GUID for CodeDiff
                    Description = "Allows you to specify edits, file creations and deletions",
                    Name= "CodeDiff",
                    Schema = @"{
  ""name"": ""CodeDiff"",
  ""description"": ""Allows you to specify an array of changes to make up a complete code or ASCII file diff, and includes a description of those changes. You must NEVER double-escape content in this diff."",
  ""input_schema"": {
    ""type"": ""object"",
    ""properties"": {
      ""changeset"": {
        ""type"": ""object"",
        ""description"": """",
        ""properties"": {
          ""description"": {
            ""type"": ""string"",
            ""description"": ""A description of this changeset""
          },
          ""files"": {
            ""type"": ""array"",
            ""description"": """",
            ""items"": {
              ""type"": ""object"",
              ""properties"": {
                ""path"": {
                  ""type"": ""string"",
                  ""description"": ""The original filename and ABSOLUTE path where the changes are to occur""
                },
                ""changes"": {
                  ""type"": ""array"",
                  ""description"": """",
                  ""items"": {
                    ""type"": ""object"",
                    ""properties"": {
                      ""lineNumber"": {
                        ""type"": ""integer"",
                        ""description"": ""The line number where the change starts, adjusted for previous changes in this changeset (not used for file creation, replacement, renaming, or deletion)""
                      },
                      ""change_type"": {
                        ""type"": ""string"",
                        ""description"": ""The type of change that occurred"",
                        ""enum"": [
                          ""addToFile"",
                          ""deleteFromFile"",
                          ""modifyFile"",
                          ""createnewFile"",
                          ""replaceFile"",
                          ""renameFile"",
                          ""deleteFile""
                        ]
                      },
                      ""oldContent"": {
                        ""type"": ""string"",
                        ""description"": ""The lines that were removed or modified (ignored for createFile, replaceFile, renameFile, and deleteFile)""
                      },
                      ""newContent"": {
                        ""type"": ""string"",
                        ""description"": ""The lines that were added, modified, or created (for replaceFile, this contains the entire new file content; for renameFile, this contains the new file path)""
                      },
                      ""description"": {
                        ""type"": ""string"",
                        ""description"": ""A human-readable explanation of this specific change""
                      }
                    },
                    ""required"": [
                      ""change_type"",
                      ""oldContent"",
                      ""newContent""
                    ]
                  }
                }
              },
              ""required"": [
                ""path"",
                ""changes""
              ]
            }
          }
        },
        ""required"": [
          ""description"",
          ""files""
        ]
      }
    },
    ""required"": [
      ""changeset""
    ]
  }
}",
                    Categories  = new List<string>{ "Development" }, // Assign appropriate category
                    Filetype = string.Empty, // Ensure initialized
                    LastModified = DateTime.UtcNow
                },
                new Tool
                {
                    Guid = "b2c3d4e5-f6a7-8901-2345-67890abcdef01", // Fixed GUID for Stop
                    Name= "Stop",
                    Description = "A tool which allows you to indicate that all outstanding tasks are completed, or you cannot proceed any further",
                    Schema = @"{
  ""name"": ""Stop"",
  ""description"": ""A tool which allows you to indicate that all outstanding tasks are completed"",
  ""input_schema"": {
    ""type"": ""object"",
    ""properties"": {
      ""param"": {
        ""type"": ""string"",
        ""description"": ""Information to the user goes here""
      }
    }
  }
}",
                    Categories  = new List<string>{ "Development" },
                    Filetype = string.Empty, // Ensure initialized
                    LastModified = DateTime.UtcNow
                }
                // Add more builtin tools here as needed, using fixed GUIDs
            };
        }
    }
}
