# 5. Key Features in Detail

## 5.1 AI Model Integration

### 5.1.1 Supported Models and Providers
AiStudio4 supports:
*   **OpenAI:** Models like GPT-3.5, GPT-4, etc. Requires OpenAI API key. (Uses `NetOpenAi` service type).
*   **Anthropic Claude:** Models like Claude 3 Opus, Sonnet, Haiku. Requires Anthropic API key. (Uses `Claude` service type).
*   **Google Gemini:** Models like Gemini Pro. Requires Google AI Studio API key. (Uses `Gemini` service type).
*   **Ollama:** Any model hosted by a local Ollama instance that exposes an OpenAI-compatible API. Configure as an OpenAI provider, pointing the URL to your Ollama instance (e.g., `http://localhost:11434/v1`). (Uses `NetOpenAi` service type).
*   **Model Context Protocol (MCP):** Connect to any AI service or tool provider that adheres to the MCP standard.

### 5.1.2 Configuring Service Providers
(See [Section 2.3.2](02-getting-started.md#232-configuring-ai-service-providers))

#### Charging Strategies
Each service provider can be configured with a charging strategy that determines how token costs are calculated:
*   **No Caching (Standard):** Basic input/output token pricing without any caching considerations
*   **Claude Caching Model:** Applies Claude's caching multipliers (1.25x for cache creation, 0.1x for cache reads)
*   **OpenAI Caching Model:** Applies OpenAI's caching multipliers (1.0x for cache creation, 0.25x for cache reads)
*   **Gemini Caching Model:** Applies Gemini's caching multipliers (0.25x for cache reads) and supports tiered pricing based on token boundaries

The charging strategy affects how the system calculates costs for AI requests, taking into account factors like prompt caching and tiered pricing models offered by different providers.

### 5.1.3 Managing AI Models
(See [Section 2.3.3](02-getting-started.md#233-adding-ai-models))
*   **Editing Models:** In Settings > Models, click the pencil icon on a model card.
*   **Deleting Models:** Click the trash icon.
*   **Favorite Models:** Click the star icon to mark a model as a favorite.
*   **Tiered Pricing:** Models can be configured with tiered pricing for providers that charge different rates based on token usage thresholds. This is particularly useful for models like Gemini 1.5 Pro that have different pricing above and below 128K tokens. When tiered pricing is enabled, you can specify:
    *   A token boundary where pricing changes
    *   Different input and output prices for requests above and below the boundary
    *   The system automatically calculates costs based on the total token count and applies the appropriate pricing tier

### 5.1.4 Model-Specific Features
When adding or editing a model, you can configure:
*   **Supports Prefill:** (Claude-specific) Some models allow "prefilling" the assistant's response to guide generation.
*   **Requires 1.0 Temperature:** Some models perform best or only work correctly with a temperature of 1.0. Check this if applicable.
*   **Reasoning Effort:** (For some OpenAI models) Can be set to `low`, `medium`, or `high` to influence model processing.
*   **Text-to-Speech (TTS):** If the model is a TTS model (e.g., some Gemini models), check "Enable Text-to-Speech (TTS)" and provide the `TTS VoiceName` (e.g., "Kore").

### 5.1.5 Temperature Control
You can easily adjust the AI's creativity/randomness via a slider in the Input Bar. This setting directly influences the 'temperature' parameter sent with requests to the AI model.

### 5.1.6 Top P (Nucleus Sampling) Control
AiStudio4 provides a "Top P" slider in the Input Bar, allowing fine-grained control over the AI's token selection process during generation. This setting adjusts the "Top P" or "Nucleus Sampling" parameter sent to compatible AI models.

*   **What is Top P?** Top P, also known as nucleus sampling, is a technique used in language models to control the randomness and diversity of generated text. Instead of considering all possible next words, the model considers only the smallest set of words whose cumulative probability exceeds a certain threshold (the "P" value).
*   **Effect on Generation:**
    *   **Higher Top P values (e.g., 0.9 to 1.0):** The model considers a larger, more diverse set of words. This can lead to more creative, varied, and sometimes unexpected outputs. A value of 1.0 typically means all words are considered (though behavior can vary by model API).
    *   **Lower Top P values (e.g., 0.1 to 0.5):** The model focuses on a smaller, more probable set of words. This results in more deterministic, focused, and often safer or more predictable outputs.
*   **How to Use:**
    *   The Top P slider is located in the Input Bar, typically next to the Temperature control.
    *   The slider ranges from **0.0 to 1.0**.
    *   The default value is **0.9**.
    *   Adjust the slider to experiment with different levels of output diversity. It can be used in conjunction with Temperature for more nuanced control over the AI's responses.

## 5.2 Tool System

### 5.2.1 Overview of Built-in Tools
AiStudio4 includes a variety of built-in tools. Click on a tool name to learn more about its usage, parameters, and examples.

#### Core Tools
*   [CreateNewFileTool](tools/create-new-file-tool.md)
*   [DeleteFileTool](tools/delete-file-tool.md)
*   [DirectoryTreeTool](tools/directory-tree-tool.md)
*   [FileRegExSearch](tools/file-reg-ex-search.md)
*   [FileSearchTool](tools/file-search-tool.md)
*   [FindAndReplaceTool](tools/find-and-replace-tool.md)
*   [InfoRequestTool](tools/info-request-tool.md)
*   [LaunchUrlTool](tools/launch-url-tool.md)
*   [ModifyFilesTool](tools/modify-files-tool.md)
*   [ReadFilesTool](tools/read-files-tool.md)
*   [ReadPartialFilesTool](tools/read-partial-files-tool.md)
*   [ReadDatabaseSchemaTool](tools/read-database-schema-tool.md)
*   [RecordMistakeTool](tools/record-mistake-tool.md)
*   [RenameFileTool](tools/rename-file-tool.md)
*   [ReplaceFileTool](tools/replace-file-tool.md)
*   [RetrieveTextFromUrlTool](tools/retrieve-text-from-url-tool.md)
*   [RunDuckDuckGoSearchTool](tools/run-duck-duck-go-search-tool.md)
*   [StopTool](tools/stop-tool.md)
*   [ThinkAndContinueTool](tools/think-and-continue-tool.md)
*   [ThinkAndAwaitUserInputTool](tools/think-and-await-user-input-tool.md)
*   [PresentResultsAndAwaitUserInputTool](tools/present-results-and-await-user-input-tool.md)

#### Azure DevOps Tools
*   [AzureDevOpsGetCommitDiffsTool](tools/azure-dev-ops-get-commit-diffs-tool.md)
*   [AzureDevOpsGetCommitsTool](tools/azure-dev-ops-get-commits-tool.md)
*   [AzureDevOpsGetItemContentTool](tools/azure-dev-ops-get-item-content-tool.md)
*   [AzureDevOpsGetPullRequestByIdTool](tools/azure-dev-ops-get-pull-request-by-id-tool.md)
*   [AzureDevOpsGetPullRequestChangesTool](tools/azure-dev-ops-get-pull-request-changes-tool.md)
*   [AzureDevOpsGetPullRequestIterationsTool](tools/azure-dev-ops-get-pull-request-iterations-tool.md)
*   [AzureDevOpsGetPullRequestsTool](tools/azure-dev-ops-get-pull-requests-tool.md)
*   [AzureDevOpsGetPullRequestThreadsTool](tools/azure-dev-ops-get-pull-request-threads-tool.md)
*   [AzureDevOpsGetRepositoriesTool](tools/azure-dev-ops-get-repositories-tool.md)
*   [AzureDevOpsGetWikiPageContentTool](tools/azure-dev-ops-get-wiki-page-content-tool.md)
*   [AzureDevOpsGetWikiPagesTool](tools/azure-dev-ops-get-wiki-pages-tool.md)
*   [AzureDevOpsGetWorkItemCommentsTool](tools/azure-dev-ops-get-work-item-comments-tool.md)
*   [AzureDevOpsGetWorkItemsTool](tools/azure-dev-ops-get-work-items-tool.md)
*   [AzureDevOpsGetWorkItemUpdatesTool](tools/azure-dev-ops-get-work-item-updates-tool.md)
*   [AzureDevOpsQueryWorkItemsTool](tools/azure-dev-ops-query-work-items-tool.md)

#### Git Tools
*   [GitCommitTool](tools/git-commit-tool.md)
*   [GitLogTool](tools/git-log-tool.md)

#### GitHub Tools
*   [GitHubGetContentTool](tools/git-hub-get-content-tool.md)
*   [GitHubListContentsTool](tools/git-hub-list-contents-tool.md)
*   [GitHubRepoInfoTool](tools/git-hub-repo-info-tool.md)
*   [GitHubSearchCodeTool](tools/git-hub-search-code-tool.md)
*   [GitHubListIssuesTool](tools/git-hub-list-issues-tool.md)
*   [GitHubGetIssueTool](tools/git-hub-get-issue-tool.md)
*   [GitHubCreateIssueTool](tools/git-hub-create-issue-tool.md)
*   [GitHubUpdateIssueTool](tools/git-hub-update-issue-tool.md)
*   [GitHubCreateIssueCommentTool](tools/git-hub-create-issue-comment-tool.md)
*   [GitHubListIssueCommentsTool](tools/git-hub-list-issue-comments-tool.md)
*   [GitHubUpdatePullRequestTool](tools/github-update-pull-request-tool.md)
*   [GitHubListPullRequestsTool](tools/github-list-pull-requests-tool.md)

#### Sentry Tools
*   [SentryTool](tools/sentry-tool.md)

#### Vite Tools
*   [CheckNodeVersionTool](tools/check-node-version-tool.md)
*   [GetViteProjectInfoTool](tools/get-vite-project-info-tool.md)
*   [InstallVitePluginTool](tools/install-vite-plugin-tool.md)
*   [ModifyViteConfigTool](tools/modify-vite-config-tool.md)
*   [NpmCreateViteTool](tools/npm-create-vite-tool.md)
*   [NpmInstallTool](tools/npm-install-tool.md)
*   [NpmRunScriptTool](tools/npm-run-script-tool.md)
*   [OpenBrowserTool](tools/open-browser-tool.md)
*   [StartViteDevServerTool](tools/start-vite-dev-server-tool.md)

#### YouTube Tools
*   [YouTubeSearchTool](tools/you-tube-search-tool.md)

#### Google Search Tools
*   [GoogleCustomSearchApiTool](tools/google-custom-search-api-tool.md)
*   [GeminiGoogleSearchTool](tools/gemini-google-search-tool.md)

### 5.2.2 Model Context Protocol (MCP)
AiStudio4 can connect to external tool servers that implement the Model Context Protocol.
1.  **Manage MCP Servers:**
    *   Command Bar (`Ctrl+K`): Type "Manage MCP Servers".
2.  In the "MCP Servers" modal:
    *   Click "New Server".
    *   **Name & Description:** For your reference.
    *   **Command:**
        *   For CLI-based MCP servers (StdIO checked): The executable path (e.g., `python`, `my_mcp_server.exe`).
        *   For HTTP/SSE-based MCP servers (StdIO unchecked): The server's base URL (e.g., `http://localhost:8000`).
    *   **Arguments:** (For CLI only) Command-line arguments for the executable.
    *   **StdIo:** Check if the server communicates over standard input/output (typical for local scripts). Uncheck for HTTP/SSE based servers.
    *   **Enabled:** Toggle whether this server is active.
    *   **Categories:** Assign categories for organization.
    *   **Select Tools to Expose:** If the server is running and tools can be fetched, you can select which of its tools are exposed to the AI. If none are selected, all tools from that server are exposed.
3.  Enabled MCP servers and their selected tools will be available for the AI to use.

### 5.2.3 Creating, Importing & Exporting Custom Tools
(Advanced) Users can define their own tools by providing a JSON schema. Tools can be organized into categories.
1.  In the Tool Library modal, click "New Tool".
2.  Provide a Name, Description, Categories, and the JSON schema defining the tool's parameters and function.
3.  **Schema Type:**
    *   `function`: Standard tool definition.
    *   `template`: If the tool's "schema" is a text template, the AI will fill it in.
    *   `custom`: For other custom behaviors (not typically user-defined).
4.  **Output Filetype:** If the tool generates a specific file type (e.g., `theme`), specify it here. This can enable special handling (like installing a theme).
5.  The Tool Library also supports importing and exporting tool definitions.

### 5.2.4 Tool Extra Properties
Some built-in tools (e.g., `FileSearchTool`, `DirectoryTreeTool`, `SentryTool`) have "Extra Properties" that can be configured to customize their behavior without changing their core code.
1.  In the Tool Library modal, edit a built-in tool.
2.  The "Extra Properties" section will show available key-value pairs.
    *   For CSV lists (e.g., `excludedFileExtensions (CSV)`), enter comma-separated values.
    *   For Sentry, you'd configure `orgSlug`, `apiToken`, etc.
3.  Changes are saved and persist in `%APPDATA%/AiStudio4/builtinToolExtraProps.json`.

## 5.3 UI and Customization

### 5.3.1 Theming
*   **Browse & Apply:** Command Bar (`Ctrl+K`) > "Theme Library". Select a theme and click "Apply Theme".
*   **AI-Created Themes:**
    1.  Create a custom tool with `Output Filetype` set to `theme`.
    2.  Click "Use Theme Schema" to populate its schema with all themeable properties.
    3.  Activate this tool.
    4.  Ask the AI to generate a theme (e.g., "Create a cyberpunk theme with neon green accents").
    5.  When the AI uses your theme tool, the output will be a theme JSON.
    6.  In the AI's response (Chat View), the code block containing the theme JSON will have an "Install Theme" button. Click it to add the theme to your library.
*   **Import Themes:** In the Theme Library, you can import theme JSON files.
*   **Advanced:** The theming engine supports dynamic font loading from CDNs specified in themes. The `ThemeManager` discovers component-specific themeable properties.

### 5.3.2 Command Bar
*   Access by pressing `Ctrl+K` (or `Cmd+K`).
*   Type to search for commands, settings, prompts, tools, models, etc.
*   Use arrow keys to navigate and `Enter` to execute.
*   Pinned commands (see below) also appear here.

### 5.3.3 Pinned Shortcuts
*   A customizable bar in the App Header for one-click access to frequently used commands.
*   **Pinning:** In the Command Bar, hover over a command. A "Pin" icon will appear. Click it to add the command to your Pinned Shortcuts.
*   **Unpinning:** Right-click a pinned shortcut button in the Header.
*   **Reordering:** Drag and drop pinned shortcut buttons in the Header to reorder them.
*   **Renaming:** Middle-click a pinned shortcut button to open a dialog to rename it and change its icon.

### 5.3.4 Slash Commands
In the Input Bar:
*   Type `/` to open a dropdown.
*   Start typing the name of a User Prompt or a file path from your current `ProjectPath`.
*   **User Prompts:** Selecting a User Prompt inserts its content.
*   **File Paths:**
    *   Selecting a file path inserts the relative path into the input.
    *   `Shift+Click` on a file path in the dropdown will attach the file instead of inserting its path.

### 5.3.5 Appearance Settings
*   Command Bar (`Ctrl+K`) > "Appearance Settings".
*   **Font Size:** Adjust the global font size for the application.
*   **Dark Mode:** (Default) AiStudio4 primarily uses a dark theme aesthetic. Theming allows full customization.
*   **Panel Size Persistence:** Panel sizes are automatically saved and restored between sessions with debounced saving to prevent performance issues.

## 5.4 Project Integration

### 5.4.1 .NET Project Analysis
*   From the application menu: `Project > Analyze .NET Projects`.
*   This tool scans your current `ProjectPath` for C# project files (`.csproj`) and source files (`.cs`).
*   It extracts namespace, class, and member (methods, properties, fields, etc.) information.
*   The results are saved to a `DotNetProjectAnalysis.txt` file in your project root.
*   This file can then be used as context for AI tasks, for example, by attaching it to a prompt or having the AI read it using the `ReadFiles` tool.

### 5.4.2 Project Source Code Packaging
*   **Pack Project Source Code:** From the application menu: `Project > Pack Project Source Code`.
    *   This tool creates a comprehensive XML package containing the source code of your project.
    *   It respects `.gitignore` rules.
    *   You can configure which file types to include (`Project > Set Packer Include File Types...`), specific filenames/patterns to exclude (`Project > Set Packer Exclude Filenames...`), and specific folder names to exclude (`Project > Set Packer Exclude Folder Names...`).
    *   The output XML includes the directory structure and the content of each included file. This is useful for providing a large codebase snapshot to an AI that might have context window limitations or no direct file system access.
    *   When you first pack a project, the output file path is saved for future use.
*   **Repack Project Source Code:** From the application menu: `Project > Repack Project Source Code`.
    *   This feature allows you to quickly re-run the packaging process using the same output file path as your last pack operation.
    *   No file dialog is shown - the project is automatically packed to the previously used location.
    *   This is useful for iterative development workflows where you frequently need to update the packaged project snapshot.
    *   If you haven't used "Pack Project Source Code" first, you'll be prompted to do so to establish an initial output file location.

## 5.5 Backend and Core Functionality

### 5.5.1 Real-time Communication
WebSockets are extensively used for:
*   Streaming AI responses.
*   Live conversation updates across different UI components.
*   Displaying status messages from the backend.

## 5.6 Integrations

### 5.6.1 Azure DevOps Wiki Sync
AiStudio4 provides automatic synchronization between Azure DevOps wiki pages and system prompts, enabling teams to maintain centralized documentation that automatically updates AI behavior.

*   **Purpose:** Allows teams to maintain system prompts in Azure DevOps wikis and automatically sync them to AiStudio4, ensuring consistent AI behavior across team members and keeping prompts up-to-date with centralized documentation.
*   **How It Works:**
    1.  Configure the sync settings to point to a specific Azure DevOps wiki page
    2.  Enable the sync feature in the application menu
    3.  On each application startup, AiStudio4 automatically fetches the latest content from the specified wiki page
    4.  If the target system prompt exists, its content is updated; if not, a new system prompt is created
    5.  Sync only occurs when the wiki content has actually changed
*   **Configuration Requirements:**
    *   Azure DevOps Organization name
    *   Azure DevOps Project name
    *   Wiki Identifier (typically `ProjectName.wiki`)
    *   Wiki Page Path (e.g., `/Instructions/MasterSystemPrompt`)
    *   Target System Prompt selection
    *   Azure DevOps Personal Access Token (PAT) with wiki read permissions
*   **Setup Process:**
    1.  Configure your Azure DevOps PAT: `File > Settings > Set Azure DevOps PAT...`
    2.  Configure wiki sync: `File > Wiki Sync > Configure Wiki Sync...`
    3.  Enable the sync: `File > Wiki Sync > Enable Wiki Sync` (checked)
    4.  Restart the application for changes to take effect
*   **Use Cases:**
    *   Centralized team prompt management
    *   Automatic updates to AI behavior based on project documentation
    *   Consistent system prompts across development team members
    *   Integration with existing Azure DevOps workflows

### 5.6.2 Google AI Studio Integration
AiStudio4 offers integration with Google AI Studio via Google Drive, allowing for a seamless exchange of conversation data.

*   **Purpose:** Facilitates users who work across both AiStudio4 and Google AI Studio or wish to migrate/backup conversations between these platforms.
*   **Import Process:** 
    1.  AiStudio4 accesses your Google Drive (requires authorization).
    2.  It looks for JSON files within a dedicated "Google AI Studio" folder.
    3.  Users select which files to import.
    4.  For each file, users can then select specific messages to include.
    5.  The selected data is converted from Google AI Studio format and saved as a new AiStudio4 conversation.
*   **Export Process:**
    1.  The main branch of the most recently modified AiStudio4 conversation is selected.
    2.  It's converted to Google AI Studio's JSON format.
    3.  Users are prompted for a filename.
    4.  The resulting file is uploaded to the "Google AI Studio" folder in your Google Drive.
*   **Setup:** This feature requires a `credentials.json` file (OAuth 2.0 Desktop App credential from Google Cloud Console) to be placed in `%APPDATA%\AiStudio4\Config\`.