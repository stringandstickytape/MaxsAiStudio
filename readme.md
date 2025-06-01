﻿# Max's AI Studio 4 - User Guide

*(This User Guide is based on the project's main README, incorporating details for users and developers.)*

**A Windows Desktop AI (LLM) Command Centre for Enhanced Development & Productivity.**

## Table of Contents

1.  [Introduction](#1-introduction)
    *   [1.1 What is Max's AI Studio 4?](#11-what-is-maxs-ai-studio-4)
    *   [1.2 Who is it for?](#12-who-is-it-for)
    *   [1.3 Highlights](#13-highlights)
2.  [Getting Started](#2-getting-started)
    *   [2.1 System Requirements](#21-system-requirements)
    *   [2.2 Installation](#22-installation)
        *   [2.2.1 Installing from a Release](#221-installing-from-a-release)
        *   [2.2.2 Building from Source](#222-building-from-source)
    *   [2.3 Initial Setup](#23-initial-setup)
        *   [2.3.1 Setting the Project Path](#231-setting-the-project-path)
        *   [2.3.2 Configuring AI Service Providers](#232-configuring-ai-service-providers)
        *   [2.3.3 Adding AI Models](#233-adding-ai-models)
        *   [2.3.4 Selecting Default Models](#234-selecting-default-models)
3.  [Core Concepts](#3-core-concepts)
    *   [3.1 The Main Interface](#31-the-main-interface)
    *   [3.2 Project Path: Its Importance](#32-project-path-its-importance)
    *   [3.3 AI Models & Providers](#33-ai-models--providers)
    *   [3.4 Tools: How They Work](#34-tools-how-they-work)
    *   [3.5 Conversations](#35-conversations)
    *   [3.6 Prompts](#36-prompts)
4.  [Using AiStudio4](#4-using-aistudio4)
    *   [4.1 Chatting with AI](#41-chatting-with-ai)
        *   [4.1.1 Sending Messages](#411-sending-messages)
        *   [4.1.2 Interjecting During Tool Use](#412-interjecting-during-tool-use)
        *   [4.1.3 Rich Content (Markdown, Diagrams, Tables)](#413-rich-content-markdown-diagrams-tables)
    *   [4.2 Managing Conversations](#42-managing-conversations)
        *   [4.2.1 Conversation History & Sidebar](#421-conversation-history--sidebar)
        *   [4.2.2 Searching Conversations](#422-searching-conversations)
        *   [4.2.3 Conversation Tree View](#423-conversation-tree-view)
    *   [4.3 Working with Prompts](#43-working-with-prompts)
        *   [4.3.1 System Prompts](#431-system-prompts)
        *   [4.3.2 User Prompts](#432-user-prompts)
    *   [4.4 Using Tools](#44-using-tools)
        *   [4.4.1 Activating Tools](#441-activating-tools)
        *   [4.4.2 AI-Driven Tool Use](#442-ai-driven-tool-use)
        *   [4.4.3 Explicit Tool Requests](#443-explicit-tool-requests)
    *   [4.5 Attachments](#45-attachments)
        *   [4.5.1 Attaching Files (Images, Text)](#451-attaching-files-images-text)
        *   [4.5.2 Attaching Git Diff](#452-attaching-git-diff)
        *   [4.5.3 Attaching Image from Clipboard](#453-attaching-image-from-clipboard)
    *   [4.6 Audio Transcription](#46-audio-transcription)
5.  [Key Features in Detail](#5-key-features-in-detail)
    *   [5.1 AI Model Integration](#51-ai-model-integration)
        *   [5.1.1 Supported Models and Providers](#511-supported-models-and-providers)
        *   [5.1.2 Configuring Service Providers](#512-configuring-service-providers)
        *   [5.1.3 Managing AI Models](#513-managing-ai-models)
        *   [5.1.4 Model-Specific Features](#514-model-specific-features)
        *   [5.1.5 Temperature Control](#515-temperature-control)
    *   [5.2 Tool System](#52-tool-system)
        *   [5.2.1 Overview of Built-in Tools](#521-overview-of-built-in-tools)
        *   [5.2.2 Model Context Protocol (MCP)](#522-model-context-protocol-mcp)
        *   [5.2.3 Creating, Importing & Exporting Custom Tools](#523-creating-importing--exporting-custom-tools)
        *   [5.2.4 Tool Extra Properties](#524-tool-extra-properties)
    *   [5.3 UI and Customization](#53-ui-and-customization)
        *   [5.3.1 Theming](#531-theming)
        *   [5.3.2 Command Bar](#532-command-bar)
        *   [5.3.3 Pinned Shortcuts](#533-pinned-shortcuts)
        *   [5.3.4 Slash Commands](#534-slash-commands)
        *   [5.3.5 Appearance Settings](#535-appearance-settings)
        *   [5.3.6 Resizable Panels](#536-resizable-panels)
    *   [5.4 Project Integration](#54-project-integration)
        *   [5.4.1 .NET Project Analysis](#541-net-project-analysis)
        *   [5.4.2 Project Source Code Packaging](#542-project-source-code-packaging)
    *   [5.5 Backend and Core Functionality](#55-backend-and-core-functionality)
        *   [5.5.1 Real-time Communication](#551-real-time-communication)
6.  [Advanced Configuration](#6-advanced-configuration)
    *   [6.1 Backend Configuration (`appsettings.json`)](#61-backend-configuration-appsettingsjson)
    *   [6.2 User-Specific Settings (`%APPDATA%/AiStudio4/`)](#62-user-specific-settings-appdatastudio4)
    *   [6.3 API Key Management](#63-api-key-management)
7.  [Troubleshooting](#7-troubleshooting)
8.  [Technical Stack](#8-technical-stack)
9.  [Contributing](#9-contributing)
    *   [9.1 Development Notes](#91-development-notes)
10. [License](#10-license)
11. [Author & Acknowledgements](#11-author--acknowledgements)
12. [Further Reading & Links](#12-further-reading--links)

---

## 1. Introduction

### 1.1 What is Max's AI Studio 4?
Max's AI Studio 4 (AiStudio4) is a feature-rich desktop application engineered to serve as a comprehensive AI-powered workspace for software developers, power users, and anyone looking to harness the capabilities of modern AI models. It provides a unified interface for interacting with various Large Language Models (LLMs), an extensible tool ecosystem, and deep integration with local development projects.

### 1.2 Who is it for?
*   **Software Developers:** Seeking to integrate AI deeply into their local development environment, automate coding tasks, get contextual help, manage project-related information efficiently, and have AI iterate over code, edit it, and even launch it.
*   **Power Users:** Who require a highly customizable and extensible AI chat client with advanced features for branching conversations and interacting with multiple AI models and tools.
*   **AI Enthusiasts:** Looking for a platform to experiment with different models, prompt engineering, and custom AI tool development.

### 1.3 Highlights
*   **Unified AI Chat:** Seamlessly chat with OpenAI, Anthropic Claude, Google Gemini models, and custom Model Context Protocol (MCP) servers from one interface. Supports Ollama through OpenAI API compatibility.
*   **Powerful Tool Ecosystem:** Leverage built-in tools for file operations (read, write, search, diff), web tasks (DuckDuckGo, YouTube, URL scraping), developer integrations (Git, GitHub, Azure DevOps, Sentry, Vite), and more. Allows AI to iterate over your code, edit it, and even launch it.
*   **Extensible & Customizable AI:** Define new tools with JSON schemas or connect to external MCP-compliant tool servers.
*   **Deep Project Integration:** Work directly with your local codebase using the `ProjectPath` setting for context-aware AI assistance and file operations.
*   **Advanced Conversation Management:** Navigate complex discussions with branched conversation history, powerful search, and an interactive tree view.
*   **Modern Desktop Experience:** A .NET 9 WPF application hosting a fluid, responsive React/TypeScript UI via WebView2.
*   **Personalizable Interface:** AI tool-based theming engine.
*   **Real-time Interaction:** Streaming AI responses and tool loop interjection. WebSockets are used for streaming AI responses, live conversation updates, status messages, and project file system monitoring.
*   **Productivity Focused:** Command bar, pinnable shortcuts, rich system & user prompt systems with model/tool/MCP associations.
*   **Extras:** Integrated local audio transcription (Whisper.net), .NET project analysis.

---

## 2. Getting Started

### 2.1 System Requirements
*   Windows Operating System (with WebView2 Runtime installed - usually included with modern Windows/Edge)
*   .NET 9 SDK (for building from source, not required for running a release)
*   **(For Developers building from source):**
    *   Node.js (check `AiStudio4/AiStudioClient/package.json` for version, typically LTS)
    *   `pnpm` (`npm install -g pnpm`) is preferred for managing frontend dependencies (or `npm`).
    *   (Optional) Git: Required for features like Git Diff inclusion and the GitCommit tool.
    *   (Optional) Conda: If planning to use locally hosted Python-based MCP servers.

### 2.2 Installation

#### 2.2.1 Installing from a Release
*(Instructions for installing from a release ZIP or installer would go here. Currently, the `create-release.bat` script creates a `release-!SHORT_SHA!.7z` archive containing the build output.)*

#### 2.2.2 Building from Source
AI Studio 4 is comprised of a .NET backend (which also acts as the host application) and a React-based frontend.

**1. Backend & Main Application (`AiStudio4` project):**
    1.  Ensure prerequisites from [Section 2.1](#21-system-requirements) are met.
    2.  Clone the repository.
    3.  Open `AiTool3.sln` in Visual Studio (ensure you have the .NET 9 SDK and relevant workloads installed).
    4.  Set `AiStudio4` as the startup project.
    5.  Build and run the `AiStudio4` project. The first build might take longer as it also builds the frontend client.
        *   During the build process (specifically via the `NpmBuild` target in `AiStudio4.csproj`), the frontend application located in `AiStudioClient/dist` is copied to the backend's output directory (e.g., `AiStudio4/bin/Debug/net9.0-windows/wwwroot`).

**2. Frontend Development (Optional, for modifying the UI - `AiStudio4/AiStudioClient` directory):**
    1.  Open a terminal in the `AiStudio4/AiStudioClient` directory.
    2.  **Install Dependencies:**
        *   `pnpm install` (Recommended)
        *   Or, if using npm: `npm install`
    3.  **Run Frontend Dev Server:**
        *   `pnpm run dev` (or `npm run dev`)
        *   This starts the Vite development server, typically on `http://localhost:5173`.
        *   When the `AiStudio4` C# project is run in **Debug** configuration, it is configured to proxy requests to this Vite dev server. This allows for Hot Module Replacement (HMR) and a faster frontend development cycle.
    4.  **Build Frontend for Production:**
        *   `pnpm run build` (or `npm run build`)
        *   This command compiles the React/TypeScript application and outputs the static assets to the `AiStudio4/AiStudioClient/dist` folder. These are the files that the C# application serves in Release mode or when the Vite dev server is not running.

### 2.3 Initial Setup

#### 2.3.1 Setting the Project Path
This is the **most crucial first step**.
1.  Go to `File > Project > Set Project Path...` in the application menu.
2.  Select the root directory of your software development project.
3.  This path is essential for context-aware AI tasks, file operations, Git integration, and .NET project analysis.
    The window title will update to reflect the current project path.

#### 2.3.2 Configuring AI Service Providers
To use AI models, you need to configure their service providers (e.g., OpenAI, Anthropic).
1.  Open the main settings:
    *   Use the Command Bar: Press `Ctrl+K` (or `Cmd+K` on Mac), type "Providers", and select "Service Providers".
    *   Or, use the application menu if available (e.g., `File > Settings` and navigate to the "Providers" tab).
2.  In the "Service Providers" section:
    *   Click "Add Provider".
    *   **Friendly Name:** A name you choose (e.g., "My OpenAI Account").
    *   **Service Name:** Select the AI service type from the dropdown (e.g., `NetOpenAi` for OpenAI, `Claude` for Anthropic, `Gemini` for Google).
    *   **API URL:** The base URL for the API (e.g., `https://api.openai.com/v1`).
    *   **API Key:** Your personal API key for the service.
    *   **Icon Name (Optional):** Select an icon to represent this provider.
    *   Click "Add Provider" to save.

#### 2.3.3 Adding AI Models
Once providers are configured, add specific AI models you want to use.
1.  In the settings modal, navigate to the "Models" tab.
2.  Click "Add Model".
3.  Fill in the details:
    *   **Friendly Name:** A display name for the model (e.g., "GPT-4 Turbo").
    *   **Model Name:** The exact model identifier used by the API (e.g., `gpt-4-turbo`).
    *   **Service Provider:** Select one of your configured providers from the dropdown.
    *   **Input/Output Token Price:** (Optional, for cost tracking) Enter the price per 1 million tokens for input and output.
    *   **Color:** (Optional) Choose a color for UI identification.
    *   **Notes:** (Optional) Any personal notes about this model.
    *   **Additional Parameters (JSON):** (Optional, Advanced) JSON string for extra parameters to send with requests to this model.
    *   **Supports Prefill:** Check if the model supports prefilling assistant responses.
    *   **Requires 1.0 Temperature:** Check if the model strictly requires a temperature of 1.0.
    *   **Reasoning Effort:** (For compatible models) Select `low`, `medium`, or `high`.
    *   **Enable Text-to-Speech (TTS):** Check if this is a TTS model.
    *   **TTS Voice Name:** If TTS is enabled, specify the voice name (e.g., "Kore" for Gemini).
4.  Click "Add Model" to save.

#### 2.3.4 Selecting Default Models
AiStudio4 uses a primary model for main chat interactions and a secondary model for tasks like summaries or quick tool responses.
1.  In the Input Bar (bottom section of the main window), you'll see a "Model Status" area.
2.  Click on the "Primary Model" button (or placeholder text if none is set).
3.  The Command Bar will open, filtered to "Primary Models". Select your desired primary model.
4.  Click on the "Secondary Model" button.
5.  The Command Bar will open, filtered to "Secondary Models". Select your desired secondary model.

These default models can also be associated with System Prompts (see [Section 4.3.1](#431-system-prompts)).

---

## 3. Core Concepts

### 3.1 The Main Interface
*   **Header:** Contains the Command Bar access, Pinned Shortcuts, Model Status, and System Prompt display.
*   **Chat View:** The central area where your conversation with the AI is displayed.
*   **Input Bar:** Located at the bottom, this is where you type messages, manage attachments, select tools, and access system prompts. It also shows temperature settings (see [Section 5.1.5](#515-temperature-control)) and status messages.
*   **Sidebar (Toggle: `Ctrl+B` or `Cmd+B`):**
    *   **Conversation History:** Lists your past conversations. Click to load one.
    *   **Search Bar:** Search through your conversation history.
    *   **Current Conversation Tree View:** An interactive D3.js visualization of the current conversation's message branches.
*   **Panels:** Other panels like Settings, Tool Library, etc., can be opened via the Command Bar or menus. The layout features resizable panels (see [Section 5.3.6](#536-resizable-panels)).

### 3.2 Project Path: Its Importance
The **Project Path** is the root directory of your software development project or working folder. Setting this correctly is **critical** because:
*   It provides context to AI models for tasks like code generation or analysis.
*   File system tools (read, write, search) operate relative to this path (with safety checks).
*   Git integration tools use this path as the repository root.
*   .NET Project Analysis scans projects within this path.

### 3.3 AI Models & Providers
*   **Service Providers:** These are the AI services you connect to (e.g., OpenAI, Anthropic). You configure them with API keys and URLs.
*   **AI Models:** These are specific models offered by providers (e.g., `gpt-4-turbo`, `claude-3-opus-20240229`). You add them to AiStudio4 and associate them with a configured provider. This allows you to manage multiple models from different providers in one place.

### 3.4 Tools: How They Work
Tools are functions that the AI can use to perform actions or retrieve information.
*   **Built-in Tools:** AiStudio4 comes with a rich set of tools for file operations, web searching, developer integrations, etc.
*   **Model Context Protocol (MCP) Tools:** You can connect AiStudio4 to external MCP servers, making their tools available to the AI.
*   **Custom Tools:** (Advanced) You can define your own tools using JSON schemas, and also import/export them. See [Section 5.2.3](#523-creating-importing--exporting-custom-tools).
*   **Tool Activation:** You activate tools you want the AI to consider using for the current conversation. The AI then decides if and when to use an active tool based on your prompts. The `Stop` tool is always included when tools are active, allowing the AI to terminate the tool loop.

### 3.5 Conversations
*   **Branched Structure:** Conversations are not strictly linear. An AI response can lead to multiple user follow-ups, creating branches. The Tree View helps visualize this.
*   **Persistent Storage:** Conversations are saved locally in your `%APPDATA%/AiStudio4/convs` directory as JSON files.
*   **Message IDs:** Each message has a unique ID and a parent ID, forming the tree structure.

### 3.6 Prompts
*   **System Prompts:** These are instructions that define the AI's role, personality, and context for a conversation. You can create a library of system prompts and associate them with specific models, tools, and even user prompts. They can be set as global defaults or on a per-conversation basis.
*   **User Prompts:** Reusable text snippets, templates, or instructions you can quickly insert into your chat. They can be accessed via Slash Commands (e.g., `/my-template` or `/MyProjectFile.cs`).

---

## 4. Using AiStudio4

### 4.1 Chatting with AI

#### 4.1.1 Sending Messages
1.  Type your message into the main input area at the bottom of the screen.
2.  Press `Enter` to send. Use `Shift+Enter` for multi-line input.
3.  AI responses will stream in.

#### 4.1.2 Interjecting During Tool Use
If the AI is taking a long time processing a tool or you realize you need to provide more information:
1.  Type your follow-up message or clarification in the input bar (it remains active).
2.  Press `Ctrl+Enter` (or `Cmd+Enter` on Mac) or click the "Interject" button (💬➕ icon).
3.  Your interjection will be incorporated by the AI in its next processing step. This allows you to correct the AI or provide more context without fully interrupting its flow.

#### 4.1.3 Rich Content (Markdown, Diagrams, Tables)
AiStudio4 renders AI responses that include:
*   **Markdown:** For formatted text, lists, etc.
*   **Code Blocks:** With syntax highlighting for various languages.
*   **Tables:** Rendered from Markdown.
*   **Diagrams:** Using Mermaid, DOT/Graphviz, or even rendering valid JSON or HTML if the AI provides them in the correct format.

### 4.2 Managing Conversations

#### 4.2.1 Conversation History & Sidebar
*   Toggle the Sidebar using `Ctrl+B` (or `Cmd+B`) or via the Command Bar ("Conversation History Toggle").
*   The top section of the Sidebar lists your past conversations, typically with a summary of the first few messages. Click a conversation to load it into the main chat view.
*   Middle-click a conversation in the list to delete it (with confirmation).

#### 4.2.2 Searching Conversations
1.  In the Sidebar, use the search bar at the top.
2.  Type your search term. Results will update live, highlighting conversations containing the term.
3.  Clicking a search result loads the conversation. Messages containing the search term will be highlighted in the Conversation Tree View, and you can cycle through them.

#### 4.2.3 Conversation Tree View
*   The bottom section of the Sidebar displays an interactive tree of the currently active conversation.
*   Nodes represent messages ("You" for user, "AI" for assistant).
*   Click a node to jump to that point in the conversation history in the main Chat View. The selected message and its direct ancestors will be shown.
*   Middle-click a node in the tree to delete that message and all its descendants (with confirmation). This is useful for pruning irrelevant branches.
*   Use the control buttons (bottom-right of the tree view area) to zoom, pan, center the tree, or focus on the latest message.

### 4.3 Working with Prompts

#### 4.3.1 System Prompts
System prompts guide the AI's behavior for an entire conversation.
1.  **Access Library:**
    *   Command Bar (`Ctrl+K`): Type "System Prompts Library".
    *   Input Bar: Click the "System Prompt" display area.
2.  **Creating/Editing:**
    *   In the library, click "Create New Prompt" or edit an existing one.
    *   **Title:** A name for your prompt.
    *   **Content:** The actual system prompt text. You can use `{ProjectPath}` which will be replaced with the current project path, and `{CommonAiMistakes}` which will insert the content of `CommonAiMistakes.md` from your project root (if it exists).
    *   **Description:** Optional explanation.
    *   **Tags:** Keywords for organization.
    *   **Set as Default:** If checked, this prompt will be used for new conversations.
    *   **Include Git Diff:** If checked, the current `git diff HEAD` output from your project will be automatically attached as a file when this prompt is applied.
    *   **Associated Tools:** Select tools from your library that should be automatically activated when this prompt is used.
    *   **Associated User Prompt:** Select a User Prompt to be automatically inserted into the input bar when this System Prompt is applied.
    *   **Associated Models:** Select a Primary and/or Secondary AI model to be automatically selected when this prompt is applied.
    *   **Associated MCP Servers:** Select MCP servers to be automatically enabled when this prompt is applied.
3.  **Applying:**
    *   From the library, click "Apply" on a prompt card.
    *   From the Command Bar, search for the system prompt by title.
    *   The selected system prompt will apply to the current or next new conversation. The Input Bar display will update.

#### 4.3.2 User Prompts
User prompts are reusable text snippets.
1.  **Access Library:**
    *   Command Bar (`Ctrl+K`): Type "User Prompts Library".
2.  **Creating/Editing:**
    *   In the library, click "Create New Prompt" or edit an existing one.
    *   **Title:** A name for your prompt.
    *   **Content:** The text snippet.
    *   **Description:** Optional explanation.
    *   **Shortcut:** (Optional) A short command (e.g., `/my-template`) to quickly insert this prompt via Slash Commands.
    *   **Tags & Favorite:** For organization. Mark prompts as favorites for easy access.
3.  **Using with Slash Commands:**
    *   In the Input Bar, type `/`. A dropdown will appear.
    *   Start typing the name or shortcut of your User Prompt (e.g., `/MyUserPrompt`).
    *   Select the desired prompt from the list to insert its content.
    *   You can also type `/` followed by a project file name (e.g., `/MyClass.cs`) to see matching files from your project. Selecting a file inserts its path. Shift+selecting a file attaches it.

### 4.4 Using Tools

#### 4.4.1 Activating Tools
1.  **Access Tool Management:**
    *   Input Bar: Click the "Tools" button (wrench icon). This opens the Tool Library modal.
    *   Command Bar (`Ctrl+K`): Search for "Manage Tools" or directly search for a tool by name to toggle its activation.
2.  **Tool Library Modal:**
    *   Browse tools by category or search.
    *   Click "Select Tool" on a tool card to activate it for the current session. The button will change to "Selected". Click again to deactivate.
    *   Active tools are listed in the Input Bar. Middle-click an active tool in the Input Bar to deactivate it. Middle-click the "Tools" button itself to deactivate all active tools.
    *   Double-click a category name in the Tool Library to select all tools in that category.

#### 4.4.2 AI-Driven Tool Use
Once tools are active, the AI will decide whether to use them based on your conversation. For example, if you ask the AI to "search for all C# files containing 'TODO'", and the `FileRegExSearch` tool is active, the AI may choose to use it. Some tools, like certain Vite commands, may prompt for user confirmation before execution.

#### 4.4.3 Explicit Tool Requests
You can also explicitly ask the AI to use a tool, e.g., "Use the ReadFiles tool to read `MyClass.cs`".

### 4.5 Attachments

#### 4.5.1 Attaching Files (Images, Text)
1.  In the Input Bar, click the "Paperclip" icon.
2.  Select "Attach file". A file dialog will open.
3.  Choose one or more files (images like PNG, JPG, PDF; or text-based files like .txt, .cs, .md, .json).
4.  Selected attachments will appear in a preview area in the InputBar.
5.  When you send your message, these attachments will be included. Text file content is often appended to your prompt, while images are sent as image data.

#### 4.5.2 Attaching Git Diff
1.  Click the "Paperclip" icon in the Input Bar.
2.  Select "Git Diff".
3.  If your current `ProjectPath` is a Git repository, AiStudio4 will generate a `git diff HEAD` and also list new/untracked files.
4.  This diff will be added as a text file attachment named `git-diff.txt`.

#### 4.5.3 Attaching Image from Clipboard
1.  Click the "Paperclip" icon in the Input Bar.
2.  Select "Image from Clipboard".
3.  If an image is present on your system clipboard, it will be attached as `clipboard-image.png`.

### 4.6 Audio Transcription
AiStudio4 can transcribe audio from media files using a local Whisper.net model (potentially via Conda for WhisperX for advanced features).
1.  **Set Conda Path (Optional but recommended for advanced features):** Ensure your Conda path (specifically to `activate.bat` in your Conda scripts directory) is set via `File > Settings > Set Conda Path...`. You'll also need a Conda environment with `whisperx` installed.
2.  **Transcribe:**
    *   From the application menu, select `Transcribe > Transcribe Media File...`.
    *   Choose an audio or video file.
    *   The transcription process will run (a command window may appear).
3.  **Insert Transcription:**
    *   Once complete, a message will indicate the transcription is ready.
    *   Select `Transcribe > Insert Last Transcription` from the menu.
    *   The transcribed text (in VTT format) will be appended to your current input in the Input Bar.

---

## 5. Key Features in Detail

### 5.1 AI Model Integration

#### 5.1.1 Supported Models and Providers
AiStudio4 supports:
*   **OpenAI:** Models like GPT-3.5, GPT-4, etc. Requires OpenAI API key. (Uses `NetOpenAi` service type).
*   **Anthropic Claude:** Models like Claude 3 Opus, Sonnet, Haiku. Requires Anthropic API key. (Uses `Claude` service type).
*   **Google Gemini:** Models like Gemini Pro. Requires Google AI Studio API key. (Uses `Gemini` service type).
*   **Ollama:** Any model hosted by a local Ollama instance that exposes an OpenAI-compatible API. Configure as an OpenAI provider, pointing the URL to your Ollama instance (e.g., `http://localhost:11434/v1`). (Uses `NetOpenAi` service type).
*   **Model Context Protocol (MCP):** Connect to any AI service or tool provider that adheres to the MCP standard.

#### 5.1.2 Configuring Service Providers
(See [Section 2.3.2](#232-configuring-ai-service-providers))

#### 5.1.3 Managing AI Models
(See [Section 2.3.3](#233-adding-ai-models))
*   **Editing Models:** In Settings > Models, click the pencil icon on a model card.
*   **Deleting Models:** Click the trash icon.
*   **Favorite Models:** Click the star icon to mark a model as a favorite.

#### 5.1.4 Model-Specific Features
When adding or editing a model, you can configure:
*   **Supports Prefill:** (Claude-specific) Some models allow "prefilling" the assistant's response to guide generation.
*   **Requires 1.0 Temperature:** Some models perform best or only work correctly with a temperature of 1.0. Check this if applicable.
*   **Reasoning Effort:** (For some OpenAI models) Can be set to `low`, `medium`, or `high` to influence model processing.
*   **Text-to-Speech (TTS):** If the model is a TTS model (e.g., some Gemini models), check "Enable Text-to-Speech (TTS)" and provide the `TTS VoiceName` (e.g., "Kore").

#### 5.1.5 Temperature Control
You can easily adjust the AI's creativity/randomness via a slider in the Input Bar. This setting directly influences the 'temperature' parameter sent with requests to the AI model.

### 5.2 Tool System

#### 5.2.1 Overview of Built-in Tools
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

#### 5.2.2 Model Context Protocol (MCP)
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

#### 5.2.3 Creating, Importing & Exporting Custom Tools
(Advanced) Users can define their own tools by providing a JSON schema. Tools can be organized into categories.
1.  In the Tool Library modal, click "New Tool".
2.  Provide a Name, Description, Categories, and the JSON schema defining the tool's parameters and function.
3.  **Schema Type:**
    *   `function`: Standard tool definition.
    *   `template`: If the tool's "schema" is a text template, the AI will fill it in.
    *   `custom`: For other custom behaviors (not typically user-defined).
4.  **Output Filetype:** If the tool generates a specific file type (e.g., `theme`), specify it here. This can enable special handling (like installing a theme).
5.  The Tool Library also supports importing and exporting tool definitions.

#### 5.2.4 Tool Extra Properties
Some built-in tools (e.g., `FileSearchTool`, `DirectoryTreeTool`, `SentryTool`) have "Extra Properties" that can be configured to customize their behavior without changing their core code.
1.  In the Tool Library modal, edit a built-in tool.
2.  The "Extra Properties" section will show available key-value pairs.
    *   For CSV lists (e.g., `excludedFileExtensions (CSV)`), enter comma-separated values.
    *   For Sentry, you'd configure `orgSlug`, `apiToken`, etc.
3.  Changes are saved and persist in `%APPDATA%/AiStudio4/builtinToolExtraProps.json`.

### 5.3 UI and Customization

#### 5.3.1 Theming
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

#### 5.3.2 Command Bar
*   Access by pressing `Ctrl+K` (or `Cmd+K`).
*   Type to search for commands, settings, prompts, tools, models, etc.
*   Use arrow keys to navigate and `Enter` to execute.
*   Pinned commands (see below) also appear here.

#### 5.3.3 Pinned Shortcuts
*   A customizable bar in the App Header for one-click access to frequently used commands.
*   **Pinning:** In the Command Bar, hover over a command. A "Pin" icon will appear. Click it to add the command to your Pinned Shortcuts.
*   **Unpinning:** Right-click a pinned shortcut button in the Header.
*   **Reordering:** Drag and drop pinned shortcut buttons in the Header to reorder them.
*   **Renaming:** Middle-click a pinned shortcut button to open a dialog to rename it and change its icon.

#### 5.3.4 Slash Commands
In the Input Bar:
*   Type `/` to open a dropdown.
*   Start typing the name of a User Prompt or a file path from your current `ProjectPath`.
*   **User Prompts:** Selecting a User Prompt inserts its content.
*   **File Paths:**
    *   Selecting a file path inserts the relative path into the input.
    *   `Shift+Click` on a file path in the dropdown will attach the file instead of inserting its path.

#### 5.3.5 Appearance Settings
*   Command Bar (`Ctrl+K`) > "Appearance Settings".
*   **Font Size:** Adjust the global font size for the application.
*   **Dark Mode:** (Default) AiStudio4 primarily uses a dark theme aesthetic. Theming allows full customization.

#### 5.3.6 Resizable Panels
The application features a customizable layout with resizable panels for elements like the conversation history/tree view, and potentially other views in the future.

### 5.4 Project Integration

#### 5.4.1 .NET Project Analysis
*   From the application menu: `Project > Analyze .NET Projects`.
*   This tool scans your current `ProjectPath` for C# project files (`.csproj`) and source files (`.cs`).
*   It extracts namespace, class, and member (methods, properties, fields, etc.) information.
*   The results are saved to a `DotNetProjectAnalysis.txt` file in your project root.
*   This file can then be used as context for AI tasks, for example, by attaching it to a prompt or having the AI read it using the `ReadFiles` tool.

#### 5.4.2 Project Source Code Packaging
*   From the application menu: `Project > Pack Project Source Code`.
*   This tool creates a comprehensive XML package containing the source code of your project.
*   It respects `.gitignore` rules.
*   You can configure which file types to include (`Project > Set Packer Include File Types...`) and specific filenames/patterns to exclude (`Project > Set Packer Exclude Filenames...`).
*   The output XML includes the directory structure and the content of each included file. This is useful for providing a large codebase snapshot to an AI that might have context window limitations or no direct file system access.

### 5.5 Backend and Core Functionality

#### 5.5.1 Real-time Communication
WebSockets are extensively used for:
*   Streaming AI responses.
*   Live conversation updates across different UI components.
*   Displaying status messages from the backend.
*   (Potentially) Project file system monitoring for real-time updates.

---

## 6. Advanced Configuration

### 6.1 Backend Configuration (`appsettings.json`)
Located in the `AiStudio4` project directory (when running from source) or installation directory, this file contains basic backend settings.
*   `WebServer:Port`: Defines the port for the embedded Kestrel web server (default: `35005`).

### 6.2 User-Specific Settings (`%APPDATA%/AiStudio4/`)
Most user preferences and data are stored in JSON files within this directory on Windows.
*   **`settings.json`**: Main user settings.
    *   `ModelList`: Your configured AI models.
    *   `ServiceProviders`: Your configured AI service providers (including encrypted API keys).
    *   `DefaultModelGuid`, `SecondaryModelGuid`: GUIDs of your default models.
    *   `Temperature`, `UsePromptCaching`, `StreamResponses`, `UseEmbeddings`, `EmbeddingModel`.
    *   `ProjectPath`: The **critical** path to your current project.
    *   `EncryptedYouTubeApiKey`, `EncryptedGitHubApiKey`, `EncryptedAzureDevOpsPAT`: Encrypted API keys.
    *   `CondaPath`: Path to your Conda environment.
    *   `AllowConnectionsOutsideLocalhost`: (Requires restart) If `true`, the Kestrel server listens on all network interfaces.
    *   `PackerIncludeFileTypes`, `PackerExcludeFilenames`: Settings for the Project Packager.
    *   `ConversationZipRetentionDays`, `ConversationDeleteZippedRetentionDays`: Settings for conversation archiving.
*   **`systemPrompts/`**: Directory containing your saved System Prompts as `.prompt.json` files.
*   **`UserPrompts/`**: Directory containing your saved User Prompts as `.prompt.json` files.
*   **`Tools/toolLibrary.json`**: Your custom tool definitions and categories.
*   **`PinnedCommands/pinnedCommands.json`**: Configuration for your Pinned Shortcuts.
*   **`themes.json`**: User-imported/created UI themes and the active theme ID.
*   **`builtinToolExtraProps.json`**: Persisted user-configured `ExtraProperties` for built-in tools.
*   **`Config/mcpServers.json`**: Definitions for Model Context Protocol (MCP) servers.
*   **`convs/`**: Directory where individual conversation JSON files are stored.
    *   **`convs/archive/`**: Subdirectory where old conversations are zipped.

### 6.3 API Key Management
Sensitive API keys for external services are managed via the application menu:
*   `File > Settings > Set YouTube API Key...`
*   `File > Settings > Set GitHub API Key...`
*   `File > Settings > Set Azure DevOps PAT...`
These keys are encrypted and stored in `settings.json`.

---

## 7. Troubleshooting
*(This section can be expanded with common issues and solutions as they are identified.)*
*   **Tool Not Working:**
    *   Ensure the tool is activated in the Tool Library or via Command Bar.
    *   Check if the associated System Prompt (if any) has the tool enabled.
    *   For API-based tools (GitHub, Azure DevOps, Sentry, YouTube), ensure the relevant API key/PAT is correctly configured in `File > Settings` or in the tool's `ExtraProperties`.
    *   Check the `_AiStudio4\bin\Debug\net9.0-windows\debug.log` file (path may vary based on build type/installation) for error messages.
*   **AI Not Using an Active Tool:**
    *   The AI makes its own decisions. Try rephrasing your prompt to be more explicit about the desired action or tool.
    *   Ensure the tool's description and schema are clear and guide the AI effectively.
*   **Project Path Dependent Features Not Working:**
    *   Verify `File > Project > Set Project Path...` is set to the correct root directory of your project.
*   **Audio Transcription Fails:**
    *   Ensure Conda is installed and the path to `activate.bat` is correctly set in `File > Settings > Set Conda Path...` if you are using Conda-dependent WhisperX features.
    *   Ensure you have a Conda environment with `whisperx` (and its dependencies like `ffmpeg`) installed. For basic Whisper.net, Conda might not be strictly necessary.

---

## 8. Technical Stack
*   **Backend & Desktop Shell:** .NET 9/C#, WPF, ASP.NET Core (Kestrel), WebView2, Newtonsoft.Json, ModelContextProtocol SDK, OpenAI SDK, Whisper.net, Roslyn (Microsoft.CodeAnalysis).
*   **Frontend (SPA in `AiStudioClient`):** React 19, TypeScript, Vite, Tailwind CSS, Shadcn/ui, Zustand, Axios, D3.js, Mermaid.js, Viz.js (@hpcc-js/wasm), React Markdown, React Syntax Highlighter, Lucide Icons, LobeHub Icons.
*   **Communication:** WebSockets, HTTP REST API.

---

## 9. Contributing
We welcome contributions! Whether it's reporting a bug, suggesting a new feature, or submitting code changes, your input is valuable. Please refer to a `CONTRIBUTING.md` file (if available in the repository) for detailed guidelines on:
*   Code of conduct.
*   Process for reporting issues.
*   How to submit pull requests.
*   Coding standards and patterns used in the project.

### 9.1 Development Notes
*   **Project Structure:** The application is divided into a C# backend/host (`AiStudio4`) and a TypeScript/React frontend (`AiStudio4/AiStudioClient`).
*   **Backend Development:** Typically done in Visual Studio. Key services are registered in `AiStudio4/App.xaml.cs`. API request handlers reside in `AiStudio4/InjectedDependencies/RequestHandlers/`. Core tool logic is found in `AiStudio4/Core/Tools/`.
*   **Frontend Development:** Uses Vite, with `pnpm` (or `npm`) for package management. Source code is in `AiStudio4/AiStudioClient/src/`. For a detailed frontend structure and data flow explanation, see `AiStudio4/Docs/Feature Designs/ProjectStructure.txt` (if available).
*   **Running Tests:** (Details on running tests would go here if available - e.g., "There aren't any. Yet...")

---

## 10. License
This project is licensed under the MIT License. See the `LICENSE.md` file for full details.
Dependency licenses can be viewed via `File > Licenses` in the application.

---

## 11. Author & Acknowledgements
*   **Author:** Max Hedge (@stringandstickytape)
*   **Acknowledgements:**
    *   Inspiration for README structure from Bane Sullivan's guide on writing good READMEs.

---

## 12. Further Reading & Links
*   (If available) Detailed Frontend Project Structure: `./AiStudio4/Docs/Feature Designs/ProjectStructure.txt`
*   (If available) Backend Architectural Overview: `./ClaudeCode.md`
*   (If available) UI Theming Guide: `./AiStudio4/Theming.md`
*   (If available) Internal Design Documents: `./AiStudio4/Docs/Feature Designs/`
*   [Whisper.net Library](https://github.com/sandrohanea/whisper.net)
*   [OpenAI .NET Library](https://github.com/openai/openai-dotnet)
