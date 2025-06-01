# 4. Using AiStudio4

## 4.1 Chatting with AI

### 4.1.1 Sending Messages
1.  Type your message into the main input area at the bottom of the screen.
2.  Press `Enter` to send. Use `Shift+Enter` for multi-line input.
3.  AI responses will stream in.

### 4.1.2 Interjecting During Tool Use
If the AI is taking a long time processing a tool or you realize you need to provide more information:
1.  Type your follow-up message or clarification in the input bar (it remains active).
2.  Press `Ctrl+Enter` (or `Cmd+Enter` on Mac) or click the "Interject" button (💬➕ icon).
3.  Your interjection will be incorporated by the AI in its next processing step. This allows you to correct the AI or provide more context without fully interrupting its flow.

### 4.1.3 Rich Content (Markdown, Diagrams, Tables)
AiStudio4 renders AI responses that include:
*   **Markdown:** For formatted text, lists, etc.
*   **Code Blocks:** With syntax highlighting for various languages.
*   **Tables:** Rendered from Markdown.
*   **Diagrams:** Using Mermaid, DOT/Graphviz, or even rendering valid JSON or HTML if the AI provides them in the correct format.

## 4.2 Managing Conversations

### 4.2.1 Conversation History & Sidebar
*   Toggle the Sidebar using `Ctrl+B` (or `Cmd+B`) or via the Command Bar ("Conversation History Toggle").
*   The top section of the Sidebar lists your past conversations, typically with a summary of the first few messages. Click a conversation to load it into the main chat view.
*   Middle-click a conversation in the list to delete it (with confirmation).

### 4.2.2 Searching Conversations
1.  In the Sidebar, use the search bar at the top.
2.  Type your search term. Results will update live, highlighting conversations containing the term.
3.  Clicking a search result loads the conversation. Messages containing the search term will be highlighted in the Conversation Tree View, and you can cycle through them.

### 4.2.3 Conversation Tree View
*   The bottom section of the Sidebar displays an interactive tree of the currently active conversation.
*   Nodes represent messages ("You" for user, "AI" for assistant).
*   Click a node to jump to that point in the conversation history in the main Chat View. The selected message and its direct ancestors will be shown.
*   Middle-click a node in the tree to delete that message and all its descendants (with confirmation). This is useful for pruning irrelevant branches.
*   Use the control buttons (bottom-right of the tree view area) to zoom, pan, center the tree, or focus on the latest message.

## 4.3 Working with Prompts

### 4.3.1 System Prompts
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

### 4.3.2 User Prompts
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

## 4.4 Using Tools

### 4.4.1 Activating Tools
1.  **Access Tool Management:**
    *   Input Bar: Click the "Tools" button (wrench icon). This opens the Tool Library modal.
    *   Command Bar (`Ctrl+K`): Search for "Manage Tools" or directly search for a tool by name to toggle its activation.
2.  **Tool Library Modal:**
    *   Browse tools by category or search.
    *   Click "Select Tool" on a tool card to activate it for the current session. The button will change to "Selected". Click again to deactivate.
    *   Active tools are listed in the Input Bar. Middle-click an active tool in the Input Bar to deactivate it. Middle-click the "Tools" button itself to deactivate all active tools.
    *   Double-click a category name in the Tool Library to select all tools in that category.

### 4.4.2 AI-Driven Tool Use
Once tools are active, the AI will decide whether to use them based on your conversation. For example, if you ask the AI to "search for all C# files containing 'TODO'", and the `FileRegExSearch` tool is active, the AI may choose to use it. Some tools, like certain Vite commands, may prompt for user confirmation before execution.

### 4.4.3 Explicit Tool Requests
You can also explicitly ask the AI to use a tool, e.g., "Use the ReadFiles tool to read `MyClass.cs`".

## 4.5 Attachments

### 4.5.1 Attaching Files (Images, Text)
1.  In the Input Bar, click the "Paperclip" icon.
2.  Select "Attach file". A file dialog will open.
3.  Choose one or more files (images like PNG, JPG, PDF; or text-based files like .txt, .cs, .md, .json).
4.  Selected attachments will appear in a preview area in the InputBar.
5.  When you send your message, these attachments will be included. Text file content is often appended to your prompt, while images are sent as image data.

### 4.5.2 Attaching Git Diff
1.  Click the "Paperclip" icon in the Input Bar.
2.  Select "Git Diff".
3.  If your current `ProjectPath` is a Git repository, AiStudio4 will generate a `git diff HEAD` and also list new/untracked files.
4.  This diff will be added as a text file attachment named `git-diff.txt`.

### 4.5.3 Attaching Image from Clipboard
1.  Click the "Paperclip" icon in the Input Bar.
2.  Select "Image from Clipboard".
3.  If an image is present on your system clipboard, it will be attached as `clipboard-image.png`.

## 4.6 Audio Transcription
AiStudio4 can transcribe audio from media files using a local Whisper.net model via Conda and WhisperX.
1.  **Set Conda Path (Optional but recommended for advanced features):** Ensure your Conda path (specifically to `activate.bat` in your Conda scripts directory) is set via `File > Settings > Set Conda Path...`. You'll also need a Conda environment with `whisperx` installed.
2.  **Transcribe:**
    *   From the application menu, select `Transcribe > Transcribe Media File...`.
    *   Choose an audio or video file.
    *   The transcription process will run (a command window may appear).
3.  **Insert Transcription:**
    *   Once complete, a message will indicate the transcription is ready.
    *   Select `Transcribe > Insert Last Transcription` from the menu.
    *   The transcribed text (in VTT format) will be appended to your current input in the Input Bar.