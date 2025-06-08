# 6. Advanced Configuration

## 6.1 Backend Configuration (`appsettings.json`)
Located in the `AiStudio4` project directory (when running from source) or installation directory, this file contains basic backend settings.
*   `WebServer:Port`: Defines the port for the embedded Kestrel web server (default: `35005`).

## 6.2 User-Specific Settings (`%APPDATA%/AiStudio4/`)
Most user preferences and data are stored in JSON files within this directory on Windows.
*   **`settings.json`**: Main user settings.
    *   `ModelList`: Your configured AI models (now includes tiered pricing properties: `PriceBoundary`, `InputPriceAboveBoundary`, `OutputPriceAboveBoundary`).
    *   `ServiceProviders`: Your configured AI service providers (including encrypted API keys and `ChargingStrategy` property).
    *   `DefaultModelGuid`, `SecondaryModelGuid`: GUIDs of your default models.
    *   `Temperature`, `UsePromptCaching`, `StreamResponses`, `UseEmbeddings`, `EmbeddingModel`.
    *   `ProjectPath`: The **critical** path to your current project.
    *   `EncryptedYouTubeApiKey`, `EncryptedGitHubApiKey`, `EncryptedAzureDevOpsPAT`: Encrypted API keys.
    *   `CondaPath`: Path to your Conda environment.
    *   `AllowConnectionsOutsideLocalhost`: (Requires restart) If `true`, the Kestrel server listens on all network interfaces.
    *   `PackerIncludeFileTypes`, `PackerExcludeFilenames`: Settings for the Project Packager.
    *   `PackerExcludeFolderNames`: (List of strings) A list of folder names (e.g., "bin", "obj", "node_modules") that will be excluded during project source code packaging. Case-insensitive.
    *   `ConversationZipRetentionDays`, `ConversationDeleteZippedRetentionDays`: Settings for conversation archiving.
    *   **Azure DevOps Wiki Sync Settings:**
        *   `EnableWikiSystemPromptSync`: Boolean flag to enable/disable automatic wiki sync.
        *   `WikiSyncAdoOrganization`: Azure DevOps organization name.
        *   `WikiSyncAdoProject`: Azure DevOps project name.
        *   `WikiSyncWikiIdentifier`: Wiki identifier (typically `ProjectName.wiki`).
        *   `WikiSyncPagePath`: Path to the wiki page (e.g., `/Instructions/MasterSystemPrompt`).
        *   `WikiSyncTargetSystemPromptGuid`: GUID of the system prompt to update with wiki content.
*   **`systemPrompts/`**: Directory containing your saved System Prompts as `.prompt.json` files.
*   **`UserPrompts/`**: Directory containing your saved User Prompts as `.prompt.json` files.
*   **`Tools/toolLibrary.json`**: Your custom tool definitions and categories.
*   **`PinnedCommands/pinnedCommands.json`**: Configuration for your Pinned Shortcuts.
*   **`themes.json`**: User-imported/created UI themes and the active theme ID.
*   **`builtinToolExtraProps.json`**: Persisted user-configured `ExtraProperties` for built-in tools.
*   **`Config/mcpServers.json`**: Definitions for Model Context Protocol (MCP) servers.
*   **`Config/projects.json`**: Definitions for your managed projects (name, path, description).
*   **`convs/`**: Directory where individual conversation JSON files are stored.
    *   **`convs/archive/`**: Subdirectory where old conversations are zipped.

## 6.3 API Key Management
Sensitive API keys for external services are managed via the application menu:
*   `File > Settings > Set YouTube API Key...`
*   `File > Settings > Set GitHub API Key...`
*   `File > Settings > Set Azure DevOps PAT...`
These keys are encrypted and stored in `settings.json`.