# 1. Introduction

## 1.1 What is Max's AI Studio 4?
Max's AI Studio 4 (AiStudio4) is a feature-rich desktop application engineered to serve as a comprehensive AI-powered workspace for software developers, power users, and anyone looking to harness the capabilities of modern AI models. It provides a unified interface for interacting with various Large Language Models (LLMs), an extensible tool ecosystem, and deep integration with local development projects.

## 1.2 Who is it for?
*   **Software Developers:** Seeking to integrate AI deeply into their local development environment, automate coding tasks, get contextual help, manage project-related information efficiently, and have AI iterate over code, edit it, and even launch it.
*   **Power Users:** Who require a highly customizable and extensible GUI-based AI chat client with advanced features for branching conversations and interacting with multiple AI models and tools.
*   **AI Enthusiasts:** Looking for a platform to experiment with different models, prompt engineering, and custom AI tool development.

## 1.3 Highlights
*   **Unified AI Chat:** Seamlessly chat with OpenAI, Anthropic Claude, Google Gemini models, and custom Model Context Protocol (MCP) servers from one interface. Supports Ollama through OpenAI API compatibility.
*   **Powerful Tool Ecosystem:** Leverage built-in tools for file operations (read, write, search, diff), web tasks (DuckDuckGo, YouTube, URL scraping), developer integrations (Git, GitHub, Azure DevOps, Sentry, Vite), and more. Allows AI to iterate over your code, edit it, and even launch it.
*   **Extensible & Customizable AI:** Define new tools with JSON schemas or connect to external MCP-compliant tool servers.
*   **Deep Project Integration:** Work directly with your local codebase using the `ProjectPath` setting for context-aware AI assistance and file operations.
*   **Google AI Studio Integration:** Import existing conversations from your Google AI Studio projects via Google Drive
*   **Advanced Conversation Management:** Navigate complex discussions with branched conversation history, powerful search, and an interactive tree view.
*   **Modern Desktop Experience:** A .NET 9 WPF application hosting a fluid, responsive React/TypeScript UI via WebView2.
*   **Personalizable Interface:** AI tool-based theming engine.
*   **Real-time Interaction:** Streaming AI responses and tool loop interjection. WebSockets are used for streaming AI responses, live conversation updates, status messages, and project file system monitoring.
*   **Productivity Focused:** Command bar, pinnable shortcuts, rich system & user prompt systems with model/tool/MCP associations.
*   **Extras:** Integrated local audio transcription (Whisper.net), .NET project analysis.