using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools
{
    static class ToolGuids
    {
        public const string AZURE_DEV_OPS_GET_COMMIT_DIFFS_TOOL_GUID = "c4d8e7f6-5a3b-2c1d-9e8f-7a6b5c4d3e2f";
        public const string AZURE_DEV_OPS_GET_COMMITS_TOOL_GUID = "7c4d9e6b-2a1f-4d8e-9c7b-5a3e2f1d8c9b";
        public const string AZURE_DEV_OPS_GET_ITEM_CONTENT_TOOL_GUID = "3d7c9e5b-8f2a-4d1e-9c6b-7d8f5e3a2c1d";
        public const string AZURE_DEV_OPS_GET_PULL_REQUEST_BY_ID_TOOL_GUID = "6d4e8f2a-9c7b-4d3e-8a5f-1b2c3d4e5f6a";
        public const string AZURE_DEV_OPS_GET_PULL_REQUEST_CHANGES_TOOL_GUID = "9d4e7f2b-8c5a-4e3d-9f2b-1a3c5e7d9f2b";
        public const string AZURE_DEV_OPS_GET_PULL_REQUEST_ITERATIONS_TOOL_GUID = "4d7e6c5b-9a8f-4e3d-2c1b-7a6b5c4d3e2f";
        public const string AZURE_DEV_OPS_GET_PULL_REQUESTS_TOOL_GUID = "5b3e9c2a-7d8f-4e1a-9b6c-8d7f5e3a2c1b";
        public const string AZURE_DEV_OPS_GET_PULL_REQUEST_THREADS_TOOL_GUID = "7c4d8e9f-2a3b-4c5d-6e7f-8a9b0c1d2e3f";
        public const string AZURE_DEV_OPS_GET_REPOSITORIES_TOOL_GUID = "8a4c7e5d-9f2b-4d1e-8c6a-3b7f9e2d5c1a";
        public const string AZURE_DEV_OPS_GET_WIKI_PAGE_CONTENT_TOOL_GUID = "e2f3a4b5-c6d7-e8f9-a0b1-c2d3e4f5a6b7";
        public const string AZURE_DEV_OPS_GET_WIKI_PAGES_TOOL_GUID = "f3a4b5c6-d7e8-9f0a-1b2c-3d4e5f6a7b8c";
        public const string AZURE_DEV_OPS_GET_WORK_ITEM_COMMENTS_TOOL_GUID = "4d7e6c5b-9a8f-4e3d-2c1b-7a6b5c4d3e2f";
        public const string AZURE_DEV_OPS_GET_WORK_ITEMS_TOOL_GUID = "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d";
        public const string AZURE_DEV_OPS_GET_WORK_ITEM_UPDATES_TOOL_GUID = "3a7b9c5d-2e4f-8g6h-1i2j-3k4l5m6n7o8p";
        public const string AZURE_DEV_OPS_QUERY_WORK_ITEMS_TOOL_GUID = "2d4e6f8a-1c3b-5a7d-9e8f-7c6b5a4d3e2f";
        
        // ... existing GUIDs
        public const string SECOND_AI_OPINION_TOOL_GUID = "a1b2c3d4-e5f6-7890-1234-abcdef987654";

        // GitHub Tools
        public const string GITHUB_CREATE_ISSUE_COMMENT_TOOL_GUID = "e1f2a3b4-c5d6-e7f8-a9b0-e1f2a3b4c5d6";
        public const string GITHUB_CREATE_ISSUE_TOOL_GUID = "c1d2e3f4-a5b6-c7d8-e9f0-c1d2e3f4a5b6";
        public const string GITHUB_CREATE_PULL_REQUEST_TOOL_GUID = "a7b8c9d0-e1f2-3456-7890-abcdef123456";
        public const string GITHUB_GET_CONTENT_TOOL_GUID = "6172c3d4-e5f6-7890-1234-56789abcdef03";
        public const string GITHUB_GET_ISSUE_TOOL_GUID = "b1c2d3e4-f5a6-b7c8-d9e0-b1c2d3e4f5a6";
        public const string GITHUB_LIST_CONTENTS_TOOL_GUID = "6172c3d4-e5f6-7890-1234-56789abcdef02";
        public const string GITHUB_LIST_ISSUE_COMMENTS_TOOL_GUID = "f1a2b3c4-d5e6-f7a8-b9c0-f1a2b3c4d5e6";
        public const string GITHUB_LIST_ISSUES_TOOL_GUID = "a1b2c3d4-e5f6-7890-1234-a1b2c3d4e5f6";
        public const string GITHUB_LIST_PULL_REQUESTS_TOOL_GUID = "c8d9e0f1-a2b3-5678-9012-cdef34567890";

        // Git Tools
        public const string GIT_BRANCH_TOOL_GUID = "e5f6a7b8-c9d0-1234-5678-90abcdef1234";
        public const string GIT_COMMIT_TOOL_GUID = "c3d4e5f6-a7b8-9012-3456-7890abcdef12";
        public const string GIT_LOG_TOOL_GUID = "d4e5f6a7-b8c9-0123-4567-890abcdef123";
        public const string GIT_STATUS_TOOL_GUID = "f6a7b8c9-d0e1-2345-6789-0abcdef12345";
        
        // Sentry Tools
        public const string SENTRY_TOOL_GUID = "a1b2c3d4-e5f6-7890-1234-56789abcdef99";
        
        // Vite Tools
        public const string CHECK_NODE_VERSION_TOOL_GUID = "v1t3c4e5-f6a7-8901-2345-67890abcdef06";
        public const string GET_VITE_PROJECT_INFO_TOOL_GUID = "v1t3c4e5-f6a7-8901-2345-67890abcdef07";
        public const string INSTALL_VITE_PLUGIN_TOOL_GUID = "v1t3c4e5-f6a7-8901-2345-67890abcdef09";
        public const string MODIFY_VITE_CONFIG_TOOL_GUID = "v1t3c4e5-f6a7-8901-2345-67890abcdef08";
        public const string NPM_CREATE_VITE_TOOL_GUID = "v1t3c4e5-f6a7-8901-2345-67890abcdef01";
        public const string NPM_INSTALL_TOOL_GUID = "v1t3c4e5-f6a7-8901-2345-67890abcdef02";
        public const string NPM_RUN_SCRIPT_TOOL_GUID = "v1t3c4e5-f6a7-8901-2345-67890abcdef03";
        public const string OPEN_BROWSER_TOOL_GUID = "v1t3c4e5-f6a7-8901-2345-67890abcdef05";
        public const string START_VITE_DEV_SERVER_TOOL_GUID = "v1t3c4e5-f6a7-8901-2345-67890abcdef04";
        
        // Core Tools
        public const string CREATE_NEW_FILE_TOOL_GUID = "a1b2c3d4-e5f6-7890-1234-567890abcd01";
        public const string DELETE_FILE_TOOL_GUID = "a1b2c3d4-e5f6-7890-1234-567890abcd02";
        public const string DIRECTORY_TREE_TOOL_GUID = "b2c3d4e5-f6a7-8901-2345-67890abcdef04";
        public const string FILE_SEARCH_TOOL_GUID = "a1b2c3d4-e5f6-7890-1234-56789abcdef07";
        public const string FIND_AND_REPLACE_TOOL_GUID = "a1b2c3d4-e5f6-7890-1234-56789abcdef08";
        public const string LAUNCH_URL_TOOL_GUID = "a1b2c3d4-e5f6-7890-1234-56789abcdef04";
        public const string MODIFY_FILES_TOOL_GUID = "a1b2c3d4-e5f6-7890-1234-567890abcd43";
        public const string PRESENT_RESULTS_AND_AWAIT_USER_INPUT_TOOL_GUID = "b2c4d4e5-f6a7-8901-2345-67890abcdef05";
        public const string YOUTUBE_SEARCH_TOOL_GUID = "d1e2f3a4-b5c6-7890-1234-567890abcdef10";
        
        // Additional Core Tools
        public const string READ_DATABASE_SCHEMA_TOOL_GUID = "c3d4e5f6-a7b8-9012-3456-7890abcdef16";
        public const string READ_FILES_TOOL_GUID = "b2c3d4e5-f6a7-8901-2345-67890abcdef05";
        public const string READ_PARTIAL_FILES_TOOL_GUID = "e3f4a5b6-c7d8-9012-3456-78901bcdef12";
        public const string RECORD_MISTAKE_TOOL_GUID = "a1b2c3d4-e5f6-7890-1234-567890abcdef";
        public const string RENAME_FILE_TOOL_GUID = "a1b2c3d4-e5f6-7890-1234-567890abcd04";
        public const string REPLACE_FILE_TOOL_GUID = "a1b2c3d4-e5f6-7890-1234-567890abcd05";
        public const string RETRIEVE_TEXT_FROM_URL_TOOL_GUID = "c3d4e5f6-a7b8-9012-3456-7890abcdef08";
        public const string RUN_DUCK_DUCK_GO_SEARCH_TOOL_GUID = "d4e5f6g7-h8i9-j0k1-l2m3-n4o5p6q7r8s9";
        public const string STOP_TOOL_GUID = "b2c3d4e5-f6a7-8901-2345-67890abcdef01";
        public const string THINK_AND_AWAIT_USER_INPUT_TOOL_GUID = "b2c4d4e5-f6a7-8901-2345-67890abcdef04";
        public const string THINK_TOOL_GUID = "b2c3d4e5-f6a7-8901-2345-67890abcdef03";
        public const string GEMINI_GOOGLE_SEARCH_TOOL_GUID = "g3m1n1s3-a4r5-c6h7-8901-234567890abc";
        public const string GOOGLE_CUSTOM_SEARCH_API_TOOL_GUID = "g00g1e5e-a7c8-4d1f-9b2e-3c5d7f9a1b3c";
    }
}