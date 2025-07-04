# 3. Core Concepts

## 3.1 The Main Interface
*   **Header:** Contains the Command Bar access, Pinned Shortcuts, and quick access controls.
*   **Chat View:** The central area where your conversation with the AI is displayed.
*   **Input Bar:** Located at the bottom with an improved layout featuring:
    *   **Top Action Bar:** Model status, tools, and temperature controls in a consolidated horizontal layout
    *   **Status Messages:** Prominently displayed for better visibility
    *   **System Prompt Section:** Easy access to system prompt management
    *   **Main Input Area:** Text input field with attachment and send controls
*   **Sidebar (Toggle: `Ctrl+B` or `Cmd+B`):**
    *   **Conversation History:** Lists your past conversations. Click to load one.
    *   **Search Bar:** Search through your conversation history.
    *   **Current Conversation Tree View:** An interactive D3.js visualization of the current conversation's message branches.
*   **Panels:** Other panels like Settings, Tool Library, etc., can be opened via the Command Bar or menus. 

## 3.2 Active Project Path: Its Importance
AiStudio4 supports managing multiple projects. The **Active Project Path** is the root directory of the currently selected project. This path is **critical** because:
*   It provides the primary context to AI models for tasks like code generation or analysis.
*   File system tools (read, write, search) operate relative to this path (with safety checks).
*   Git integration tools use this path as the repository root.
*   The active project can be switched quickly via the `Project > Manage Projects...` menu or automatically by selecting a System Prompt that is associated with a specific project.

## 3.3 AI Models & Providers
*   **Service Providers:** These are the AI services you connect to (e.g., OpenAI, Anthropic, Llama.cpp). You configure them with API keys and URLs.  Presets are provided for Anthropic, OpenAI, Google and OpenRouter - you will need to fill out the relevant API keys.
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
*   **System Prompts:** These are instructions that define the AI's role, personality, and context. You can create a library of system prompts and associate them with specific models, tools, user prompts, and **projects**. When a system prompt with an associated project is activated, that project automatically becomes the active one for the application.
*   **User Prompts:** Reusable text snippets, templates, or instructions you can quickly insert into your chat. They can be accessed via Slash Commands (e.g., `/my-template` or `/MyProjectFile.cs`).