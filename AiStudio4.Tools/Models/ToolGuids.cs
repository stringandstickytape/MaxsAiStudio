namespace AiStudio4.Tools.Models
{
    /// <summary>
    /// Contains unique GUID constants for tools in the shared library.
    /// These GUIDs are used to identify and reference specific tools throughout the system.
    /// </summary>
    public static class ToolGuids
    {
        // YouTube Tools
        public const string YOUTUBE_SEARCH_TOOL_GUID = "d1e2f3a4-b5c6-7890-1234-567890abcdef10";

        // Azure DevOps Tools (for future migration)
        public const string AZURE_DEV_OPS_SEARCH_WIKI_TOOL_GUID = "a2b3c4d5-e6f7-0891-2345-678901234567";
        
        // File Operation Tools (for future migration)
        public const string CREATE_NEW_FILE_TOOL_GUID = "1f2e3d4c-5b6a-7098-9876-543210fedcba";
        public const string DELETE_FILE_TOOL_GUID = "2a3b4c5d-6e7f-8901-2345-678901234567";
        public const string READ_FILES_TOOL_GUID = "3b4c5d6e-7f80-9012-3456-789012345678";
        public const string RENAME_FILE_TOOL_GUID = "4c5d6e7f-8091-0123-4567-890123456789";
    }
}