const { useState } = React;

const HeaderBar = () => {
    const [logoText, setLogoText] = useState("AI Studio");
    const [systemPrompt, updateSystemPrompt] = useState("");

    const changeLogo = (newText) => {
        setLogoText(newText);
    };

    const handleSystemPromptChange = (e) => {
        setSystemPrompt(e.target.value);
    };

    // export changeLogo and getSystemPrompt
    window.changeLogo = changeLogo;
    window.getSystemPrompt = () => systemPrompt;
    window.updateSystemPrompt = updateSystemPrompt;

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
                    .system-prompt-input {
                        flex-grow: 1;
                        margin-left: 20px;
                        padding: 8px;
                        font-size: 14px;
                        border: none;
                        border-radius: 4px;
                        background-color: #555;
                        color: white;
                    }
                    .system-prompt-input::placeholder {
                        color: #aaa;
                    }
                `}
            </style>

            <div className="header-bar">
                <div className="logo">{logoText}</div>
                <input
                    type="text"
                    className="system-prompt-input"
                    placeholder="Enter system prompt..."
                    value={systemPrompt}
                    onChange={handleSystemPromptChange}
                />
            </div>
        </>
    );
};