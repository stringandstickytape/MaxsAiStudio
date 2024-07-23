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
                        align-items: center;
                        background-color: #333;
                        color: white;
                        padding: 10px 20px;
                    }
                    .logo {
                        font-size: 24px;
                        font-weight: bold;
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
                    }
                    .system-prompt-input::placeholder {
                        color: #aaa;
                    }
                `}
            </style>

            <div className="header-bar">
                <div className="logo">{logoText}</div>
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