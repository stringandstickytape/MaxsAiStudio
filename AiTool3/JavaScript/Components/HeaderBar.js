const { useState } = React;

const HeaderBar = () => {
    const [logoText, setLogoText] = useState("AI Studio");
    const [systemPrompt, setSystemPrompt] = useState("");
    const [mainAIOptions, setMainAIOptions] = useState([]);
    const [summaryAIOptions, setSummaryAIOptions] = useState([]);
    const [mainAIValue, setMainAIValue] = useState(mainAIOptions[0]);
    const [summaryAIValue, setSummaryAIValue] = useState(summaryAIOptions[0]);

    // Export methods
    window.setDropdownOptions = (dropdownName, options) => {
        if (dropdownName === 'mainAI') {
            setMainAIOptions(options);
        } else if (dropdownName === 'summaryAI') {
            setSummaryAIOptions(options);
        }
    };

    window.getDropdownValue = (dropdownName) => {
        if (dropdownName === 'mainAI') {
            return mainAIValue;
        } else if (dropdownName === 'summaryAI') {
            return summaryAIValue;
        }
    };

    window.setDropdownValue = (dropdownName, value) => {
        if (dropdownName === 'mainAI') {
            setMainAIValue(value);
        } else if (dropdownName === 'summaryAI') {
            setSummaryAIValue(value);
        }
    };

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
                        flex-direction: column;
                        background-color: #333;
                        color: white;
                    }
                    .main-header {
                        display: flex;
                        justify-content: space-between;
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
                    .options-bar {
                        display: flex;
                        justify-content: space-around;
                        background-color: #444;
                        padding: 5px 0;
                    }
                    .option {
                        cursor: pointer;
                        padding: 5px 10px;
                    }
                    .option:hover {
                        background-color: #555;
                    }
                                        .dropdown-container {
                        display: flex;
                        align-items: center;
                        margin-right: 20px;
                    }
                    .dropdown-container label {
                        margin-right: 10px;
                        color: white;
                    }
                    .dropdown-container select {
                        background-color: #555;
                        color: white;
                        border: none;
                        border-radius: 4px;
                        padding: 5px;
                    }
                `}
            </style>

            <div className="header-bar">
                <div className="main-header">
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
                <div className="options-bar">
                    <DropDown
                        id="mainAI"
                        label="Main AI"
                        options={mainAIOptions}
                        value={mainAIValue}
                        onChange={setMainAIValue}
                    />
                    <DropDown
                        id="summaryAI"
                        label="Summaries/Suggestions AI"
                        options={summaryAIOptions}
                        value={summaryAIValue}
                        onChange={setSummaryAIValue}
                    />
                </div>
            </div>
        </>
    );
};