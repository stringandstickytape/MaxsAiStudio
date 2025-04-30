// ClaudeCode.md
# ClaudeCode.md - AiStudio4 Architectural Overview

## Core Architecture

AiStudio4 is a hybrid desktop application built using:

*   **.NET 9 / WPF:** Serves as the main application host, providing the window frame, menus, and native OS integration.
*   **WebView2:** Hosts the primary user interface, which is a Single Page Application (SPA).
*   **ASP.NET Core Components:** Leveraged within the WPF application for:
    *   **Dependency Injection:** `Microsoft.Extensions.DependencyInjection` is used extensively (`App.xaml.cs`) to manage service lifetimes and dependencies.
    *   **Configuration:** Reads settings from `appsettings.json` (`Microsoft.Extensions.Configuration.Json`).
    *   **Logging:** Uses `Microsoft.Extensions.Logging`.
    *   **Web Server (Kestrel):** An embedded web server (`WebServer.cs`) hosts the SPA and potentially provides an API endpoint for the frontend.
    *   **SPA Proxy:** (`Microsoft.AspNetCore.SpaProxy`) Facilitates development-time hosting of the SPA.

## Frontend (AiStudio4.Web)

*   **Technology:** Based on the project setup (`AiStudio4.csproj`), this is likely a **React/TypeScript** SPA located in the `AiStudio4.Web` directory.
*   **Build:** Built using npm (as indicated by the `NpmBuild` target in the `.csproj`). The built static assets (`dist` folder) are embedded as resources in the main application assembly and potentially copied to the output directory.
*   **Communication:** Interacts with the C# backend primarily via **WebSockets** (`WebSocketServer.cs`, `WebSocketMessageHandler.cs`, `IWebSocketNotificationService`) for real-time updates and potentially standard HTTP requests to the embedded Kestrel server.
*   **State Management:**
    *   **Zustand:** Used as the primary global state management solution. Stores are located in `src/stores/` and cover various application domains (e.g., `useConvStore`, `useModelStore`, `useToolStore`, `useAppearanceStore`, `useThemeStore`, `useWebSocketStore`). These stores hold shared application data, settings, and status. Theme changes are propagated via `useThemeStore`, which enables components (including D3-based visualizations) to reactively update their appearance on theme switch.
    *   **React `useState`:** Employed extensively within components for managing local UI state, such as dialog visibility, temporary form input values, hover/drag states, component lifecycle flags, and filter/search terms.
    *   **Custom Hooks:** Located in `src/hooks/`. These often encapsulate related state logic, potentially combining local `useState` with interactions with Zustand stores or backend APIs (e.g., `useChatManagement`, `useResourceManagement`, `useAttachmentManager`).
    *   **`react-hook-form`:** Utilized within form components (like `GenericForm.tsx`) for managing form state, validation, and submission.

## Backend (C# - AiStudio4 Project)

*   **Entry Point:** `App.xaml.cs` initializes the DI container and starts key services.
*   **Main Window:** `MainWindow.xaml.cs` (`WebViewWindow` class) manages the WebView2 control, main menu interactions, and settings access.
*   **Key Services & Responsibilities:**
    *   `ISettingsService`: Manages application settings, crucially the `ProjectPath`.
    *   `IConvStorage`, `IChatService`, `ChatManager`, `ChatProcessingService`, `MessageHistoryService`: Handle chat session logic, storage, and processing.
    *   `IToolService`, `IBuiltinToolService`, `IToolProcessorService`: Manage and execute available tools (both built-in and potentially external).
    *   `IMcpService`: Interacts with Model Context Protocol servers (likely for AI model interactions).
    *   `ISystemPromptService`, `IPinnedCommandService`, `IUserPromptService`: Manage different types of prompts used in AI interactions.
    *   `WebSocketServer`, `WebSocketConnectionManager`, `WebSocketMessageHandler`: Handle WebSocket communication with the frontend.
    *   `WebServer`: Hosts the SPA/API using Kestrel.
    *   `WindowManager`: Manages application windows.
    *   `UiRequestBroker`: Likely facilitates communication or requests between UI and backend services.
    *   `FileServer`: Provides file access capabilities.
    *   **Dialogs:**
        *   `WpfInputDialog`: A reusable WPF window for simple text input, located in the `Dialogs` folder. Used for tasks like setting the YouTube API key via the main menu.
*   **AI Integration:** Uses `OpenAI` and `ModelContextProtocol` NuGet packages, indicating direct interaction with OpenAI models and potentially other AI services via MCP.
*   **Project Dependencies:** Relies on `AiTool3` and `SharedClasses` projects for additional functionality.

## Critical Configuration

*   **`ProjectPath`:** This setting, managed by `ISettingsService` and configured via the "File" menu in the UI (`WebViewWindow.xaml.cs`), is **essential**. It determines the context for many operations, especially file system access and context-aware AI tasks performed by tools (`IBuiltinToolService.UpdateProjectRoot`).
*   **`appsettings.json`:** Contains backend configuration details.
*   **MCP Server Definitions:** Managed via `IMcpService` and configurable through the UI, determining available AI model endpoints.

## Key Patterns & Concepts

*   **Dependency Injection:** Central to the application's structure.
*   **Service-Oriented Architecture:** Logic is encapsulated in distinct services.
*   **Hybrid Application Model:** Combines native WPF shell with a web-based UI (WebView2).
*   **Real-time Communication:** WebSockets are used for frontend-backend interaction.
*   **Settings Management:** Centralized handling of user/application settings.
*   **Tool Abstraction:** Tools are managed and executed through dedicated services.

## State Management Improvements

Recent improvements to the state management approach in the AiStudio4 web application include:

### 1. Command Registry Service

Created a centralized service for command registration and management that abstracts direct store access:

- `src/services/commandRegistry.ts`: Provides methods for registering, unregistering, and executing commands, as well as searching and retrieving commands.

### 2. Window Events Service

Created a centralized registry of window events to improve maintainability:

- `src/services/windowEvents.ts`: Documents all window events used in the application and provides helper functions for working with them.

### 3. Refactored Command Files

Refactored command files to use the new services instead of direct store access:

- `src/commands/coreCommands.ts`
- `src/commands/settingsCommands.ts`
- `src/commands/systemPromptCommands.ts`
- `src/commands/themeCommands.ts`
- `src/commands/toolCommands.ts`
- `src/commands/userPromptCommands.ts`

### 4. Refactored Components

Refactored components to use the new services instead of direct store access:

- `src/CommandInitializationPlugin.tsx`
- `src/components/InputBar.tsx`

## Component Architecture Improvements

### ConvView Component Refactoring

The ConvView component was identified as a "god component" with too many responsibilities. It has been refactored into smaller, more focused components:

#### New Component Structure

- `src/components/ConvView/` - New directory containing all conversation view components
  - `ConvView.tsx` - Main orchestration component
  - `MessageItem.tsx` - Renders individual messages
  - `MessageActions.tsx` - Handles message action buttons (copy, edit, save)
  - `MessageEditor.tsx` - Manages message editing functionality
  - `MessageMetadata.tsx` - Displays timing, token usage, and cost information
  - `StreamingMessage.tsx` - Handles streaming token display
  - `ConversationControls.tsx` - Manages loading more messages
  - `ScrollManager.tsx` - Handles scroll behavior and "stick to bottom" functionality
  - `index.ts` - Exports all components

#### Benefits of Refactoring

1. **Single Responsibility Principle**: Each component now has a clear, focused responsibility
2. **Improved Maintainability**: Smaller components are easier to understand and modify
3. **Better Testability**: Components can be tested in isolation
4. **Enhanced Reusability**: Components can be reused in different contexts
5. **Reduced Complexity**: Main component is now simpler and delegates to specialized components

## Tool Configurability via ExtraProperties

Many built-in tools in AiStudio4 expose additional configuration or metadata through the `ExtraProperties` dictionary on their `Tool` definition. This allows for flexible, tool-specific customization without changing the core schema or requiring code changes for every new property.

**Pattern:**
- Each tool's `GetToolDefinition()` can add arbitrary key-value pairs to `ExtraProperties`.
- These properties are typically used for UI hints, filtering, or runtime logic.
- Properties are usually documented as "PropertyName (CSV)" for lists, or with clear naming for single values.
- **Built-in tool extra properties are now persisted across sessions using `IBuiltInToolExtraPropertiesService` and stored in `%APPDATA%/AiStudio4/builtinToolExtraProps.json`. The service is injected into `BuiltinToolService`, which loads and saves extra properties for each built-in tool by name.**

**Examples:**

- **FileSearchTool:**
    - `ExcludedFileExtensions (CSV)`: A comma-separated list of file extensions (e.g., `.cs,.dll,.xml`) that should be excluded from search results.
    - `ExcludedFilePrefixes (CSV)`: A comma-separated list of filename prefixes (e.g., `jquery`) to exclude.
    - These are parsed at runtime and used to filter files, replacing hardcoded logic.
- **ThinkTool:**
    - Demonstrates use of `ExtraProperties` for arbitrary metadata (e.g., `TestProperty`).
- **DirectoryTreeTool:**
    - `ExcludedFileExtensions (CSV)`: A comma-separated list of file extensions (e.g., `.cs`) that should be excluded from the directory tree.
    - `ExcludedDirectories (CSV)`: A comma-separated list of directory names (e.g., `bin,obj,node_modules`) to exclude from traversal.
    - These are parsed at runtime and used to filter files and directories, replacing hardcoded logic.

**Benefits:**
- Enables per-tool customization without code changes.
- Facilitates UI discovery of tool-specific options.
- Supports future extensibility for new tool behaviors.

**Best Practice:**
- When adding new filtering or configuration logic to a tool, prefer using `ExtraProperties` with clear, documented keys.
- Use CSV format for lists, and document expected value types in the property name or tool documentation.

## Overall Benefits of Recent Improvements

These improvements provide several benefits:

1. **Improved Testability**: Components and logic are easier to test in isolation
2. **Better Performance**: Optimized rendering with proper hook usage
3. **Enhanced Maintainability**: Consistent patterns make the codebase easier to understand
4. **Better Documentation**: Window events are centrally documented
5. **Reduced Bugs**: Standardized state access patterns reduce the chance of unexpected interactions
6. **Improved Component Architecture**: Breaking down large components into smaller, focused ones

## Future Improvements

Future improvements could include:

1. Refactoring more components to use hooks instead of direct store access
2. Creating custom hooks for complex state logic
3. Adding persistence middleware to stores
4. Standardizing global state access patterns further
5. Continuing to identify and refactor "god components" into smaller, focused components
6. Adding comprehensive unit tests for the new component structure
7. Creating a component library documentation to showcase the available components