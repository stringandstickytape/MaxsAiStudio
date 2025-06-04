# 7. Troubleshooting

*   **Tool Not Working:**
    *   Ensure the tool is activated in the Tool Library or via Command Bar.
    *   Check if the associated System Prompt (if any) has the tool enabled.
    *   For API-based tools (GitHub, Azure DevOps, Sentry, YouTube), ensure the relevant API key/PAT is correctly configured in `File > Settings` or in the tool's `ExtraProperties`.
    *   Check the `_AiStudio4\bin\Debug\net9.0-windows\debug.log` file (path may vary based on build type/installation) for error messages.
*   **AI Not Using an Active Tool:**
    *   The AI makes its own decisions. Try rephrasing your prompt to be more explicit about the desired action or tool.
    *   Ensure the tool's description and schema are clear and guide the AI effectively.
*   **Project Path Dependent Features Not Working:**
    *   Verify `File > Project > Set Project Path...` is set to the correct root directory of your project.
*   **Audio Transcription Fails:**
    *   Ensure Conda is installed and the path to `activate.bat` is correctly set in `File > Settings > Set CondaPath...` if you are using Conda-dependent WhisperX features.
    *   Ensure you have a Conda environment with `whisperx` (and its dependencies like `ffmpeg`) installed. For basic Whisper.net, Conda might not be strictly necessary.

*   **Azure DevOps Wiki Sync Issues:**
    *   **Wiki Sync Not Working:**
        *   Verify that wiki sync is enabled: `File > Wiki Sync > Enable Wiki Sync` should be checked.
        *   Ensure all required configuration settings are filled in via `File > Wiki Sync > Configure Wiki Sync...`
        *   Check that your Azure DevOps PAT is configured: `File > Settings > Set Azure DevOps PAT...`
        *   Verify your PAT has the necessary permissions to read wiki content.
        *   Check the application logs (`debug.log`) for specific error messages during sync attempts.
        *   Restart the application after making configuration changes, as sync only occurs at startup.
    *   **Authentication Errors:**
        *   Ensure your Azure DevOps Personal Access Token (PAT) is valid and not expired.
        *   Verify the PAT has "Wiki (read)" permissions at minimum.
        *   Check that the organization and project names are spelled correctly and match your Azure DevOps setup.
    *   **Wiki Page Not Found:**
        *   Verify the wiki identifier is correct (typically `ProjectName.wiki`).
        *   Ensure the wiki page path exists and is spelled correctly (e.g., `/Instructions/MasterSystemPrompt`).
        *   Check that the wiki page has content and is published.
    *   **System Prompt Not Updating:**
        *   Verify the target system prompt GUID is correct in the configuration.
        *   Check that the wiki content has actually changed since the last sync.
        *   Look for error messages in the application logs that might indicate parsing or saving issues.

*   **Google Drive Integration Issues:**
    *   **Import/Export Fails or 'Credentials Not Found' Error:**
        *   A `credentials.json` file is required in `%APPDATA%\AiStudio4\Config\`. This file should be an OAuth 2.0 Client ID credential for a Desktop application, obtained from your Google Cloud Console. Ensure it's correctly named and placed.
        *   Ensure you have authorized AiStudio4 to access your Google Drive when prompted during the first use of the feature.
        *   Verify that a folder named "Google AI Studio" (case-sensitive) exists in the root of your Google Drive. AiStudio4 will look for files to import from, and save exported files to, this specific folder.
    *   **No Files Listed for Import:**
        *   Check that your JSON files exported from Google AI Studio are located directly within the "Google AI Studio" folder in your Google Drive. Subfolders are not currently scanned.
        *   Ensure the files are in the expected JSON format.
    *   **Conversion Errors During Import/Export:**
        *   The file format from Google AI Studio or AiStudio4 might have changed or might not be fully compatible. Report this issue with details of the error message if possible. This can happen if the structure of the exported/imported JSON is not what AiStudio4 expects.
