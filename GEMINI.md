# Project Overview: Max's AI Studio

This project is a sophisticated, multi-faceted C# .NET application named "Max's AI Studio". It functions as both a standalone Windows Presentation Foundation (WPF) desktop application and a Visual Studio (VSIX) extension.

The core purpose of the application is to serve as a client for various AI models, with explicit support for Gemini, Claude, OpenAI, and local LlamaCpp instances.

It features a hybrid user interface: the main window is a WPF application that hosts a modern web-based UI built with React, TypeScript, and Vite.

## Key Technologies

- **Backend & Desktop:** C# / .NET (WPF)
- **Frontend (Web Client):** React, TypeScript, Vite, Tailwind CSS
- **AI Integrations:** Gemini, Claude, OpenAI, LlamaCpp
- **Package Management:**
    - NuGet for .NET dependencies.
    - pnpm for frontend dependencies.
- **Testing:**
    - Cypress for frontend end-to-end testing.
    - .NET testing framework (likely MSTest, NUnit, or xUnit) for backend tests.
- **Build & Deployment:** PowerShell (`.ps1`) and Batch (`.bat`) scripts.

## Project Structure

- **`AiTool3.sln`**: The main solution file for the entire project.
- **`AiStudio4/`**: The main WPF application project.
    - **`AiStudio4/AiStudioClient/`**: The source code for the React/TypeScript web client.
    - **`AiStudio4/AiServices/`**: Contains the specific implementations for interacting with different AI provider APIs.
    - **`AiStudio4/Controls/AiStudioWebView2.cs`**: The WPF control used to host the web client.
- **`VSIXTest/`**: The Visual Studio Extension project.
- **`SharedClasses/`**: A shared library likely containing code used by both the main application and the VSIX extension.
- **`build_and_release.ps1`**: The primary script for creating a release build.

## Common Commands

### .NET Backend

- **Build Solution:** `dotnet build AiTool3.sln`
- **Run Application:** `dotnet run --project AiStudio4/AiStudio4.csproj`
- **Run Backend Tests:** `dotnet test AiTool3.sln`

### Frontend (Web Client)

*All frontend commands should be run from the `AiStudio4/AiStudioClient/` directory.*

- **Install Dependencies:** `pnpm install`
- **Run Development Server:** `pnpm dev`
- **Build for Production:** `pnpm build`
- **Run E2E Tests (Headless):** `pnpm cypress run`
- **Open E2E Tests (Interactive):** `pnpm cypress open`
