# Max's AI Studio

An open-source Windows C# application to query various LLM AIs, including Anthropic Claude, OpenAI ChatGPT, and Ollama-hosted models.  Many features including branched conversations.

![The basic chat interface for Max's AI Studio](./AiTool3/Screenshots/MainUI.png)

# Pre-requisites

For OpenAI, Anthropic, Groq or Gemini, you will need an API key.  Enter it in Edit -> Settings or on first run.

For local AI via Ollama, you will need to install https://ollama.com/download/windows and one or more models.

For media transcription, you will need to install https://github.com/m-bain/whisperX .  You can configure the Conda activate.bat path in Edit -> Settings.

For media transcription *and* live transcription, you willl need https://developer.download.nvidia.com/compute/cuda/redist/libcublas/windows-x86_64/
 - extract the three files to the install folder.  You will also need to copy them into whisperX, likely to the path C:\Users\<username>\miniconda3\envs\whisperx\bin
 
# UI

Note that two AIs are specified.  The main AI is used for normal chat, the secondary AI is used for everything else (except embeddings).

![The AI selection bar for Max's AI Studio, which shows main AI and secondary AI dropdown lists](./AiTool3/Screenshots/AIChoice.png)

Use the sidebar to revisit and continue previous conversations and messages.  Right-click for options in the Conversations list and the Messages diagram.  Conversations are automatically summarised using the secondary AI.

![The sidebar for Max's AI Studio, including a Conversations list and a Messages node diagram](./AiTool3/Screenshots/Sidebar.png)

