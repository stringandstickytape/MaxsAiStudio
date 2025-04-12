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
    *   **Zustand:** Used as the primary global state management solution. Stores are located in `src/stores/` and cover various application domains (e.g., `useConvStore`, `useModelStore`, `useToolStore`, `useAppearanceStore`, `useWebSocketStore`). These stores hold shared application data, settings, and status.
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