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
    *   Ensure Conda is installed and the path to `activate.bat` is correctly set in `File > Settings > Set Conda Path...` if you are using Conda-dependent WhisperX features.
    *   Ensure you have a Conda environment with `whisperx` (and its dependencies like `ffmpeg`) installed. For basic Whisper.net, Conda might not be strictly necessary.