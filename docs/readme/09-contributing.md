# 9. Contributing

We welcome contributions! Whether it's reporting a bug, suggesting a new feature, or submitting code changes, your input is valuable. Please refer to a `CONTRIBUTING.md` file (if available in the repository) for detailed guidelines on:
*   Code of conduct.
*   Process for reporting issues.
*   How to submit pull requests.
*   Coding standards and patterns used in the project.

## 9.1 Development Notes
*   **Project Structure:** The application is divided into a C# backend/host (`AiStudio4`) and a TypeScript/React frontend (`AiStudio4/AiStudioClient`).
*   **Backend Development:** Typically done in Visual Studio. Key services are registered in `AiStudio4/App.xaml.cs`. API request handlers reside in `AiStudio4/InjectedDependencies/RequestHandlers/`. Core tool logic is found in `AiStudio4/Core/Tools/`.
*   **Frontend Development:** Uses Vite, with `pnpm` (or `npm`) for package management. Source code is in `AiStudio4/AiStudioClient/src/`. For a detailed frontend structure and data flow explanation, see `AiStudio4/Docs/Feature Designs/ProjectStructure.txt` (if available).
*   **Running Tests:** (Details on running tests would go here if available - e.g., "There aren't any. Yet...")