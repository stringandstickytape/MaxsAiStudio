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
        
        // Azure DevOps Tools
        public const string AZURE_DEV_OPS_SEARCH_WIKI_TOOL_GUID = "a8b9c0d1-e2f3-4567-8901-23456789abcd";
        public const string AZURE_DEV_OPS_GET_WIKI_PAGE_CONTENT_TOOL_GUID = "e2f3a4b5-c6d7-e8f9-a0b1-c2d3e4f5a6b7";
        public const string AZURE_DEV_OPS_GET_WIKI_PAGES_TOOL_GUID = "f3a4b5c6-d7e8-9f0a-1b2c-3d4e5f6a7b8c";
        public const string AZURE_DEV_OPS_CREATE_OR_UPDATE_WIKI_PAGE_TOOL_GUID = "d5e6f7a8-b9c0-1234-5678-90abcdef5678";
        public const string AZURE_DEV_OPS_CREATE_OR_UPDATE_WIKI_PAGE_VIA_LOCAL_TOOL_GUID = "b7c8d9e0-f1a2-3456-7890-bcdef1234567";
    }
}