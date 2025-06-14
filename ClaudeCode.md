// ClaudeCode.md
# ClaudeCode.md - AiStudio4 Architectural Overview

## 🚨 MANDATORY FRONTEND DEVELOPMENT REFERENCE

**Before making any frontend changes, bug fixes, or new feature implementations, you MUST consult the [Frontend Best Practices and Architectural Patterns](docs/readme/FrontendBestPractices.md) document.** This document contains the essential architectural patterns, state management strategies, component organization principles, and coding standards that govern the AiStudio4 frontend.

**Key Requirements:**
- Follow established Zustand store patterns and state management strategies
- Maintain component architecture consistency with domain-based organization
- Use the resource factory pattern for CRUD operations
- Implement proper TypeScript typing throughout
- Follow the command system architecture for user actions
- Respect the established theming and styling patterns
- Maintain WebSocket integration patterns for real-time features

Failure to follow these patterns may result in inconsistent code, architectural drift, and maintenance issues.

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

## Frontend (AiStudioClient)

*   **Technology:** A **React/TypeScript** SPA built with Vite, located in the `AiStudioClient` directory.
*   **Build:** End users manually build the frontend using `pnpm build` in the AiStudioClient directory. The built static assets (`dist` folder) are then served by the embedded web server.
*   **Communication:** Interacts with the C# backend primarily via **WebSockets** (`WebSocketServer.cs`, `WebSocketMessageHandler.cs`, `IWebSocketNotificationService`) for real-time updates and potentially standard HTTP requests to the embedded Kestrel server.
*   **UI Framework:** Uses **Radix UI** primitives with **Tailwind CSS** for styling, providing accessible, customizable components.
*   **Key Libraries:** 
    - **Mermaid** and **D3.js** for diagram rendering and data visualization
    - **React Hook Form** with **Zod** for form validation
    - **React Markdown** for content rendering
    - **Recharts** for data visualization
    - **React Flow** for node-based diagrams
*   **State Management:**
    *   **Zustand:** Primary global state management solution. Stores are located in `src/stores/` and cover various application domains (e.g., `useConvStore`, `useModelStore`, `useToolStore`, `useThemeStore`, `useWebSocketStore`, `useModalStore`, `usePinnedCommandsStore`). These stores hold shared application data, settings, and status.
    *   **Redux Toolkit:** Used alongside Zustand for specific state management needs, providing additional tooling for complex state operations.
    *   **React `useState`:** Employed extensively within components for managing local UI state, such as dialog visibility, temporary form input values, hover/drag states, component lifecycle flags, and filter/search terms.
    *   **Custom Hooks:** Located in `src/hooks/`. These often encapsulate related state logic, potentially combining local `useState` with interactions with Zustand stores or backend APIs (e.g., `useChatManagement`, `useResourceManagement`, `useAttachmentManager`, `useMessageStream`).
    *   **`react-hook-form`:** Utilized within form components (like `GenericForm.tsx`) for managing form state, validation, and submission.

## Backend (C# - AiStudio4 Project)

*   **Entry Point:** `App.xaml.cs` initializes the DI container and starts key services.
*   **Main Window:** `MainWindow.xaml.cs` (`WebViewWindow` class) manages the WebView2 control, main menu interactions, and settings access.
*   **Key Services & Responsibilities:**
    *   `IGeneralSettingsService`, `IAppearanceSettingsService`: Manage application settings including the crucial `ProjectPath` and UI appearance configuration.
    *   `IConversationStorage`, `IChatService`, `ChatManager`, `ChatProcessingService`: Handle chat session logic, storage, and processing.
    *   `IToolService`, `IBuiltinToolService`, `IToolProcessorService`: Manage and execute available tools (both built-in and external).
    *   `IMcpService`: Interacts with Model Context Protocol servers for AI model interactions.
    *   `ISystemPromptService`, `IPinnedCommandService`, `IUserPromptService`: Manage different types of prompts used in AI interactions.
    *   `WebSocketServer`, `WebSocketConnectionManager`, `WebSocketMessageHandler`: Handle WebSocket communication with the frontend.
    *   `WebServer`: Hosts the SPA/API using Kestrel.
    *   `WindowManager`: Manages application windows.
    *   `UiRequestBroker`, `UiRequestRouter`: Facilitate communication and route requests between UI and backend services.
    *   `FileServer`: Provides file access capabilities.
    *   `IProjectFileWatcherService`: Monitors project file system changes.
    *   `BuiltInToolExtraPropertiesService`: Manages persistent extra properties for built-in tools.
    *   **Dialogs:**
        *   `WpfInputDialog`: A reusable WPF window for simple text input, located in the `Dialogs` folder. Used for tasks like setting the YouTube API key via the main menu.
*   **AI Integration:** Multi-provider AI service architecture supporting various AI models:
    *   **OpenAI:** Direct OpenAI API integration (`OpenAI.cs`, `NetOpenAi.cs`)
    *   **Claude:** Anthropic Claude integration (`Claude.cs`)
    *   **Gemini:** Google Gemini integration (`Gemini.cs`)
    *   **MCP:** Model Context Protocol support for external AI services
    *   **Testing:** Mock and Lorem Ipsum services for development (`MockAiService.cs`, `LoremIpsumService.cs`)
    *   **Service Resolution:** `AiServiceResolver.cs` manages provider selection and routing
*   **Project Dependencies:** Relies on `SharedClasses` project for shared models and utilities.

## Critical Configuration

*   **`ProjectPath`:** This setting, managed by `IGeneralSettingsService` and configured via the "File" menu in the UI (`MainWindow.xaml.cs`), is **essential**. It determines the context for many operations, especially file system access and context-aware AI tasks performed by tools (`IBuiltinToolService.UpdateProjectRoot`).
*   **Settings Storage:** Application settings are persisted in `%APPDATA%/AiStudio4/` including general settings, appearance settings, and built-in tool extra properties.
*   **MCP Server Definitions:** Managed via `IMcpService` and configurable through the UI, determining available AI model endpoints and external tool integrations.

## Key Patterns & Concepts

*   **Dependency Injection:** Central to the application's structure.
*   **Service-Oriented Architecture:** Logic is encapsulated in distinct services.
*   **Hybrid Application Model:** Combines native WPF shell with a web-based UI (WebView2).
*   **Real-time Communication:** WebSockets are used for frontend-backend interaction.
*   **Settings Management:** Centralized handling of user/application settings.
*   **Tool Abstraction:** Tools are managed and executed through dedicated services.

## Built-in Tool System

AiStudio4 includes a comprehensive set of built-in tools organized by category:

**File System Tools:**
- `ReadFilesTool`, `ReadPartialFilesTool`: Read file contents with optional line ranges
- `CreateNewFileTool`, `DeleteFileTool`, `RenameFileTool`, `ReplaceFileTool`: Basic file operations
- `ModifyFilesTool`: Advanced file modification with diff-based changes
- `DirectoryTreeTool`: Generate directory structure listings
- `FileSearchTool`, `FileRegExSearch`: Search files by name patterns or regex content

**Code Management Tools:**
- `CodeDiff` system: Sophisticated file operation handlers with preprocessing, validation, and security checks
- `FindAndReplaceTool`: Text replacement across files

**Version Control Tools:**
- `GitStatusTool`, `GitBranchTool`, `GitCommitTool`, `GitLogTool`: Git operations

**External Integration Tools:**
- **GitHub:** Complete GitHub API integration (issues, PRs, content, search)
- **Azure DevOps:** Comprehensive DevOps integration (work items, repos, PRs, wiki)
- **Vite/Node.js:** Development environment tools (npm, Vite config, dev server)
- **Sentry:** Error tracking integration
- **YouTube:** Video search capabilities

**AI/Search Tools:**
- `GeminiGoogleSearchTool`, `GoogleCustomSearchApiTool`, `RunDuckDuckGoSearchTool`: Web search
- `SecondAiOpinionTool`: Get secondary AI analysis
- `RetrieveTextFromUrlTool`: Extract content from URLs

**Workflow Tools:**
- `PresentResultsAndAwaitUserInputTool`, `ThinkAndAwaitUserInputTool`: Interactive workflow control
- `ThinkAndContinueTool`: AI reasoning continuation
- `StopTool`: Explicit workflow termination
- `RecordMistakeTool`: Error tracking and learning

**Database Tools:**
- `ReadDatabaseSchemaTool`: Database schema inspection

## Tool Configurability via ExtraProperties

Built-in tools expose additional configuration through the `ExtraProperties` dictionary, enabling flexible customization:

**Key Features:**
- **Persistent Storage:** Extra properties are persisted using `IBuiltInToolExtraPropertiesService` in `%APPDATA%/AiStudio4/builtinToolExtraProps.json`
- **Runtime Configuration:** Properties modify tool behavior without code changes
- **UI Integration:** Properties can be configured through the tool management interface

**Common Property Patterns:**
- `excludedFileExtensions (CSV)`: File extension exclusion lists
- `excludedDirectories (CSV)`: Directory exclusion lists  
- `excludedFilePrefixes (CSV)`: Filename prefix exclusion lists

## Component Architecture

The frontend follows a modular component architecture:

**Core Layout Components:**
- `App.tsx`: Root application component with theme and state providers
- `ChatContainer.tsx`: Main chat interface container
- `Sidebar.tsx`: Navigation and status sidebar
- `InputBar/`: Modular input system with multiple sub-components
- `PanelManager.tsx`: Manages resizable panel layouts

**Feature-Specific Components:**
- `ConvView/`: Message display and conversation rendering
- `ConvTreeView/`: Conversation tree visualization with D3.js
- `MarkdownPane/`: Markdown content rendering with syntax highlighting
- `SystemPrompt/`, `UserPrompt/`: Prompt management interfaces
- `modals/`: Modal dialog system with unified modal architecture

**UI Components:**
- `ui/`: Reusable UI primitives based on Radix UI
- `diagrams/`: Specialized renderers for various content types (Mermaid, DOT, JSON, etc.)
- `content/`: Content block rendering system with pluggable renderers

**Utility Systems:**
- `commands/`: Command system with registration and execution
- `services/`: API clients and WebSocket management
- `hooks/`: Reusable React hooks for common patterns
- `utils/`: Pure utility functions for data manipulation

## Content Rendering System

AiStudio4 features a pluggable content rendering system for different message content types:

**Content Block Registry:**
- `contentBlockRendererRegistry.ts`: Central registry mapping content types to React components
- Supports both enum-based (C# backend) and string-based content type identification
- Extensible architecture allowing registration of new content renderers

**Built-in Content Renderers:**
- `TextContentRenderer`: Standard text content with markdown support
- `SystemContentRenderer`: System messages and notifications
- `AiHiddenContentRenderer`: Hidden AI reasoning content

**Specialized Diagram Renderers:**
- `mermaid-renderer`: Mermaid diagram rendering
- `dot-renderer`: Graphviz DOT diagram rendering
- `json-renderer`: Formatted JSON display
- `html-renderer`: Safe HTML content rendering
- `codeDiff-renderer`: Code diff visualization
- `modifyFiles-renderer`: File modification preview
- `txt-renderer`: Plain text with syntax highlighting

This system enables the application to handle diverse content types from AI responses while maintaining type safety and extensibility.

## Testing Infrastructure

AiStudio4 includes comprehensive testing capabilities:

**Frontend Testing:**
- **Cypress:** End-to-end testing framework configured for the React application
- **Test Organization:** Tests located in `AiStudioClient/cypress/e2e/` covering core functionality
- **Test Categories:**
  - `basic_tests_1.cy.ts`: Fundamental application behavior
  - `command_bar.cy.ts`: Command system testing
  - `providers.cy.ts`: AI provider integration testing
- **Configuration:** Custom Cypress configuration in `cypress.config.mjs`
- **Scripts:** `pnpm cypress:open` for interactive testing, `pnpm cypress:run` for CI/CD

**Development Testing:**
- **Mock Services:** `MockAiService.cs` and `LoremIpsumService.cs` provide deterministic responses for development
- **Service Isolation:** AI service abstraction enables testing without external API dependencies
