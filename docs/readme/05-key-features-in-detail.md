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

### 5.1.3 Managing AI Models
(See [Section 2.3.3](02-getting-started.md#233-adding-ai-models))
*   **Editing Models:** In Settings > Models, click the pencil icon on a model card.
*   **Deleting Models:** Click the trash icon.
*   **Favorite Models:** Click the star icon to mark a model as a favorite.

### 5.1.4 Model-Specific Features
When adding or editing a model, you can configure:
*   **Supports Prefill:** (Claude-specific) Some models allow "prefilling" the assistant's response to guide generation.
*   **Requires 1.0 Temperature:** Some models perform best or only work correctly with a temperature of 1.0. Check this if applicable.
*   **Reasoning Effort:** (For some OpenAI models) Can be set to `low`, `medium`, or `high` to influence model processing.
*   **Text-to-Speech (TTS):** If the model is a TTS model (e.g., some Gemini models), check "Enable Text-to-Speech (TTS)" and provide the `TTS VoiceName` (e.g., "Kore").

### 5.1.5 Temperature Control
You can easily adjust the AI's creativity/randomness via a slider in the Input Bar. This setting directly influences the 'temperature' parameter sent with requests to the AI model.

## 5.2 Tool System

### 5.2.1 Overview of Built-in Tools
AiStudio4 includes a variety of built-in tools, categorized for ease of use. Here are some key examples:

*   **File System (`MaxCode` Category):**
    *   `ReadFiles`: Reads content of specified files.
    *   `ReadPartialFiles`: Reads specific line ranges from files.
    *   `FileRegExSearch`: Searches files for lines matching regular expressions.
    *   `FileSearch`: Searches files for specific text terms.
    *   `DirectoryTree`: Generates a tree view of a directory.
    *   `FindAndReplace`: Performs text replacements across files.
    *   `CreateNewFile`: Creates a new file with specified content.
    *   `DeleteFile`: Deletes a file.
    *   `ModifyFiles`: Applies complex modifications to existing files (can use secondary AI for merging, includes handling merge failures).
    *   `RenameFile`: Renames a file.
    *   `ReplaceFile`: Replaces the entire content of a file.
    *   `GitCommit`: Commits specified files with a message (requires project to be a Git repo).
    *   `ReadDatabaseSchema`: (SQL Server) Reads table or column information from a hardcoded 'SHEFFIELD' database on localhost.
*   **Web & Information Retrieval (`Development` Category):**
    *   `RetrieveTextFromUrl`: Scrapes text content from web pages.
    *   `RunDuckDuckGoSearch`: Performs a DuckDuckGo web search (Note: can be prone to bot detection).
    *   `YouTubeSearch`: Searches YouTube for videos.
    *   `LaunchUrl`: Opens URLs in the default browser.
*   **Developer Integrations:**
    *   **GitHub (`GitHub` Category):** Tools to get repo info, list contents, get file content, search code. Requires GitHub API Key.
    *   **Azure DevOps (`AzureDevOps` Category):** Tools to interact with work items, pull requests, repositories, commits, diffs, and wiki pages. Requires Azure DevOps PAT.
    *   **Sentry (`APITools` Category):** Tools to get organization details, project info, and issue lists. Requires Sentry API token and Org Slug configured in tool properties.
    *   **Vite (`Vite` Category):** Tools to create Vite projects, manage npm dependencies, run scripts, start/stop dev server, modify `vite.config.js`, check Node.js version. (Note: Some Vite commands may prompt for user confirmation before execution).
*   **Utility & Meta-Tools (`MaxCode` & `Development` Categories):**
    *   `Think`: Allows AI to "think out loud" or log its reasoning process.
    *   `Stop`: Signals completion of a multi-step task, terminating the tool loop. This tool is always available to the AI when other tools are active.
    *   `InfoRequest`: AI can request more information from the user.
    *   `RecordMistake`: Logs AI errors to `CommonAiMistakes.md` in the project root, which is then used to refine future AI interactions (includes AI-assisted consolidation).

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

## 5.4 Project Integration

### 5.4.1 .NET Project Analysis
*   From the application menu: `Project > Analyze .NET Projects`.
*   This tool scans your current `ProjectPath` for C# project files (`.csproj`) and source files (`.cs`).
*   It extracts namespace, class, and member (methods, properties, fields, etc.) information.
*   The results are saved to a `DotNetProjectAnalysis.txt` file in your project root.
*   This file can then be used as context for AI tasks, for example, by attaching it to a prompt or having the AI read it using the `ReadFiles` tool.

### 5.4.2 Project Source Code Packaging
*   From the application menu: `Project > Pack Project Source Code`.
*   This tool creates a comprehensive XML package containing the source code of your project.
*   It respects `.gitignore` rules.
*   You can configure which file types to include (`Project > Set Packer Include File Types...`) and specific filenames/patterns to exclude (`Project > Set Packer Exclude Filenames...`).
*   The output XML includes the directory structure and the content of each included file. This is useful for providing a large codebase snapshot to an AI that might have context window limitations or no direct file system access.

## 5.5 Backend and Core Functionality

### 5.5.1 Real-time Communication
WebSockets are extensively used for:
*   Streaming AI responses.
*   Live conversation updates across different UI components.
*   Displaying status messages from the backend.

## 5.6 Integrations

### 5.6.1 Google AI Studio Integration
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