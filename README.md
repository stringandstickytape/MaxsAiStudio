# Max's AI Studio

An open-source Windows C# application to query various LLM AIs, including Anthropic Claude, OpenAI ChatGPT, and Ollama-hosted models.  Many features including branched conversations.

![The basic chat interface for Max's AI Studio](./AiTool3/Screenshots/MainUI.png)

# Pre-requisites

For OpenAI, Anthropic, Groq or Gemini, you will need an API key.

For local AI via Ollama, you will need to install https://ollama.com/download/windows and one or more models.

For media transcription, you will need to install https://github.com/m-bain/whisperX .  You can configure the Conda activate.bat path in Edit -> Settings.

For media transcription *and* live transcription, you willl need https://developer.download.nvidia.com/compute/cuda/redist/libcublas/windows-x86_64/
 - extract the three files to the install folder.  You will also need to copy them into whisperX, likely to the path C:\Users\<username>\miniconda3\envs\whisperx\bin
 
 