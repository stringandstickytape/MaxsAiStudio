# 4. Using AiStudio4

## 4.1 Chatting with AI

### 4.1.1 Sending Messages
1.  Type your message into the main input area at the bottom of the screen.
2.  Press `Enter` to send. Use `Shift+Enter` for multi-line input.
3.  AI responses will stream in.

### 4.1.2 Input Bar Layout
The Input Bar has been redesigned with an improved layout featuring:
*   **Top Action Bar:** Contains model status, tools, and temperature controls in a consolidated horizontal layout
*   **Status Messages:** Displayed prominently above the input area for better visibility
*   **System Prompt Section:** Located below the status messages for easy access
*   **Main Input Area:** The text input field with attachment and send controls
*   **Organized Controls:** All interactive elements are logically grouped for better usability

### 4.1.3 Interjecting During Tool Use
If the AI is taking a long time processing a tool or you realize you need to provide more information:
1.  Type your follow-up message or clarification in the input bar (it remains active).
2.  Press `Ctrl+Enter` (or `Cmd+Enter` on Mac) or click the "Interject" button (💬➕ icon).
3.  Your interjection will be incorporated by the AI in its next processing step. This allows you to correct the AI or provide more context without fully interrupting its flow.

### 4.1.5 Viewing Message Metadata
Each AI message includes metadata such as the model used, response time, and token cost. This information is displayed below each message and can be useful for understanding the performance and cost of different models.
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
    *   **Include Git Diff:** If checked, the current `git diff HEAD` output from your project will be automatically attached as a file when this prompt is applied.
    *   **Associated Tools:** Select tools from your library that should be automatically activated when this prompt is used.
    *   **Associated User Prompt:** Select a User Prompt to be automatically inserted into the input bar when this System Prompt is applied.
    *   **Associated Models:** Select a Primary and/or Secondary AI model to be automatically selected when this prompt is applied.
    *   **Associated MCP Servers:** Select MCP servers to be automatically enabled when this prompt is applied.
    *   **Associated Project:** Select a project from your defined list. When this system prompt is activated, the associated project will automatically become the active project for the application.
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

## 4.7 Azure DevOps Wiki Sync
AiStudio4 can automatically synchronize system prompts with Azure DevOps wiki pages, enabling teams to maintain centralized documentation that automatically updates AI behavior.

### 4.7.1 Setting Up Wiki Sync
1.  **Configure Azure DevOps PAT:**
    *   Go to `File > Settings > Set Azure DevOps PAT...`
    *   Enter your Azure DevOps Personal Access Token with wiki read permissions
2.  **Configure Wiki Sync Settings:**
    *   Go to `File > Wiki Sync > Configure Wiki Sync...`
    *   **Azure DevOps Organization:** Enter your organization name (e.g., 'mycompany')
    *   **Azure DevOps Project:** Enter your project name
    *   **Wiki Identifier:** Usually `ProjectName.wiki`
    *   **Wiki Page Path:** Path to your wiki page (e.g., `/Instructions/MasterSystemPrompt`)
    *   **System Prompt to Update:** Select the system prompt that will receive the wiki content
    *   Click "Save Configuration"
3.  **Enable Wiki Sync:**
    *   Go to `File > Wiki Sync > Enable Wiki Sync` and ensure it's checked
    *   Restart the application for changes to take effect

### 4.7.2 How Wiki Sync Works
*   **Automatic Sync:** The sync occurs automatically each time you start AiStudio4
*   **Content Updates:** If the target system prompt exists, its content is updated with the latest wiki content
*   **New Prompt Creation:** If the target system prompt doesn't exist, a new one is created automatically
*   **Change Detection:** Sync only occurs when the wiki content has actually changed since the last sync
*   **Error Handling:** Any sync errors are logged and won't prevent the application from starting

### 4.7.3 Wiki Sync Use Cases
*   **Team Collaboration:** Maintain consistent system prompts across all team members
*   **Centralized Documentation:** Keep AI behavior documentation in your existing Azure DevOps wiki
*   **Automatic Updates:** Ensure AI prompts stay current with project requirements and guidelines
*   **Version Control:** Leverage Azure DevOps wiki's version control for prompt management

## 4.8 Project Management
You can define and switch between multiple development projects.

1.  **Open Management Dialog:** Go to `Project > Manage Projects...` to open the project management window.
2.  **Add a Project:** Click "Add", provide a name and path for your project, and save.
3.  **Set Active Project:** Select a project from the list and click "Set Active". The main window will update to reflect this new context.
4.  **Edit/Delete:** You can also edit the details of or delete existing projects from this dialog.

### 4.9 Project Source Code Packaging
AiStudio4 provides tools to package your project's source code into a comprehensive XML format for AI analysis and sharing.

### 4.9.1 Pack Project Source Code
1. **Access the Feature:**
   * Go to `Project > Pack Project Source Code` in the application menu.
2. **Configure Packaging Options (Optional):**
   * Set file types to include: `Project > Set Packer Include File Types...`
   * Set specific filenames to exclude: `Project > Set Packer Exclude Filenames...`
   * Set folder names to exclude: `Project > Set Packer Exclude Folder Names...`
3. **Create Package:**
   * A file dialog will appear asking where to save the XML package.
   * Choose your desired location and filename (defaults to `ProjectName_SourceCode.xml`).
   * The tool will create a comprehensive XML package containing your project's source code.
   * The packaging process respects `.gitignore` rules and your configured include/exclude settings.
   * The output file path is automatically saved for future repack operations.

### 4.9.2 Repack Project Source Code
1. **Quick Repack:**
   * Go to `Project > Repack Project Source Code` in the application menu.
   * This will automatically re-run the packaging process using the same output file path as your last pack operation.
   * No file dialog is shown - the project is packed directly to the previously used location.
2. **Use Cases:**
   * Iterative development workflows where you frequently update the packaged project snapshot.
   * Quick updates after making changes to your codebase.
   * Automated packaging in development scripts or workflows.
3. **Requirements:**
   * You must have used "Pack Project Source Code" at least once to establish an output file location.
   * If no previous pack location exists, you'll be prompted to use "Pack Project Source Code" first.

### 4.9.3 Package Contents and Uses
* **XML Structure:** The output includes the complete directory structure and content of each included file.
* **AI Integration:** Useful for providing large codebase snapshots to AI models that have context window limitations or no direct file system access.
* **Code Review:** Can be shared with team members or external reviewers for comprehensive code analysis.
* **Documentation:** Serves as a snapshot of your project at a specific point in time.

## 4.10 Google AI Studio Integration
AiStudio4 allows you to import conversations from Google AI Studio and export conversations back to Google Drive for use in Google's ecosystem.

### 4.10.1 Importing Conversations from Google AI Studio
1.  **Ensure Setup:** 
    *   A `credentials.json` file (obtained from your Google Cloud Console for an OAuth 2.0 Desktop app) is required in your `%APPDATA%\AiStudio4\Config\` directory.
    *   You may need to authorize AiStudio4 to access your Google Drive the first time you use this feature.
    **Initiate Import:**
    *   Go to `File > Import/Export > Import from Google AI Studio via Google Drive` in the application menu.
3.  **Select Files:**
    *   A dialog will appear listing JSON files found in your "Google AI Studio" Drive folder.
    *   Select the conversation file(s) you wish to import and click "Import Selected".
4.  **Select Messages (Per File):**
    *   For each selected file, another dialog will appear showing the messages from that conversation.
    *   Select the specific messages you want to include in the imported AiStudio4 conversation.
    *   Click "Import Selected" or "Import All" for that file.
5.  **Completion:**
    *   The selected messages will be converted and saved as a new conversation in AiStudio4.
    *   The new conversation(s) will appear in your Conversation History, and the first successfully imported conversation will be automatically loaded.

### 4.10.2 Exporting Conversations to Google AI Studio
1.  **Ensure Setup:** Similar to importing, ensure Google Drive authorization is complete and the "Google AI Studio" folder exists.
2.  **Initiate Export:**
    *   Go to `File > Import/Export > Upload current thread to Google AI Studio....`
3.  **Conversation Selection:**
    *   AiStudio4 will automatically select the main branch of your most recently modified conversation for export.
4.  **Filename Prompt:**
    *   A dialog will prompt you for a filename. A default name based on the conversation summary and timestamp will be provided (e.g., `MySummary_20250601103000.json`). You can change this if needed.
5.  **Upload:**
    *   The conversation will be converted to Google AI Studio format and uploaded as a JSON file to your "Google AI Studio" folder on Google Drive.
    *   A confirmation message will appear with the file ID if successful.