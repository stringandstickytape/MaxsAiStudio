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

*   **Google Drive Integration Issues:**
    *   **Import/Export Fails or 'Credentials Not Found' Error:**
        *   Ensure you have authorized AiStudio4 to access your Google Drive when prompted during the first use of the feature.
        *   A `credentials.json` file might be required in `%APPDATA%\AiStudio4\Config\`. This file should be an OAuth 2.0 Client ID credential for a Desktop application, obtained from your Google Cloud Console. Ensure it's correctly named and placed.
        *   Verify that a folder named "Google AI Studio" (case-sensitive) exists in the root of your Google Drive. AiStudio4 will look for files to import from, and save exported files to, this specific folder.
    *   **No Files Listed for Import:**
        *   Check that your JSON files exported from Google AI Studio are located directly within the "Google AI Studio" folder in your Google Drive. Subfolders are not currently scanned.
        *   Ensure the files are in the expected JSON format.
    *   **Conversion Errors During Import/Export:**
        *   The file format from Google AI Studio or AiStudio4 might have changed or might not be fully compatible. Report this issue with details of the error message if possible. This can happen if the structure of the exported/imported JSON is not what AiStudio4 expects.
