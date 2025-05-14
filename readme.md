## 🚀 Max's AI Studio 4 🤖

A Windows Desktop AI Command Centre for Enhanced Development & Productivity

## 🌟 Highlights

*   **💬 Unified AI Chat:** Seamlessly chat with OpenAI, Anthropic Claude, Google Gemini models, and custom Model Context Protocol (MCP) servers from one interface.  Supports Ollama through OpenAI API comptibility.
*   **🛠️ Powerful Tool Ecosystem:** Leverage built-in tools for file operations (read, write, search, diff), web tasks (DuckDuckGo, YouTube, URL scraping), developer integrations (Git, GitHub, Azure DevOps, Sentry, Vite), and more.
*   **🔌 Extensible & Customizable AI:** Define new tools with JSON schemas or connect to external MCP-compliant tool servers.
*   **📝 Deep Project Integration:** Work directly with your local codebase using the `ProjectPath` setting for context-aware AI assistance and file operations.
*   **🌳 Advanced Conversation Management:** Navigate complex discussions with branched conversation history, powerful search, and an interactive tree view.
*   **🖥️ Modern Desktop Experience:** A .NET 9 WPF application hosting a fluid, responsive React/TypeScript UI via WebView2.
*   **🎨 Personalisable Interface:** AI tool-based theming engine supporting.
*   **⚡ Real-time Interaction:** Streaming AI responses and tool loop interjection.
*   **⌨️ Productivity Focused:** Command bar, pinnable shortcuts, rich system & user prompt systems with model/tool/MCP associations.
*   **🎤 Extras - Audio Transcription:** Integrated Whisper.net for local audio-to-text transcription.

---

## ℹ️ Overview

Max's AI Studio 4 (AiStudio4) is a feature-rich desktop application engineered to serve as a comprehensive AI-powered workspace for software developers, power users, and anyone looking to harness the capabilities of modern AI models.

**Who is it for?**
*   **Software Developers:** Seeking to integrate AI deeply into their local development environment, automate coding tasks, get contextual help, and manage project-related information efficiently.
*   **Power Users:** Who require a highly customizable and extensible AI chat client with advanced features for branching conversations and interacting with multiple AI models and tools.
*   **AI Enthusiasts:** Looking for a platform to experiment with different models, prompt engineering, and custom AI tool development.

---

## 🖼️ Screenshots / GIFs

**(Placeholder - To be added by the project owner)**

*   *Main Interface:* A screenshot showing the chat view, input bar, sidebar, and potentially the conversation tree.
*   *Tool Usage:* A GIF demonstrating the AI using a file system tool or a developer integration tool.
*   *System Prompt Editor:* Screenshot of the system prompt editor showing associations with models, tools, and user prompts.
*   *Theming:* A couple of screenshots showcasing different UI themes.
*   *Command Bar:* GIF of the command bar in action, searching for and executing a command.

---

## 🏁 Installation From A Release ZIP

TODO

---

## 📖 Usage Instructions

*(This section would benefit greatly from illustrative GIFs or short videos for each step.)*

1.  **Initial Setup:**
    *   **Set Project Path:** The first and most crucial step. Go to `File > Project > Set Project Path...` in the application menu and select the root directory of your software development project. This enables many context-aware features.
    *   **Configure AI Providers:**
        *   Open the main settings modal (e.g., via Command Bar: `Ctrl/Cmd+K`, type "Settings", or from a menu if available).
        *   Navigate to the "Service Providers" section. Add your AI service provider details (e.g., for OpenAI, you'd add the API key and base URL like `https://api.openai.com/v1`).
    *   **Add AI Models:**
        *   In the settings modal, go to the "Models" section.
        *   Add specific AI models you intend to use (e.g., "gpt-4-turbo", "claude-3-opus-20240229"). For each model, associate it with a configured Service Provider and set other properties like friendly name, pricing (for cost tracking), and UI color.
    *   **Select Default Models:** From the Input Bar's model status section, click to select your default "Primary Model" (for main chat) and "Secondary Model" (often used for summaries or quick tasks by tools).

2.  **Chatting with AI:**
    *   Type your messages into the main input area.
    *   Press `Enter` to send. For multi-line input, use `Shift+Enter`.
    *   AI responses will stream in. Markdown, code blocks, and diagrams are rendered appropriately.
    *   To **interject** while the AI is processing a tool, type your follow-up message and press `Ctrl+Enter` (or `Cmd+Enter`).

3.  **Using Tools:**
    *   Activate tools via the "Tools" button in the Input Bar or through the Command Bar.
    *   The AI will decide when to use an active tool based on your prompts. You can also explicitly request tool use (e.g., "Use the FileSearchTool to find all files containing 'TODO'").
    *   Some tools (like Vite commands) prompt for user confirmation before execution.
    *   Most tools are hard-coded to continue the tool loop.  The Stop tool terminates the loop, and is always included when tools are used, so the AI can always choose to terminate the loop.

4.  **Managing Prompts:**
    *   **System Prompts:**
        *   Access the System Prompt Library (default shortcut usually `Ctrl/Cmd+P`, or via Command Bar).
        *   Create new prompts, edit existing ones, or select a prompt to apply to the current conversation.
        *   When editing, you can associate specific tools, user prompts, AI models, and MCP servers that will be automatically activated/selected when this system prompt is applied.
        *   The `Include Git Diff` option will automatically attach the project's current Git diff to the context when the prompt is applied.
    *   **User Prompts:**
        *   Access the User Prompt Library (default shortcut usually `Ctrl/Cmd+U`, or via Command Bar).
        *   Create reusable text snippets or templates.
        *   Access User Prompts, reference project filenames and attach project files from the slash command dropdown, for instance `/Gemini.cs`  or `/MyUserPrompt`.

5.  **Navigating Conversations:**
    *   **Sidebar:** Toggle with `Ctrl/Cmd+B` (or via Command Bar) to see a list of past conversations. Click to load one.
    *   **Conversation Tree View:** (Accessible via Command Bar) Provides an interactive D3.js visualization of the current conversation's message branches. Click nodes to navigate. Middle-click a node to delete it and its descendants (with confirmation).
    *   **Search:** Use the search bar in the Sidebar to find text within your conversation history. Results will highlight matching conversations. Clicking a result loads the conversation and highlights matching messages in the tree view.

6.  **Customizing Appearance:**
    *   Access "Appearance Settings" (via Command Bar or Settings panel). Adjust the global font size.
    *   Explore the "Theme Library" (via Command Bar or Appearance Settings) to apply pre-defined themes or create/import your own custom themes.

7.  **Theming**
    *   Retheme the app using AI!  Create a tool with an output filetype of "theme", and set its schema using the "Use Theme Schema" button.  Enable the tool, and ask the AI for a particular style of theme; the response should include a "theme" file which can then be installed to the Theme Library.

---

## ✨ Key Features

### 1. Multi-Model AI Chat & Interaction
   *   **Broad Model Support:** Connect to OpenAI, Anthropic (Claude), Google (Gemini), Ollama instances, and any Model ContextProtocol (MCP) compliant server.
   *   **Configuration:** Manage API keys, base URLs, and model-specific parameters through a user-friendly settings interface.
   *   **Streaming Responses** 
   *   **Rich Content Rendering:** Markdown support for text, code blocks (with syntax highlighting), tables, and diagrams (Mermaid, DOT/Graphviz, JSON, HTML).
   *   **Attachments:** Include images and text files in your messages.
   *   **Temperature Control:** Easily adjust AI creativity/randomness via a slider in the input bar.

### 2. Conversation & Prompt Management
   *   **Branched Conversations:** Messages are stored in a tree structure, allowing for non-linear exploration of chat history.
   *   **Persistent Local Storage:** Conversations are saved to the user's local file system.
   *   **Conversation History:** Browse, search, and load previous conversations.
   *   **Interactive Tree View:** A D3.js-powered visualization displays the structure of the current conversation, enabling easy navigation between branches.
   *   **System Prompts:**
        *   Create, edit, and manage a library of custom system prompts.
        *   **Advanced Associations:** Link system prompts to specific tools, user prompts, primary/secondary AI models, and MCP servers for automated setup when a prompt is activated.
        *   **Git Diff Integration:** Optionally include the current project's Git diff in the context when a system prompt is applied.  Create a ReviewBot or CommitBot using an appropriate System Prompt, User Prompt and automatic attachment of a Git diff.
        *   Set global default or per-conversation system prompts.
   *   **User Prompts:**
        *   Maintain a library of reusable text snippets, templates, or instructions.
        *   Assign shortcuts (e.g., `/mytemplate`) for quick insertion via the input bar's slash command feature.
        *   Mark prompts as favorites for easy access.
   *   **Interjection:** Send follow-up messages or clarifications while the AI is in a tool-processing loop, which are then incorporated into the ongoing process.  Tell the AI it's got it wrong without interrupting the flow!

### 3. Advanced Tool System
   *   **Tool Library:** Create, edit, import, and export custom tools defined by JSON schemas. Organize tools into categories.
   *   **Built-in File System Tools:**
        *   `ReadFiles` & `ReadPartialFiles`: Access content from local files.
        *   `FileRegExSearch` & `FileSearchTool`: Powerful file content searching.
        *   `DirectoryTreeTool`: Visualize directory structures.
        *   `FindAndReplaceTool`: Perform text replacements across files.
        *   `CreateNewFile`, `DeleteFile`, `ModifyFiles`, `RenameFile`, `ReplaceFile`: Programmatic and AI-assisted file modifications, including handling merge failures.
   *   **Web & Information Retrieval Tools:**
        *   `RetrieveTextFromUrlTool`: Scrape and clean text from web pages.
        *   `RunDuckDuckGoSearchTool`: Integrate web search capabilities (prone to bot detection!)
        *   `YouTubeSearchTool`: Find YouTube videos.
        *   `LaunchUrlTool`: Open URLs in the system's default browser.
   *   **Developer-Focused Integrations:**
        *   **Git:** `GitCommitTool` for committing specified files with AI-generated or user-provided messages.
        *   **GitHub:** Fetch repository info, list directory contents, retrieve file content, search code within repositories.
        *   **Azure DevOps:** Interact with work items (get, query, updates, comments), pull requests (list, get by ID, threads, iterations, changes), and repositories.
        *   **Sentry:** Access organization details, project information, and issue lists.
        *   **Vite:** Create new Vite projects, manage npm dependencies (`npm install`, `npm run script`), control the Vite dev server, and modify `vite.config.js`.
   *   **Model Context Protocol (MCP) Support:**
        *   Define and manage connections to external MCP tool servers.
        *   Tools exposed by enabled MCP servers become available for AI use.
   *   **Utility & Meta-Tools:**
        *   `ThinkTool`: Allows the AI to "think out loud" or log its reasoning process.
        *   `StopTool`: Enables the AI to signal the completion of a multi-step task.
        *   `InfoRequestTool`: AI can request additional information or clarification from the user.
        *   `RecordMistakeTool`: Log AI errors to `CommonAiMistakes.md`, which is then used to refine future AI interactions. Includes AI-assisted consolidation of this mistake log.
   *   **Tool Configuration & Persistence:**
        *   Some built-in tools expose `ExtraProperties` (e.g., `excludedFileExtensions`, `excludedDirectories`) for runtime customization.
        *   These user-configured extra properties are persisted locally in `%APPDATA%/AiStudio4/builtinToolExtraProps.json`.

### 4. UI & Customization
   *   **Modern Web Interface:** Built with React, TypeScript, Vite, Shadcn/ui components, and Tailwind CSS for a responsive and fluid user experience.
   *   **Theming Engine:**
        *   Create, import, and apply custom UI themes defined in JSON.
        *   Supports dynamic font loading from CDNs specified in themes.
        *   ThemeManager discovers component-specific themeable properties.
   *   **Appearance Settings:** Globally adjust font size. Application defaults to a dark mode.
   *   **Resizable Panels:** Customizable layout with resizable and (in future) pinnable panels for conversation history, tree view, settings, and tool management.
   *   **Command Bar:** (Ctrl/Cmd+K) Provides quick, keyboard-driven access to all application features, commands, and settings.
   *   **Pinned Shortcuts:** A customizable bar in the App Header for one-click access to frequently used commands.
   *   **Slash Commands:** Type `/` in the input bar to quickly search and insert user prompts or project file paths.

### 5. Backend & Core Functionality
   *   **Host Application:** .NET 9 WPF provides the native desktop shell.
   *   **Embedded Web Server:** ASP.NET Core with Kestrel serves the SPA and handles backend API requests.
   *   **UI Rendering:** WebView2 hosts the React frontend.
   *   **Real-time Communication:** WebSockets are used for streaming AI responses, live conversation updates, status messages, and project file system monitoring.
   *   **Dependency Injection:** Manages services and dependencies throughout the C# backend.
   *   **Configuration:** Utilizes `appsettings.json` for backend settings and user-specific JSON files in `%APPDATA%/AiStudio4` for user preferences, conversation data, prompts, tools, etc.
   *   **Project Context (`ProjectPath`):** A critical setting that defines the root directory for the user's development project. This enables context-aware file operations, Git commands, and project analysis tools.
   *   **Audio Transcription:** Employs Whisper.net for local, efficient audio-to-text conversion. Transcribed text can be inserted into the chat.
   *   **.NET Project Analysis:** Analyzes C# project files (`.csproj`, `.cs`) to extract namespace, class, and member information, which can be used for context or by tools.
   *   **Project Packaging:** A tool to create a comprehensive XML package of a project's source code, respecting `.gitignore` rules and user-defined file type/name exclusions.

---

## 🏁 Building From Source [Not reviewed]

### Prerequisites
*   **.NET 9 SDK:** (or the version specified in `AiStudio4/AiStudio4.csproj`)
*   **Node.js & pnpm (or npm):** Check `AiStudio4/AiStudioClient/package.json` for recommended Node.js version. `pnpm` is preferred for managing frontend dependencies.
*   **WebView2 Runtime:** This is usually included with modern versions of Windows or Microsoft Edge. If not, it may need tobe installed separately.
*   **(Optional) Git:** Required for features like Git Diff inclusion and the GitCommit tool.
*   **(Optional) Conda:** If you plan to use locally hosted Python-based MCP servers (e.g., for advanced WhisperX transcription via an MCP server), a Conda environment might be necessary. The application allows setting a Conda path in its settings.

### Installation & Running

AI Studio 4 is comprised of a .NET backend (which also acts as the host application) and a React-based frontend.

**1. Backend & Main Application (`AiStudio4` project):**
   *   Open the solution file `AiTool3.sln` in Visual Studio (ensure you have the .NET 9 SDK and relevant workloads installed).
   *   Set the `AiStudio4` project as the startup project.
   *   **Build and Run:**
        *   Build the `AiStudio4` project. During the build process (specifically before `BeforeBuild` and `PrepareForRun` targets via the `NpmBuild` target in `AiStudio4.csproj`), the frontend application located in `AiStudioClient/dist` is typically copied to the backend's output directory (e.g., `AiStudio4/bin/Debug/net9.0-windows/`).
        *   Run the `AiStudio4` project from Visual Studio (e.g., by pressing F5).
        *   This launches the WPF desktop application. The embedded Kestrel server will start, and the WebView2 control will load the SPA.

**2. Frontend Development (`AiStudio4/AiStudioClient` directory):**
   *   This is primarily for development purposes if you want to modify the frontend UI.
   *   Open a terminal in the `AiStudio4/AiStudioClient` directory.
   *   **Install Dependencies:**
        *   `pnpm install` (Recommended)
        *   Or, if using npm: `npm install`
   *   **Run Frontend Dev Server:**
        *   `pnpm run dev` (or `npm run dev`)
        *   This starts the Vite development server, typically on `http://localhost:5173`.
        *   When the `AiStudio4` C# project is run in **Debug** configuration, it is configured to proxy requests to this Vite dev server. This allows for Hot Module Replacement (HMR) and a faster frontend development cycle.
   *   **Build Frontend for Production:**
        *   `pnpm run build` (or `npm run build`)
        *   This command compiles the React/TypeScript application and outputs the static assets to the `AiStudio4/AiStudioClient/dist` folder. These are the files that the C# application serves in Release mode or when the Vite dev server is not running.

**Important Notes on Running:**
*   For a standard user running the application (not developing the frontend), simply building and running the `AiStudio4` C# project from Visual Studio should be sufficient.
*   If you are developing the frontend, run both the C# backend (from Visual Studio in Debug mode) and the Vite dev server (`pnpm run dev` in `AiStudioClient`).

---

## ⚙️ Configuration [Not reviewed]

AI Studio 4 uses a combination of a backend configuration file and user-specific settings stored in the application data directory.

*   **Backend Configuration (`AiStudio4/appsettings.json`):**
    *   `WebServer:Port`: Defines the port on which the embedded Kestrel web server listens (default: `35005`).
*   **User-Specific Settings (Stored in `%APPDATA%/AiStudio4/` on Windows):**
    *   `settings.json`: The main user settings file. This JSON file stores:
        *   `ModelList`: An array of AI model definitions, including their names, GUIDs, associated provider GUIDs, pricing, UI color, etc.
        *   `ServiceProviders`: An array of AI service provider configurations, including API keys, base URLs, friendly names, and service names (e.g., "OpenAI", "Claude").
        *   `DefaultModelGuid` & `SecondaryModelGuid`: GUIDs of the models selected as primary and secondary defaults.
        *   AI interaction parameters: `Temperature`, `UsePromptCaching`, `StreamResponses`, `UseEmbeddings`, `EmbeddingModel`.
        *   `ProjectPath`: **This is a critical setting.** It defines the root directory of the user's active development project. Many tools (file system, Git, .NET analysis) operate relative to this path. It can be set via the "File" -> "Project" -> "Set Project Path..." menu in the application UI.
        *   API Keys: Stores API keys for external services like YouTube, GitHub, and Azure DevOps (as a PAT). These are typically set via the "File" -> "Settings" menu.
        *   `CondaPath`: Path to the Conda environment (if used for local Python-based MCP servers).
        *   `AllowConnectionsOutsideLocalhost`: A boolean (default `false`) determining if the embedded Kestrel server listens on all network interfaces or only on localhost. *Changing this setting requires an application restart to take effect.*
        *   `PackerIncludeFileTypes` & `PackerExcludeFilenames`: Configurations for the Project Packager tool.
    *   `systemPrompts/`: This directory contains JSON files, each representing a user-created system prompt (`.prompt.json`).
    *   `UserPrompts/`: Similarly, this directory stores user-created user prompts as JSON files (`.prompt.json`).
    *   `Tools/toolLibrary.json`: A JSON file storing custom tool definitions and tool categories created by the user.
    *   `PinnedCommands/pinnedCommands.json`: Stores the configuration for the user's pinned shortcuts in the UI.
    *   `themes.json`: Contains user-imported or created UI themes and the ID of the currently active theme.
    *   `builtinToolExtraProps.json`: Persists user-configured extra properties for built-in tools (e.g., custom exclusion lists for file search tools).
    *   `mcpServers.json` (in `Config/` subfolder): Stores definitions for Model Context Protocol (MCP) servers.

---

## 🛠️ Tech Stack

*   **Backend & Desktop Shell:**
    *   .NET 9 / C#
    *   WPF (Windows Presentation Foundation)
    *   ASP.NET Core (with Kestrel for embedded web server)
    *   WebView2 (for hosting web UI)
    *   Newtonsoft.Json (JSON processing)
    *   ModelContextProtocol SDK (for MCP client)
    *   OpenAI SDK (for OpenAI integration)
    *   Whisper.net (for audio transcription)
    *   Microsoft.CodeAnalysis (Roslyn - for .NET project analysis)
*   **Frontend (SPA in `AiStudioClient`):**
    *   React 19
    *   TypeScript
    *   Vite (build tool & dev server)
    *   Tailwind CSS (utility-first CSS)
    *   Shadcn/ui (accessible UI components)
    *   Zustand (state management)
    *   Axios (HTTP client)
    *   D3.js (conversation tree visualization)
    *   Mermaid.js, Viz.js (@hpcc-js/wasm) (diagram rendering in Markdown)
    *   React Markdown, React Syntax Highlighter (Markdown rendering)
    *   Lucide Icons, LobeHub Icons (UI icons)
*   **Communication Protocols:**
    *   WebSockets
    *   HTTP REST API

---

## 🌱 Development Notes

*   **Project Structure:** The application is divided into a C# backend/host (`AiStudio4`) and a TypeScript/React frontend (`AiStudio4/AiStudioClient`).
*   **Backend Development:** Typically done in Visual Studio. Key services are registered in `AiStudio4/App.xaml.cs`. API request handlers reside in `AiStudio4/InjectedDependencies/RequestHandlers/`. Core tool logic is found in `AiStudio4/Core/Tools/`.
*   **Frontend Development:** Uses Vite, with `pnpm` (or `npm`) for package management. Source code is in `AiStudio4/AiStudioClient/src/`. For a detailed frontend structure and data flow explanation, see `AiStudio4/Docs/Feature Designs/ProjectStructure.txt`.

### Running Tests
*   There aren't any.  Yet...

---

## 🤝 Contributing

We welcome contributions! Whether it's reporting a bug, suggesting a new feature, or submitting code changes, your input is valuable. Please refer to our `CONTRIBUTING.md` (placeholder - to be created) for detailed guidelines on:
*   Our code of conduct.
*   The process for reporting issues.
*   How to submit pull requests.
*   Coding standards and patterns used in the project.

---

## 📜 License

**This project is licensed under the MIT License. See the `LICENSE.md` file for full details.

*   **Dependency Licenses:**
    *   See File -> Licenses

---

## ✍️ Author & Acknowledgements

*   **Author:** Max Hedge (@stringandstickytape)
*   **Acknowledgements:**
    *   This README structure was inspired by [Bane Sullivan's guide on writing good READMEs](https://github.com/banesullivan/README), written by Gemini Pro 2.5 Preview, and reviewed/edited by hand :)
    
---

## 📖 Further Reading & Links

*   [Detailed Frontend Project Structure](./AiStudio4/Docs/Feature%20Designs/ProjectStructure.txt)
*   [Backend Architectural Overview](./ClaudeCode.md)
*   [UI Theming Guide](./AiStudio4/Theming.md)
*   [Internal Design Documents](./AiStudio4/Docs/Feature%20Designs/) (Contains detailed designs for various features)
*   [Whisper.net Library](https://github.com/sandrohanea/whisper.net)
*   [OpenAI .NET Library](https://github.com/openai/openai-dotnet)

---