# 3. Core Concepts

## 3.1 The Main Interface
*   **Header:** Contains the Command Bar access, Pinned Shortcuts, Model Status, and System Prompt display.
*   **Chat View:** The central area where your conversation with the AI is displayed.
*   **Input Bar:** Located at the bottom, this is where you type messages, manage attachments, select tools, and access system prompts. It also shows temperature settings (see [Section 5.1.5](05-key-features-in-detail.md#515-temperature-control)) and status messages.
*   **Sidebar (Toggle: `Ctrl+B` or `Cmd+B`):**
    *   **Conversation History:** Lists your past conversations. Click to load one.
    *   **Search Bar:** Search through your conversation history.
    *   **Current Conversation Tree View:** An interactive D3.js visualization of the current conversation's message branches.
*   **Panels:** Other panels like Settings, Tool Library, etc., can be opened via the Command Bar or menus.

## 3.2 Project Path: Its Importance
The **Project Path** is the root directory of your software development project or working folder. Setting this correctly is **critical** because:
*   It provides context to AI models for tasks like code generation or analysis.
*   File system tools (read, write, search) operate relative to this path (with safety checks).
*   Git integration tools use this path as the repository root.
*   .NET Project Analysis scans projects within this path.

## 3.3 AI Models & Providers
*   **Service Providers:** These are the AI services you connect to (e.g., OpenAI, Anthropic). You configure them with API keys and URLs.  Presets are provided for Anthropic, OpenAI, Google and OpenRouter - you will need to fill out the relevant API keys.
*   **AI Models:** These are specific models offered by providers (e.g., `gpt-4-turbo`, `claude-3-opus-20240229`). You add them to AiStudio4 and associate them with a configured provider. This allows you to manage multiple models from different providers in one place.

## 3.4 Tools: How They Work
Tools are functions that the AI can use to perform actions or retrieve information.
*   **Built-in Tools:** AiStudio4 comes with a rich set of tools for file operations, web searching, developer integrations, etc.
*   **Model Context Protocol (MCP) Tools:** You can connect AiStudio4 to external MCP servers, making their tools available to the AI.
*   **Custom Tools:** (Advanced) You can define your own tools using JSON schemas, and also import/export them. See [Section 5.2.3](05-key-features-in-detail.md#523-creating-importing--exporting-custom-tools).
*   **Tool Activation:** You activate tools you want the AI to consider using for the current conversation. The AI then decides if and when to use an active tool based on your prompts. The `Stop` tool is always included when tools are active, allowing the AI to terminate the tool loop.

## 3.5 Conversations
*   **Branched Structure:** Conversations are not strictly linear. An AI response can lead to multiple user follow-ups, creating branches. The Tree View helps visualize this.
*   **Persistent Storage:** Conversations are saved locally in your `%APPDATA%/AiStudio4/convs` directory as JSON files.
*   **Message IDs:** Each message has a unique ID and a parent ID, forming the tree structure.

## 3.6 Prompts
*   **System Prompts:** These are instructions that define the AI's role, personality, and context for a conversation. You can create a library of system prompts and associate them with specific models, tools, and even user prompts. They can be set as global defaults or on a per-conversation basis.
*   **User Prompts:** Reusable text snippets, templates, or instructions you can quickly insert into your chat. They can be accessed via Slash Commands (e.g., `/my-template` or `/MyProjectFile.cs`).