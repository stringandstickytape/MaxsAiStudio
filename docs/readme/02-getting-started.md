# 2. Getting Started

## 2.1 System Requirements
*   Windows Operating System (with WebView2 Runtime installed - usually included with modern Windows/Edge)
*   .NET 9 SDK (for building from source, not required for running a release)
*   **(For Developers building from source):**
    *   Node.js (check `AiStudio4/AiStudioClient/package.json` for version, typically LTS)
    *   `pnpm` (`npm install -g pnpm`) is preferred for managing frontend dependencies (or `npm`).
    *   (Optional) Git: Required for features like Git Diff inclusion and the GitCommit tool.
    *   (Optional) Conda: If planning to use locally hosted Python-based MCP servers.

## 2.2 Installation

### 2.2.1 Installing from a Release
*(Instructions for installing from a release ZIP or installer would go here. Currently, the `create-release.bat` script creates a `release-!SHORT_SHA!.7z` archive containing the build output.)*

### 2.2.2 Building from Source
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

## 2.3 Initial Setup

### 2.3.1 Setting the Project Path
This is the **most crucial first step**.
1.  Go to `File > Project > Set Project Path...` in the application menu.
2.  Select the root directory of your software development project.
3.  This path is essential for context-aware AI tasks, file operations, Git integration, and .NET project analysis.
    The window title will update to reflect the current project path.

### 2.3.2 Configuring AI Service Providers
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

### 2.3.3 Adding AI Models
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

### 2.3.4 Selecting Default Models
AiStudio4 uses a primary model for main chat interactions and a secondary model for tasks like summaries or quick tool responses.
1.  In the Input Bar (bottom section of the main window), you'll see a "Model Status" area.
2.  Click on the "Primary Model" button (or placeholder text if none is set).
3.  The Command Bar will open, filtered to "Primary Models". Select your desired primary model.
4.  Click on the "Secondary Model" button.
5.  The Command Bar will open, filtered to "Secondary Models". Select your desired secondary model.

These default models can also be associated with System Prompts (see [Section 4.3.1](04-using-aistudio4.md#431-system-prompts)).