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
*   **Running Tests:** The project includes a suite of end-to-end tests for the frontend, built with Cypress. You can run these tests from the `AiStudio4/AiStudioClient` directory:
    *   `pnpm cypress run`: Runs the tests in headless mode.
    *   `pnpm cypress open`: Opens the interactive Cypress test runner.
    *   When developing, you can use the `--testing-profile` command-line argument when launching `AiStudio4` to ensure a clean data environment for testing.