const { useState } = React;

const HeaderBar = () => {
    const [logoText, setLogoText] = useState("AI Studio");
    const [systemPrompt, setSystemPrompt] = useState("");

    const changeLogo = (newText) => {
        setLogoText(newText);
    };

    const handleSystemPromptChange = (newPrompt) => {
        setSystemPrompt(newPrompt);
    };

    // export changeLogo and getSystemPrompt
    window.changeLogo = changeLogo;
    window.getSystemPrompt = () => systemPrompt;
    window.updateSystemPrompt = setSystemPrompt;

    return (
        <>
            <style>
                {`
                    .header-bar {
                        display: flex;
                        justify-content: space-between;
                        background-color: #333;
                        color: white;
                        padding: 10px 20px;
                    }
                    .left-section {
                        display: flex;
                        flex-direction: column;
                    }
                    .logo {
                        font-size: 24px;
                        font-weight: bold;
                        margin-bottom: 10px;
                    }
                    .system-prompt-container {
                        flex-grow: 1;
                        margin-left: 20px;
                    }
                    .system-prompt-input {
                        background-color: #555;
                        color: white;
                        border: none;
                        border-radius: 4px;
                        width: 100%;
                    }
                    .system-prompt-input::placeholder {
                        color: #aaa;
                    }
                `}
            </style>

            <div className="header-bar">
                <div className="left-section">
                    <div className="logo">{logoText}</div>
                    <ToolsDropdown />
                </div>
                <div className="system-prompt-container">
                    <InputBox
                        onSend={() => { }}
                        value={systemPrompt}
                        onChange={handleSystemPromptChange}
                        placeholder="Enter system prompt..."
                        className="system-prompt-input"
                    />
                </div>
            </div>
        </>
    );
};