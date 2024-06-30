# MaxsAiTool
A Windows tool to query various LLM AIs.  Supports branched conversations, history and summaries among others.

The following instructions were written by AI based on the source code of the main form, and then reviewed by the author.

## Preamble

This application is an advanced AI conversation tool that allows users to interact with various AI models, manage conversations, and utilize different features for an enhanced AI interaction experience. It provides a user-friendly interface for conducting AI-assisted conversations, organizing topics, and managing templates.

## Features

- Multiple AI model support
- Conversation management with branching
- Topic and template organization
- Audio recording and transcription (via Whisper)
- Image input capability
- Cost tracking for token usage
- Conversation search functionality
- Customizable system prompts and user inputs
- Network diagram visualization of conversations
- Snippet detection and formatting in responses
- Automatic readme update based on latest GitHub diff and commit message
- Suggested next prompt feature
- Text-to-Speech (TTS) for AI responses
- Low-pass filter for audio processing
- Customizable settings with toggle options
- View HTML in WebView
- Code review function (for project maintainers)

## Detailed Guide

### 1. User Interface Overview

The main window is divided into several sections:
- Input area for user prompts
- System prompt area
- Output display for AI responses
- Conversation network diagram
- Conversation history list
- Category and template selection

### 2. Starting a New Conversation

- Click the "Clear" button to start a new conversation.
- Select a category and template from the dropdown menus to populate the system prompt and initial user input.
- Alternatively, manually enter your system prompt and user input.

### 3. Interacting with the AI

- Type your message in the input area.
- Click the "Go" button or press Ctrl+Enter to send your message.
- The AI's response will appear in the output area.
- The conversation will be visualized in the network diagram.
- If available, a suggested next prompt will be formatted and displayed in the input area.
- If enabled, the AI's response will be read aloud using Text-to-Speech.

### 4. Managing Conversations

- Previous conversations are listed in the conversation history.
- Click on a conversation in the list to load it.
- Use the "Restart" button to begin a new branch from the current conversation.
- The "New Conversation from Last Exchange" button creates a new conversation using the last user-AI exchange.

### 5. Using Templates

- Select a category from the "Categories" dropdown.
- Choose a template from the "Templates" dropdown.
- Click "Edit Template" to modify existing templates.
- Use "Add Template" to create new templates.

### 6. Audio Input

- Click the "Start Recording" button to begin audio recording.
- Speak your message.
- Click "Stop Recording" to end the recording and automatically transcribe it to text using Whisper.
- The transcribed text will be added to the input area.
- A low-pass filter is applied to the audio for improved quality.

### 7. Image Input

- Click the "Choose Image" button to select an image file.
- The image will be encoded and included with your next message to the AI.

### 8. Conversation Visualization

- The network diagram shows the structure of your conversation.
- Each node represents a message, color-coded by the AI model used.
- Click on nodes to navigate through the conversation.
- Node labels are truncated to 500 characters for better visibility.

### 9. Snippet Detection

- The application automatically detects code snippets in AI responses.
- Snippets are highlighted and formatted for better readability.

### 10. Cost Tracking

- The application displays the estimated cost of each interaction based on token usage.

### 11. Search Functionality

- Use the search bar to filter conversations based on their content.

### 12. Settings

- Access the settings menu to configure API keys, model selection, and other preferences.
- New toggle options are available for customizing features like Text-to-Speech.

### 13. Saving and Exporting

- Conversations are automatically saved.
- Right-click on a node in the network diagram to save the conversation as a text file.

### 14. Keyboard Shortcuts

- Ctrl+Enter: Send message to AI

### 15. Automatic Readme Update

- The application now includes a feature to automatically update the readme based on the latest GitHub diff and commit message.
- This feature can be accessed through the new "Specials" menu.
- For users with the appropriate setup, the updated README is now written directly to the repository.

### 16. Text-to-Speech (TTS)

- AI responses can be read aloud using Windows TTS.
- This feature can be toggled on/off in the settings.

### 17. View HTML in WebView

- The application now supports viewing HTML content in a WebView.
- This feature allows for better rendering and interaction with HTML responses from the AI.

### 18. Code Review Function

- A new code review function has been added for project maintainers.
- This feature can analyze the project's C# code and provide feedback on potential bugs.

This AI conversation tool provides a comprehensive environment for interacting with AI models, managing conversations, and organizing your AI-assisted work. By utilizing its various features, users can efficiently conduct and manage complex AI conversations across multiple topics and models.