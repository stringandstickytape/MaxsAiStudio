﻿# 2. Getting Started

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

**3. Running with a Testing Profile:**
    *   To run the application with a separate, clean data profile for testing, you can use the `--testing-profile` command-line argument. This will store all application data in `%APPDATA%\AiStudio4_Test` and will clear this directory on each startup.
    *   You can configure this in Visual Studio under the project's Debug Properties.

**2. Frontend Development (Optional, for modifying the UI - `AiStudio4/AiStudioClient` directory):**
    1.  Open a terminal in the `AiStudio4/AiStudioClient` directory.
    2.  **Install Dependencies:**
        *   `pnpm install`
    3.  **Run Frontend Dev Server:**
        *   `pnpm run dev` - This starts the Vite development server, typically on `http://localhost:5173`.
        *   When a debugger is attached at the startup of the `AiStudio4` C# project, it is configured to proxy requests to this Vite dev server. This allows for Hot Module Replacement (HMR) and a faster frontend development cycle.
    4.  **Build Frontend for Production:**
        *   `pnpm run build`
        *   This command compiles the React/TypeScript application and outputs the static assets to the `AiStudio4/AiStudioClient/dist` folder. These are the files that the C# application serves when no debugger is attached.

**5. Running End-to-End Tests:**
    *   The project includes a suite of end-to-end tests using Cypress.
    *   To run the tests in headless mode (e.g., for CI): `pnpm cypress run`
    *   To open the interactive Cypress test runner: `pnpm cypress open`

## 2.3 Initial Setup

### 2.3.1 Managing and Setting Projects
This is the **most crucial first step**. AiStudio4 allows you to manage a list of projects. The "active" project provides the context for all file and development-related tools.

1.  **Open Project Management:** Go to `Project > Manage Projects...` in the application menu.
2.  **Add Your First Project:**
    *   Click "Add" to open the Project Editor.
    *   Give your project a **Name** (e.g., "My Web App").
    *   Set the **Path** to the root directory of your software development project.
    *   Click "Save".
3.  **Set the Active Project:**
    *   In the "Project Management" window, select your newly created project.
    *   Click "Set Active".
    *   The main window title will update to reflect the active project's path. A shortcut "folder" icon in the menu bar also provides quick Explorer access.

You can add multiple projects and switch between them using this dialog. The active project determines the context for all AI tasks, file operations, Git integration, and .NET project analysis.

### 2.3.2 Configuring AI Service Providers
To use AI models, you need to configure their service providers (e.g., OpenAI, Anthropic).
1.  Open the main settings:
    *   Use the Command Bar: Press `Ctrl+K`, type "Providers", and select "Service Providers".
    *   Or, use the application menu if available (e.g., `File > Settings` and navigate to the "Providers" tab).
2.  In the "Service Providers" section, add API keys to the existing providers, or add new providers:
    *   Click "Add Provider".
    *   **Friendly Name:** A name you choose (e.g., "My OpenAI Account").
    *   **Service Name:** Select the AI service type from the dropdown (e.g., `NetOpenAi` for OpenAI, `Claude` for Anthropic, `Gemini` for Google).
    *   **API URL:** The base URL for the API (e.g., `https://api.openai.com/v1`).
    *   **Charging Strategy:** Select the cost calculation model for this provider:
        *   **No Caching (Standard):** Basic input/output token pricing without caching considerations
        *   **Claude Caching Model:** Uses Claude's caching multipliers (1.25x for cache creation, 0.1x for cache reads)
        *   **OpenAI Caching Model:** Uses OpenAI's caching multipliers (1.0x for cache creation, 0.25x for cache reads)
        *   **Gemini Caching Model:** Uses Gemini's caching multipliers (0.25x for cache reads) with tiered pricing support
    *   **API Key:** Your personal API key for the service.
    *   **Icon Name (Optional):** Select an icon to represent this provider.
    *   Click "Add Provider" to save.

### 2.3.3 Adding AI Models
Once providers are configured, add specific AI models you want to use.

#### 2.3.3.1 Configuring Llama.cpp Models
For local models using Llama.cpp, you must select the `LlamaCpp` service provider when adding a model. The `Additional Parameters (JSON)` field must be used to provide the necessary configuration:

```json
{
  "ModelPath": "C:\\path\\to\\your\\model.gguf",
  "ContextSize": 4096,
  "GpuLayerCount": -1,
  "Threads": -1,
  "BatchSize": 2048,
  "FlashAttention": true,
  "AdditionalArgs": ""
}
```
- **ModelPath**: The absolute path to your `.gguf` model file. Note that backslashes in the JSON must be escaped (e.g., `C:\\path`).
- **ContextSize**: The context size for the model.
- **GpuLayerCount**: Number of layers to offload to the GPU. `-1` will attempt to auto-detect.
- **Threads**: Number of threads to use. `-1` will attempt to auto-detect.
- **BatchSize**: The batch size for processing.
- **FlashAttention**: Enable flash attention if your hardware supports it.
- **AdditionalArgs**: Any other command-line arguments to pass to the `llama-server` process.

When you first use a Llama.cpp model, AiStudio4 will automatically download the appropriate `llama-server` binary to your `%APPDATA%\AiStudio4\llama-cpp` directory if it's not already present.

1.  In the settings modal, navigate to the "Models" tab.
2.  Click "Add Model".
3.  Fill in the details:
    *   **Friendly Name:** A display name for the model (e.g., "GPT-4 Turbo").
    *   **Model Name:** The exact model identifier used by the API (e.g., `gpt-4-turbo`).
    *   **Service Provider:** Select one of your configured providers from the dropdown.
    *   **Enable Tiered Pricing:** (Optional) Check this box to enable tiered pricing for models that have different rates above and below a token boundary (e.g., Gemini 1.5 Pro with 128K token boundary).
    *   **Pricing Configuration:**
        *   **Input Price/1M (< Boundary):** Price per 1 million input tokens below the boundary (or default price if tiered pricing is disabled).
        *   **Output Price/1M (< Boundary):** Price per 1 million output tokens below the boundary (or default price if tiered pricing is disabled).
        *   **Token Boundary:** (Tiered pricing only) The token limit where pricing changes (e.g., 128000 for Gemini 1.5 Pro).
        *   **Input Price/1M (> Boundary):** (Tiered pricing only) Price per 1 million input tokens above the boundary.
        *   **Output Price/1M (> Boundary):** (Tiered pricing only) Price per 1 million output tokens above the boundary.
    *   **Color:** (Optional) Choose a color for UI identification.
    *   **Notes:** (Optional) Any personal notes about this model.
    *   **Additional Parameters (JSON):** (Optional, Advanced) JSON string for extra parameters to send with requests to this model.
    *   **Supports Prefill:** Check if the model supports prefilling assistant responses.  The UI does not currently support prefilling however.
    *   **Requires 1.0 Temperature:** Check if the model strictly requires a temperature of 1.0.
    *   **Reasoning Effort:** (For compatible models) Select `low`, `medium`, or `high`.
    *   **Enable Text-to-Speech (TTS):** Check if this is a Gemini TTS model.
    *   **TTS Voice Name:** If TTS is enabled, specify the voice name (e.g., "Kore" for Gemini).
4.  Click "Add Model" to save.

### 2.3.4 Selecting Default Models
AiStudio4 uses a primary model for main chat interactions and a secondary model for tasks like summaries or quick tool responses.
1.  In the Input Bar (bottom section of the main window), you'll see a "Model Status" area.
2.  Click on the "Primary Model" button.
3.  The Command Bar will open, filtered to "Primary Models". Select your desired primary model.
4.  Click on the "Secondary Model" button.
5.  The Command Bar will open, filtered to "Secondary Models". Select your desired secondary model.

These default models can also be associated with System Prompts (see [Section 4.3.1](04-using-aistudio4.md#431-system-prompts)).